using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Bokumyaku.Presentation;
public static class BokumyakuPortraitCheck
{
    static Queue<Action> queue;
    static List<object> checks;
    static bool passed;
    static double next;
    static BokumyakuGame game;
    static EditorWindow view;
    static BindingFlags flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static void Check(string name,bool ok){checks.Add(new{name,passed=ok});passed &=ok;}
    static void Click(float x,float y){queue.Enqueue(()=>Send(EventType.MouseDown,x,y));queue.Enqueue(()=>Send(EventType.MouseUp,x,y));}
    static void Send(EventType type,float x,float y)
    {
        var target=(Rect)view.GetType().GetProperty("targetInParent",flags).GetValue(view);
        float s=Screen.width/440f;
        var point=new Vector2(target.x+x*s*target.width/Screen.width,target.y+y*s*target.height/Screen.height);
        view.SendEvent(new Event{type=type,button=0,mousePosition=point});
    }
    public static object Run()
    {
        game=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();
        if(game==null || !EditorApplication.isPlaying || !game.SavePath.Contains("portrait-v12"))throw new Exception("Isolated portrait Play required");
        view=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));view.Focus();
        queue=new Queue<Action>();checks=new List<object>();passed=true;next=0;game.SetPaused(true);game.SetSpeed(1);game.SetAutoRepeat(false);
        float h=Screen.height/(Screen.width/440f);
        int ink=game.Run.RunInk, level=game.Run.UpgradeLevels[0], cost=game.Run.UpgradeCost(Bokumyaku.Simulation.UpgradeKind.Damage);
        Click(62,h-67);queue.Enqueue(()=>Check("buy",game.Run.UpgradeLevels[0]==level+1&&game.Run.RunInk==ink-cost));
        Click(192,h-138);queue.Enqueue(()=>Check("speed",game.Speed==2));
        Click(290,h-138);queue.Enqueue(()=>Check("repeat",game.AutoRepeatEnabled));
        Click(370,40);queue.Enqueue(()=>Check("resume",!game.IsPaused));
        Click(370,40);queue.Enqueue(()=>Check("pause",game.IsPaused));
        Click(388,h-138);queue.Enqueue(()=>Check("details",(bool)typeof(BokumyakuGame).GetField("portraitDetails",flags).GetValue(game)));
        EditorApplication.update+=Tick;return "Started 6 short checks";
    }
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.2;
        if(queue.Count>0){queue.Dequeue()();return;}
        EditorApplication.update-=Tick;
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("../docs/evidence/bokumyaku-portrait-details-v12.png"));
        File.WriteAllText(Path.GetFullPath("../docs/evidence/bokumyaku-portrait-v12.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new{passed,width=Screen.width,height=Screen.height,checks,method="GameView frame-separated synthetic clicks; isolated save; not device or full regression"},Newtonsoft.Json.Formatting.Indented));
        game.SetAutoRepeat(false);game.SetPaused(true);
    }
}
