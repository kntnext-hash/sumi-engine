using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Bokumyaku.Presentation;
using Bokumyaku.Simulation;
public static class BokumyakuResearchPurchaseCheck
{
    static Queue<Action> steps; static double next; static BokumyakuGame game; static EditorWindow view;
    static int cost; static bool bought,reloaded; static BindingFlags flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    public static object Run()
    {
        game=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();
        if(game==null||!game.SavePath.Contains("ink-qa-"))throw new Exception("Isolated Play required");
        if(game.ResumeAvailable)game.AbandonRun();
        game.Meta.Crystals=100;cost=game.Meta.Cost(MetaUpgrade.Damage);
        game.SetPage(BokumyakuGame.GamePage.Research);
        view=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));view.Focus();
        steps=new Queue<Action>();next=0;
        steps.Enqueue(()=>Send(EventType.MouseDown));steps.Enqueue(()=>Send(EventType.MouseUp));
        steps.Enqueue(()=>{bought=game.Meta.Damage==1&&game.Meta.Crystals==100-cost;ScreenCapture.CaptureScreenshot(Path.GetFullPath("../docs/evidence/research-purchased.png"));});
        steps.Enqueue(()=>{game.TryReloadProgress();reloaded=game.Meta.Damage==1&&game.Meta.Crystals==100-cost;});
        EditorApplication.update+=Tick;return "Purchase by GUI, then reload isolated save";
    }
    static void Send(EventType type)
    {
        var r=(Rect)view.GetType().GetProperty("targetInParent",flags).GetValue(view);
        view.SendEvent(new Event{type=type,button=0,mousePosition=new Vector2(r.x+352f/440*r.width,r.y+367f/(Screen.height/(Screen.width/440f))*r.height)});
    }
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.3;
        if(steps.Count>0){steps.Dequeue()();return;}
        EditorApplication.update-=Tick;
        File.WriteAllText(Path.GetFullPath("../docs/evidence/research-purchase.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new{bought,reloaded,cost,method="Artificial 100 crystal budget in isolated save; real GUI purchase and disk reload"},Newtonsoft.Json.Formatting.Indented));
    }
}
