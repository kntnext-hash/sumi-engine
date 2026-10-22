using System;
using System.IO;
using System.Reflection;
using System.Collections;
using UnityEditor;
using UnityEngine;
using Bokumyaku.Presentation;
using Bokumyaku.Simulation;
public static class BokumyakuCombatInkCheck
{
    static BokumyakuGame game;
    static double start;
    static int kills,frame;
    static BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
    public static object Run()
    {
        game=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();
        if(!game.SavePath.Contains("ink-qa-"))throw new Exception("isolated save required");
        Application.runInBackground=true;EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
        if(game.ResumeAvailable)game.AbandonRun();game.SetWeapon(WeaponKind.Sweep);game.SetBuild(BuildPreset.Farming);game.TryStartRun(42);
        for(int i=0;i<7000&&game.Run.State==RunStatus.Running;i++)game.Run.Tick();
        kills=game.Run.Kills;frame=Time.frameCount;start=EditorApplication.timeSinceStartup;
        EditorApplication.update+=Watch;return "Watch real attacks/defeats for up to 30 seconds";
    }
    static void Watch()
    {
        if(EditorApplication.timeSinceStartup-start<1)return;
        var bursts=(IList)typeof(BokumyakuGame).GetField("inkBursts",flags).GetValue(game);
        var flashes=(IList)typeof(BokumyakuGame).GetField("flashes",flags).GetValue(game);
        bool seen=game.ObservedInkBursts>0&&bursts.Count>0&&flashes.Count>0;
        if(!seen&&EditorApplication.timeSinceStartup-start<30)return;
        EditorApplication.update-=Watch;
        game.SetPaused(true);
        // Keep the last observed transient frame visible for screenshot review only.
        foreach(var b in bursts)b.GetType().GetField("Life").SetValue(b,.5f);
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("../docs/evidence/bokumyaku-combat-ink.png"));
        File.WriteAllText(Path.GetFullPath("../docs/evidence/bokumyaku-combat-ink.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=seen&&Time.frameCount>frame&&game.Run.Kills>kills,game.ObservedInkBursts,activeBursts=bursts.Count,activeAttacks=flashes.Count,killDelta=game.Run.Kills-kills,frameDelta=Time.frameCount-frame,method="Natural Update after accelerated fixture; transient held for screenshot; limited check"},Newtonsoft.Json.Formatting.Indented));
    }
}
