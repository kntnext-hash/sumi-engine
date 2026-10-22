using System;
using System.Collections;
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
        [Serializable] sealed class Report
        {
            public string version, error;
            public bool passed;
            public int startFrame, endFrame;
            public WeaponReport[] runs;
        }
        [Serializable] sealed class WeaponReport
        {
            public string requestedWeapon, weapon, build;
            public bool passed, sawAttack, weaponMatches;
            public int ticks, branches, kills, purchases, peakFields, peakWeaponFields;
            public float pigment;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Application.isEditor) return;
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "--bokumyaku-smoke-output");
            if (i < 0 || i + 1 >= args.Length) return;
            var probe = new GameObject("Bokumyaku explicit player probe").AddComponent<BokumyakuPlayerProbe>();
            probe.output = Path.GetFullPath(args[i + 1]);
            Application.runInBackground = true;
            Application.targetFrameRate = 60;
        }
        IEnumerator Start()
        {
            yield return null;
            var report = new Report { version = Application.unityVersion, startFrame = Time.frameCount, passed = true, runs = new WeaponReport[3] };
            BokumyakuGame game = null;
            try
            {
                Directory.CreateDirectory(output);
                game = FindFirstObjectByType<BokumyakuGame>();
                if (game == null) throw new InvalidOperationException("Game scene missing.");
                game.SavePathOverride = Path.Combine(output, "qa-progress.json");
                game.Meta = new MetaProgress();
            }
            catch (Exception e) { report.error = e.ToString(); report.passed = false; }
            for (int i = 0; i < report.runs.Length && report.error == null; i++)
            {
                var row = new WeaponReport { requestedWeapon = ((WeaponKind)i).ToString() };
                report.runs[i] = row;
                try
                {
                    game.SetBuild(BuildPreset.Farming); game.SetWeapon((WeaponKind)i);
                    game.StartRun(42); game.SetSpeed(2);
                }
                catch (Exception e) { report.error = e.ToString(); report.passed = false; break; }
                float deadline = Time.realtimeSinceStartup + 25;
                while (game.Run.TickCount < 900 && Time.realtimeSinceStartup < deadline)
                {
                    foreach (var hit in game.Run.Attacks) row.sawAttack |= hit.Weapon == (WeaponKind)i;
                    row.peakFields = Math.Max(row.peakFields, game.Run.Fields.Count);
                    int matchingFields = 0;
                    foreach (var field in game.Run.Fields) if (field.Weapon == (WeaponKind)i) matchingFields++;
                    row.peakWeaponFields = Math.Max(row.peakWeaponFields, matchingFields);
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
                    row.pigment = game.GetComponent<SumiCanvas>().Core.Paper.TotalPigment();
                    row.passed = row.ticks >= 900 && row.branches > 4 && row.purchases > 0 &&
                        row.pigment > 0 && row.sawAttack && row.weaponMatches &&
                        (i != (int)WeaponKind.Bleed || row.peakWeaponFields > 0);
                    report.passed &= row.passed;
                }
                catch (Exception e) { report.error = e.ToString(); report.passed = false; }
            }
            report.endFrame = Time.frameCount;
            report.passed &= report.endFrame > report.startFrame;
            File.WriteAllText(Path.Combine(output, "player-report.json"), JsonUtility.ToJson(report, true));
            Application.Quit(report.passed ? 0 : 1);
        }
    }
}
