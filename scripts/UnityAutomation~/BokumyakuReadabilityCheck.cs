using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Bokumyaku.Presentation;
using Bokumyaku.Simulation;
public static class BokumyakuReadabilityCheck
{
    static BokumyakuGame game;
    static List<object> rows;
    static int weapon,frame;
    static double start;
    static bool passed;
    static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    public static object Run()
    {
        game=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();
        if(game==null||!game.SavePath.Contains("ink-qa-"))throw new Exception("Isolated Play required");
        rows=new List<object>();weapon=0;passed=true;
        Application.runInBackground=true;EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
        Begin();EditorApplication.update+=Poll;return "Six short live weapon samples, each <=12 seconds";
    }
    static void Begin()
    {
        if(game.ResumeAvailable)game.AbandonRun();
        game.SetWeapon((WeaponKind)weapon);game.SetBuild(BuildPreset.Farming);
        if(!game.TryStartRun(42))throw new Exception("Could not start isolated run");
        for(int i=0;i<6000&&game.Run.State==RunStatus.Running;i++)game.Run.Tick();
        game.SetPaused(false);frame=Time.frameCount;start=EditorApplication.timeSinceStartup;
    }
    static void Poll()
    {
        var flashes=(IList)typeof(BokumyakuGame).GetField("flashes",Flags).GetValue(game);
        bool ready=flashes.Count>0&&Time.frameCount-frame>=10;
        if(!ready&&EditorApplication.timeSinceStartup-start<12)return;
        passed &=ready;
        rows.Add(new{weapon=((WeaponKind)weapon).ToString(),attackVisible=ready,frames=Time.frameCount-frame,game.Run.Wave,activeAttacks=flashes.Count});
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("../docs/evidence/readability-"+((WeaponKind)weapon)+".png"));
        game.SetPaused(true);EditorApplication.update-=Poll;
        // Let the capture render before resetting the run.
        start=EditorApplication.timeSinceStartup;EditorApplication.update+=Next;
    }
    static void Next()
    {
        if(EditorApplication.timeSinceStartup-start<.5)return;
        EditorApplication.update-=Next;
        if(++weapon<6){Begin();EditorApplication.update+=Poll;return;}
        File.WriteAllText(Path.GetFullPath("../docs/evidence/readability-six-weapons.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new{passed,rows,method="Short live samples after accelerated seed42 fixture; not balance/full regression/device test"},Newtonsoft.Json.Formatting.Indented));
    }
}
