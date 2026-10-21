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
            public string version, error, build;
            public bool passed;
            public int startFrame, endFrame, ticks, branches, kills, purchases;
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
            var report = new Report { version = Application.unityVersion, startFrame = Time.frameCount };
            BokumyakuGame game = null;
            try
            {
                Directory.CreateDirectory(output);
                game = FindFirstObjectByType<BokumyakuGame>();
                if (game == null) throw new InvalidOperationException("Game scene missing.");
                game.SavePathOverride = Path.Combine(output, "qa-progress.json");
                game.Meta = new MetaProgress();
                game.SetBuild(BuildPreset.Farming);
                game.StartRun(42); game.SetSpeed(2);
            }
            catch (Exception e) { report.error = e.ToString(); }
            float deadline = Time.realtimeSinceStartup + 35;
            while (report.error == null && game.Run.TickCount < 1200 && Time.realtimeSinceStartup < deadline)
                yield return null;
            try
            {
                if (report.error == null)
                {
                    game.SetPaused(true);
                    report.endFrame = Time.frameCount;
                    report.ticks = game.Run.TickCount;
                    report.branches = game.Run.Branches.Count;
                    report.kills = game.Run.Kills;
                    foreach (int level in game.Run.UpgradeLevels) report.purchases += level;
                    report.build = game.Run.Build.ToString();
                    report.pigment = game.GetComponent<SumiCanvas>().Core.Paper.TotalPigment();
                    report.passed = report.endFrame > report.startFrame && report.ticks >= 1200 &&
                        report.branches > 4 && report.purchases > 0 && report.pigment > 0;
                }
            }
            catch (Exception e) { report.error = e.ToString(); report.passed = false; }
            File.WriteAllText(Path.Combine(output, "player-report.json"), JsonUtility.ToJson(report, true));
            Application.Quit(report.passed ? 0 : 1);
        }
    }
}
