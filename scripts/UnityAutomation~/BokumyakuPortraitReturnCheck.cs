using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Bokumyaku.Presentation;
public static class BokumyakuPortraitReturnCheck
{
    static Queue<Action> actions;
    static List<object> checks;
    static double next;
    static BokumyakuGame game;
    static EditorWindow view;
    static BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    public static object Run()
    {
        game=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();
        if(!game.SavePath.Contains("ink-qa-"))throw new Exception("Isolated save required");
        view=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));view.Focus();
        actions=new Queue<Action>();checks=new List<object>();next=0;
        foreach(var page in new[]{BokumyakuGame.GamePage.Research,BokumyakuGame.GamePage.Records})
        {
            var target=page;
            actions.Enqueue(()=>{typeof(BokumyakuGame).GetField("portraitDetails",flags).SetValue(game,true);game.SetPage(target);});
            actions.Enqueue(()=>ScreenCapture.CaptureScreenshot(Path.GetFullPath("../docs/evidence/portrait-"+target+".png")));
            actions.Enqueue(()=>Send(EventType.MouseDown));actions.Enqueue(()=>Send(EventType.MouseUp));
            actions.Enqueue(()=>checks.Add(new{page=target.ToString(),passed=game.Page==BokumyakuGame.GamePage.Battle&&!(bool)typeof(BokumyakuGame).GetField("portraitDetails",flags).GetValue(game)&&!game.IsPaused}));
        }
        EditorApplication.update+=Tick;return "Two return-button checks";
    }
    static void Send(EventType type)
    {
        var r=(Rect)view.GetType().GetProperty("targetInParent",flags).GetValue(view);
        var p=new Vector2(r.x+348f/440*r.width,r.y+35f/(Screen.height/(Screen.width/440f))*r.height);
        view.SendEvent(new Event{type=type,button=0,mousePosition=p});
    }
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.2;
        if(actions.Count>0){actions.Dequeue()();return;}
        EditorApplication.update-=Tick;game.SetPaused(true);
        File.WriteAllText(Path.GetFullPath("../docs/evidence/portrait-return-check.json"),Newtonsoft.Json.JsonConvert.SerializeObject(checks,Newtonsoft.Json.Formatting.Indented));
    }
}
