using System;
using System.IO;
using Bokumyaku.Presentation;
using UnityEngine;

public static class StartSpeedCheck
{
    public static object Run()
    {
        var game = UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();
        if (game == null || !game.SavePath.Contains("ink-qa-"))
            throw new Exception("Isolated Play save required");
        game.SetSpeed(2);
        if (!game.SaveBuildSlot(0)) throw new Exception("Save slot failed");
        game.SetSpeed(1);
        if (!game.LoadBuildSlot(0) || game.Speed != 2) throw new Exception("Slot speed lost");
        if (!game.TryStartRun(42) || game.Speed != 2) throw new Exception("Start speed lost");
        game.SetPaused(true);
        if (!game.SaveProgress() || !game.TryReloadProgress() || game.Speed != 2)
            throw new Exception("Resume speed lost");
        if (!game.AbandonRun()) throw new Exception("Abandon failed");
        game.SetSpeed(1);
        if (!game.TryStartRun(73) || game.Speed != 1) throw new Exception("Normal speed lost");
        game.SetPaused(true);
        var report = new { passed = true, slotSpeed = 2, startSpeed = 2, reloadSpeed = 2, normalStartSpeed = 1,
            method = "Isolated Editor Play API check; not physical input or Player build" };
        File.WriteAllText(Path.GetFullPath("../docs/evidence/start-speed.json"),
            Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
        return report;
    }
}
