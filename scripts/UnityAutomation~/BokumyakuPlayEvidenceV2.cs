using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Bokumyaku.Presentation;
using Bokumyaku.Simulation;

// Real Update progression and synthesized GameView clicks, not physical mouse input.
public static class BokumyakuPlayEvidenceV2
{
    static BokumyakuGame game;
    static EditorWindow view;
    static Queue<Action> actions;
    static double next, started;
    static int firstFrame, pauseTick, stage, manualTick, manualLevels;
    static MetaProgress original;
    static string originalPath;
    static List<object> checks;
    static bool passed;
    const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    public static string Run()
    {
        if (!EditorApplication.isPlaying) throw new Exception("Play game scene first");
        game = UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();
        if (game == null || !game.AwaitingStart) throw new Exception("Use a fresh game scene; do not overwrite an active run");
        original = game.Meta; originalPath = game.SavePathOverride;
        game.Meta = new MetaProgress();
        game.SavePathOverride = Path.GetFullPath("../logs/bokumyaku-qa-v2/progress.json");
        view = EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));
        view.Focus();
        firstFrame = Time.frameCount; started = EditorApplication.timeSinceStartup; next = started;
        stage = 0; passed = true; checks = new List<object>(); actions = new Queue<Action>();
        Click(795, 246);
        actions.Enqueue(() => Check("出撃前ビルド選択", game.SelectedBuild == BuildPreset.Farming && game.AwaitingStart, game.SelectedBuild.ToString()));
        Click(955, 350);
        actions.Enqueue(() => Check("出撃前成長方針", game.Run.Policy == GrowthPolicy.Focus, game.Run.Policy.ToString()));
        Click(271, 482);
        actions.Enqueue(() => Check("出撃と選択保持", !game.AwaitingStart && game.Run.Build == BuildPreset.Farming && game.Run.Policy == GrowthPolicy.Focus, new { build = game.Run.Build.ToString(), policy = game.Run.Policy.ToString() }));
        Click(620, 178);
        actions.Enqueue(() => { pauseTick = game.Run.TickCount; Check("一時停止", game.IsPaused, pauseTick); });
        actions.Enqueue(() => Check("停止中tick維持", game.Run.TickCount == pauseTick, game.Run.TickCount));
        Click(800, 178); Click(620, 178);
        actions.Enqueue(() => Check("再開と2倍速", !game.IsPaused && game.Speed == 2, game.Speed));
        Click(635, 350);
        actions.Enqueue(() => Check("実行中成長方針変更", game.Run.Policy == GrowthPolicy.Balanced, game.Run.Policy.ToString()));
        EditorApplication.update += Tick;
        return "v0.2 UI probe started; live 3600 ticks, auto investment, build switching, manual stop, restart retention";
    }
    static int Levels() { int sum = 0; foreach (int n in game.Run.UpgradeLevels) sum += n; return sum; }
    static void Check(string name, bool ok, object detail) { checks.Add(new { name, passed = ok, detail }); passed &= ok; }
    static void Click(float x, float y) { actions.Enqueue(() => Send(EventType.MouseDown, x, y)); actions.Enqueue(() => Send(EventType.MouseUp, x, y)); }
    static void Send(EventType type, float x, float y)
    {
        float scale = Mathf.Min(Screen.width / 1080f, Screen.height / 760f);
        var p = new Vector2((Screen.width - 1080 * scale) / 2 + x * scale, (Screen.height - 760 * scale) / 2 + y * scale);
        var r = (Rect)view.GetType().GetProperty("targetInParent", Flags).GetValue(view);
        p = new Vector2(r.x + p.x * r.width / Screen.width, r.y + p.y * r.height / Screen.height);
        view.SendEvent(new Event { type = type, button = 0, mousePosition = p });
    }
    static void Tick()
    {
        if (EditorApplication.timeSinceStartup < next) return;
        next = EditorApplication.timeSinceStartup + .18;
        try
        {
            if (!EditorApplication.isPlaying || game == null) throw new Exception("Play stopped during probe");
            if (EditorApplication.timeSinceStartup - started > 150) throw new TimeoutException("UI probe exceeded 150 seconds");
            if (actions.Count > 0) { actions.Dequeue()(); return; }
            if (stage == 0)
            {
                if (game.Run.TickCount < 3600) return;
                Check("実Updateで周回継続", game.Run.State == RunStatus.Running, new { game.Run.TickCount, game.Run.Kills, game.Run.Wave });
                Check("自動分岐購入", game.Run.UpgradeLevels[3] > 0, game.Run.UpgradeLevels);
                Check("枝の実成長", game.Run.Branches.Count > 4, game.Run.Branches.Count);
                game.SetPaused(true);
                Click(955, 246);
                actions.Enqueue(() => Check("多枝火力へ切替", game.Run.Build == BuildPreset.Record, game.Run.Build.ToString()));
                Click(635, 246);
                actions.Enqueue(() => { manualLevels = Levels(); manualTick = game.Run.TickCount; Check("手動へ切替", game.Run.Build == BuildPreset.Manual, manualLevels); game.SetPaused(false); });
                stage = 1; return;
            }
            if (stage == 1)
            {
                if (game.Run.TickCount < manualTick + 90) return;
                Check("手動時は自動購入停止", Levels() == manualLevels, Levels());
                game.SetPaused(true);
                // Public restart API verifies retention; this is not a natural defeat.
                game.SetBuild(BuildPreset.Record); game.SetPolicy(GrowthPolicy.Focus);
                game.StartRun(43);
                Check("再出撃でも方針保持", game.Run.Build == BuildPreset.Record && game.Run.Policy == GrowthPolicy.Focus && game.Run.TickCount == 0, new { build = game.Run.Build.ToString(), policy = game.Run.Policy.ToString() });
                game.SetPaused(true);
            }
        }
        catch (Exception e) { Check("exception", false, e.ToString()); }
        EditorApplication.update -= Tick;
        Check("フレーム進行", Time.frameCount > firstFrame, new { firstFrame, endFrame = Time.frameCount });
        File.WriteAllText(Path.GetFullPath("../docs/evidence/bokumyaku-play-v2.json"), Newtonsoft.Json.JsonConvert.SerializeObject(new
        {
            passed, method = "GameView.SendEvent + live Update + public restart API; QA isolated save. No physical input or complete defeat loop.", checks
        }, Newtonsoft.Json.Formatting.Indented));
        game.Meta = original; game.SavePathOverride = originalPath;
        game.SetPaused(true);
    }
}
