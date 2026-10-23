using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Bokumyaku.Presentation;
using Bokumyaku.Simulation;
using ConnectPiece.SumiEngine;
using UnityEngine;

namespace Bokumyaku.Diagnostics
{
    // Opt-in installed-player check. Ordinary launches never create a probe or alter saves.
    public sealed class BokumyakuPlayerProbe : MonoBehaviour
    {
        string output;
        bool resumeOnly;
        bool captureScreens;
        [Serializable] sealed class Report
        {
            public string version, error;
            public BattleTiming.Sample[] slowSections;
            public bool passed;
            public int startFrame, endFrame;
            public WeaponReport[] runs;
            public bool autoRepeatPassed;
            public int autoRepeatSeed, completedRuns, loadedArtCount, overlayTransparentPixels, overlayInkPixels;
            public long sessionCrystals;
            public string autoRepeatMethod;
            public bool checkpointResumePassed, separateProcessResume;
        }
        [Serializable] sealed class WeaponReport
        {
            public string requestedWeapon, weapon, build;
            public bool passed, sawAttack, weaponMatches, checkpointResumePassed;
            public int ticks, branches, kills, purchases, peakFields, peakWeaponFields;
            public bool heardWeaponSource;
            public string attackClip;
            public float pigment;
            public int measuredFrames;
            public double averageFrameMs, maxFrameMs;
            public int startupFrames, steadyFrames;
            public double startupMaxMs, steadyAverageMs, steadyMaxMs;
            public List<SlowFrame> slowFrames = new List<SlowFrame>();
        }
        [Serializable] sealed class SlowFrame
        {
            public int startFrame, endFrame, tick, pendingBranches, collections;
            public double milliseconds;
            public bool startup;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Application.isEditor)
            {
#if UNITY_EDITOR
                // Explicit, one-shot Editor QA isolation, applied before any Start migration.
                const string key = "Bokumyaku.QaSavePath";
                string isolated = UnityEditor.SessionState.GetString(key, "");
                UnityEditor.SessionState.EraseString(key);
                if (!string.IsNullOrEmpty(isolated))
                {
                    var editorGame = FindFirstObjectByType<BokumyakuGame>();
                    if (editorGame != null) editorGame.SavePathOverride = isolated;
                }
#endif
                return;
            }
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "--bokumyaku-smoke-output");
            if (i < 0 || i + 1 >= args.Length) return;
            var probe = new GameObject("Bokumyaku explicit player probe").AddComponent<BokumyakuPlayerProbe>();
            BattleTiming.Enabled=true;
            probe.output = Path.GetFullPath(args[i + 1]);
            probe.resumeOnly = Array.IndexOf(args, "--bokumyaku-resume-check") >= 0;
            probe.captureScreens = Array.IndexOf(args, "--bokumyaku-capture-screens") >= 0;
            // Set the isolated save before the scene's Start can load normal progress.
            var game = FindFirstObjectByType<BokumyakuGame>();
            if (game != null) game.SavePathOverride = Path.Combine(probe.output, "qa-campaign.json");
            Application.runInBackground = true;
            Application.targetFrameRate = 60;
        }
        // The persistent mask is GPU-only after its first upload. Read back only in explicit QA.
        static Color32[] ReadInkMask(Texture2D source)
        {
            if(source==null)throw new InvalidOperationException("Persistent ink mask missing.");
            var old=RenderTexture.active;
            var target=RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGB32);
            Texture2D copy=null;
            try
            {
                Graphics.Blit(source,target);RenderTexture.active=target;
                copy=new Texture2D(source.width,source.height,TextureFormat.RGBA32,false);
                copy.ReadPixels(new Rect(0,0,source.width,source.height),0,0);copy.Apply();
                return copy.GetPixels32();
            }
            finally{RenderTexture.active=old;if(copy!=null)Destroy(copy);RenderTexture.ReleaseTemporary(target);}
        }
        IEnumerator Start()
        {
            yield return null;
            var report = new Report { version = Application.unityVersion, startFrame = Time.frameCount, passed = true, runs = new WeaponReport[resumeOnly ? 0 : 6], separateProcessResume = resumeOnly };
            BokumyakuGame game = null;
            try
            {
                Directory.CreateDirectory(output);
                game = FindFirstObjectByType<BokumyakuGame>();
                if (game == null) throw new InvalidOperationException("Game scene missing.");
                if (!game.TryReloadProgress()) throw new InvalidOperationException("Isolated campaign load failed.");
            }
            catch (Exception e) { report.error = e.ToString(); report.passed = false; }
            if (resumeOnly && report.error == null)
            {
                RunSimulator expected = null;
                try
                {
                    string saved = File.ReadAllText(Path.Combine(output, "checkpoint-expected.json"));
                    report.checkpointResumePassed = game.ResumeAvailable && game.IsPaused && game.Speed == 2 &&
                        game.Campaign.TotalRuns == 1 && JsonUtility.ToJson(game.Run.CaptureState()) == saved;
                    expected = RunSimulator.Restore(game.Run.CaptureState());
                    if (!report.checkpointResumePassed || !game.ResumeRun()) throw new InvalidOperationException("Fresh-process resume differs from saved state.");
                }
                catch (Exception e) { report.error = e.ToString(); report.passed = false; }
                int target = game.Run == null ? 0 : game.Run.TickCount + 600;
                float resumeDeadline = Time.realtimeSinceStartup + 20;
                while (report.error == null && game.Run.TickCount < target && game.Run.State == RunStatus.Running && Time.realtimeSinceStartup < resumeDeadline) yield return null;
                if (report.error == null)
                {
                    try
                    {
                        game.SetPaused(true);
                        while (expected.TickCount < game.Run.TickCount && expected.State == RunStatus.Running) expected.Tick();
                        report.checkpointResumePassed &= game.Run.TickCount >= target &&
                            JsonUtility.ToJson(expected.CaptureState()) == JsonUtility.ToJson(game.Run.CaptureState()) && game.SaveProgress();
                        report.passed &= report.checkpointResumePassed;
                    }
                    catch (Exception e) { report.error = e.ToString(); report.passed = false; }
                }
                report.slowSections=BattleTiming.Capture();
            report.endFrame = Time.frameCount;
                File.WriteAllText(Path.Combine(output, "player-resume-report.json"), JsonUtility.ToJson(report, true));
                Application.Quit(report.passed ? 0 : 1); yield break;
            }
            if (captureScreens && report.error == null)
            {
                game.SetPage(BokumyakuGame.GamePage.Home);
                yield return null;
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(output, "player-home.png"));
                yield return null;
            }
            for (int i = 0; i < report.runs.Length && report.error == null; i++)
            {
                var row = new WeaponReport { requestedWeapon = ((WeaponKind)i).ToString() };
                report.runs[i] = row;
                try
                {
                    if (game.ResumeAvailable && !game.AbandonRun()) throw new InvalidOperationException("Could not discard previous QA run.");
                    game.SetBuild(BuildPreset.Farming); game.SetWeapon((WeaponKind)i);
                    if (!game.TryStartRun(42)) throw new InvalidOperationException("Could not persist new run.");
                    game.SetSpeed(2);
                }
                catch (Exception e) { report.error = e.ToString(); report.passed = false; break; }
                var expectedClip = "sfx_attack_" + ((WeaponKind)i).ToString().ToLowerInvariant();
                var audioSources = game.GetComponents<AudioSource>();
                float deadline = Time.realtimeSinceStartup + 25;
                double frameTotalMs = 0;
                double runStart = Time.realtimeSinceStartupAsDouble, steadyTotalMs = 0;
                while (game.Run.TickCount < 900 && Time.realtimeSinceStartup < deadline)
                {
                    foreach (var source in audioSources)
                        if (source.isPlaying && source.clip != null && source.clip.name == expectedClip && source.volume > 0 && !source.mute)
                        { row.heardWeaponSource = true; row.attackClip = source.clip.name; }
                    foreach (var hit in game.Run.Attacks) row.sawAttack |= hit.Weapon == (WeaponKind)i;
                    row.peakFields = Math.Max(row.peakFields, game.Run.Fields.Count);
                    int matchingFields = 0;
                    foreach (var field in game.Run.Fields) if (field.Weapon == (WeaponKind)i) matchingFields++;
                    row.peakWeaponFields = Math.Max(row.peakWeaponFields, matchingFields);
                    double frameStart = Time.realtimeSinceStartupAsDouble;
                    int startFrame = Time.frameCount, collections = GC.CollectionCount(0);
                    bool startup = frameStart - runStart < 2;
                    yield return null;
                    double elapsedMs = (Time.realtimeSinceStartupAsDouble - frameStart) * 1000;
                    frameTotalMs += elapsedMs; row.measuredFrames++;
                    row.maxFrameMs = Math.Max(row.maxFrameMs, elapsedMs);
                    if(startup){row.startupFrames++;row.startupMaxMs=Math.Max(row.startupMaxMs,elapsedMs);}
                    else {row.steadyFrames++;steadyTotalMs+=elapsedMs;row.steadyMaxMs=Math.Max(row.steadyMaxMs,elapsedMs);}
                    if(elapsedMs>33.333 && row.slowFrames.Count<64)
                        row.slowFrames.Add(new SlowFrame{startFrame=startFrame,endFrame=Time.frameCount,tick=game.Run.TickCount,
                            pendingBranches=game.Run.Branches.Count-game.PaintedBranchCount,collections=GC.CollectionCount(0)-collections,
                            milliseconds=elapsedMs,startup=startup});
                }
                row.averageFrameMs = row.measuredFrames == 0 ? 0 : frameTotalMs / row.measuredFrames;
                row.steadyAverageMs = row.steadyFrames == 0 ? 0 : steadyTotalMs / row.steadyFrames;
                if (captureScreens && i == 0)
                {
                    game.SetPaused(true);
                    yield return null;
                    yield return new WaitForEndOfFrame();
                    ScreenCapture.CaptureScreenshot(Path.Combine(output, "player-battle.png"));
                    yield return null;
                }
                try
                {
                    game.SetPaused(true);
                    row.ticks = game.Run.TickCount;
                    row.branches = game.Run.Branches.Count;
                    row.kills = game.Run.Kills;
                    foreach (int level in game.Run.UpgradeLevels) row.purchases += level;
                    row.build = game.Run.Build.ToString();
                    row.weapon = game.Run.Weapon.ToString();
                    row.weaponMatches = game.Run.Weapon == (WeaponKind)i;
                    game.PresentBattle();
                    foreach(var pixel in ReadInkMask(game.BranchOverlay)) row.pigment += pixel.a / 255f;
                    string before = JsonUtility.ToJson(game.Run.CaptureState());
                    row.checkpointResumePassed = game.SaveProgress() && game.TryReloadProgress() && game.IsPaused &&
                        game.ResumeAvailable && game.Speed == 2 && JsonUtility.ToJson(game.Run.CaptureState()) == before;
                    row.passed = row.ticks >= 900 && row.branches > 4 && row.purchases > 0 &&
                        row.pigment > 0 && row.sawAttack && row.weaponMatches && row.checkpointResumePassed &&
                        row.heardWeaponSource && game.LoadedWeaponAudioCount == 6 &&
                        (i != (int)WeaponKind.Bleed || row.peakWeaponFields > 0);
                    report.passed &= row.passed;
                }
                catch (Exception e) { report.error = e.ToString(); report.passed = false; }
            }
            if (report.error == null && report.passed)
            {
                try
                {
                    if (game.ResumeAvailable && !game.AbandonRun()) throw new InvalidOperationException("Could not discard weapon fixture.");
                    game.SetBuild(BuildPreset.Manual); game.SetWeapon(WeaponKind.Point);
                    game.SetPolicy(GrowthPolicy.Focus); game.SetAutoRepeat(true);
                    game.StartRun(42); game.SetSpeed(2);
                    // Accelerate only the deterministic fixture; the final defeat, settlement,
                    // saved reward, countdown and next run must occur through live Update.
                    var expected = new RunSimulator(42, game.Meta);
                    expected.SetPolicy(GrowthPolicy.Focus);
                    while (expected.State == RunStatus.Running) expected.Tick();
                    if (expected.State != RunStatus.Defeated) throw new InvalidOperationException("Defeat fixture missing.");
                    while (game.Run.TickCount < expected.TickCount - 1) game.Run.Tick();
                    report.autoRepeatMethod = "Fast-forwarded deterministic pre-defeat fixture; live Update defeat/save/3-second active countdown/restart";
                }
                catch (Exception e) { report.error = e.ToString(); report.passed = false; }
                float retryDeadline = Time.realtimeSinceStartup + 12;
                while (report.error == null && game.Run.Seed == 42 && Time.realtimeSinceStartup < retryDeadline)
                    yield return null;
                try
                {
                    game.SetPaused(true);
                    report.autoRepeatSeed = game.Run.Seed;
                    report.completedRuns = game.CompletedRuns;
                    report.sessionCrystals = game.SessionCrystals;
                    report.loadedArtCount = game.LoadedArtCount;
                    foreach (var pixel in ReadInkMask(game.BranchOverlay))
                    {
                        if (pixel.a == 0) report.overlayTransparentPixels++;
                        else report.overlayInkPixels++;
                    }
                    report.autoRepeatPassed = game.Run.Seed == 43 && game.CompletedRuns == 1 && game.SessionCrystals > 0 &&
                        game.Speed == 2 && game.Run.Policy == GrowthPolicy.Focus && game.Run.Build == BuildPreset.Manual &&
                        game.Run.Weapon == WeaponKind.Point && !game.AutoRepeatPending && game.Meta.Crystals == game.SessionCrystals;
                    report.passed &= report.autoRepeatPassed && report.loadedArtCount == 7 && report.overlayTransparentPixels > 0 && report.overlayInkPixels > 0;
                    game.SetAutoRepeat(false);
                    report.checkpointResumePassed = game.SaveProgress();
                    File.WriteAllText(Path.Combine(output, "checkpoint-expected.json"), JsonUtility.ToJson(game.Run.CaptureState()));
                    report.passed &= report.checkpointResumePassed;
                }
                catch (Exception e) { report.error = e.ToString(); report.passed = false; }
            }
            report.slowSections=BattleTiming.Capture();
            report.endFrame = Time.frameCount;
            report.passed &= report.endFrame > report.startFrame;
            File.WriteAllText(Path.Combine(output, "player-report.json"), JsonUtility.ToJson(report, true));
            Application.Quit(report.passed ? 0 : 1);
        }
    }
}
