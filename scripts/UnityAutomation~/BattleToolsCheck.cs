using System;
using System.IO;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using Bokumyaku.Presentation;
using Bokumyaku.Simulation;
public static class BattleToolsCheck
{
    static int clicks;
    static BokumyakuGame Game()
    {
        var game=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();
        if(game==null||!game.SavePath.Contains("ink-qa-"))throw new Exception("Isolated Play required");
        return game;
    }
    static BokumyakuBattleHud Hud()=>UnityEngine.Object.FindFirstObjectByType<BokumyakuBattleHud>();
    static void Refresh()=>typeof(BokumyakuBattleHud).GetMethod("Refresh",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(Hud(),null);
    static void CheckText()
    {
        Canvas.ForceUpdateCanvases();
        foreach(var t in Hud().GetComponentsInChildren<TextMeshProUGUI>())
        {
            t.ForceMeshUpdate();
            if(t.isTextOverflowing)throw new Exception("Overflow: "+t.name);
            if(!t.font.HasCharacters(t.text))throw new Exception("Missing glyph: "+t.text);
        }
    }
    static void Click(string name)
    {
        Canvas.ForceUpdateCanvases();
        var button=Hud().GetComponentsInChildren<UnityEngine.UI.Button>().Single(x=>x.name==name);
        if(!button.interactable)throw new Exception("Disabled: "+name);
        var rect=(RectTransform)button.transform;
        var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center)),button=PointerEventData.InputButton.Left};
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
        if(hits.Count==0||ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)!=button.gameObject)throw new Exception("Blocked raycast: "+name+" hits="+string.Join(",",hits.Select(x=>x.gameObject.name)));
        ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
        clicks++;
        Refresh();CheckText();
    }
    static bool Visible(string name)=>Hud().GetComponentsInChildren<RectTransform>().Any(x=>x.name==name);
    public static object Prepare()
    {
        var g=Game();g.SetBuild(BuildPreset.Manual);if(!g.TryStartRun(42))throw new Exception("Start");g.SetPaused(true);
        for(int i=0;i<1800;i++)g.Run.Tick();
        Application.runInBackground=true;
        SessionState.SetInt("ToolsQA.frame",Time.frameCount);
        Refresh();return new{g.Run.Wave,g.Run.TickCount};
    }
    static IEnumerator steps;
    static int lastFrame;
    static double deadline;
    public static object Run()
    {
        if(steps!=null)throw new Exception("Already running");
        steps=Steps();lastFrame=Time.frameCount;deadline=EditorApplication.timeSinceStartup+90;
        EditorApplication.update+=Pump;return "Started frame-separated pointer checks";
    }
    static void Pump()
    {
        try
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Frame check timed out");
            if(Time.frameCount<=lastFrame+1)return;
            lastFrame=Time.frameCount;
            if(steps.MoveNext())return;
        }
        catch(Exception e)
        {
            File.WriteAllText(Path.GetFullPath("../docs/evidence/battle-tools-"+Screen.width+".json"),Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=false,error=e.ToString()}));
        }
        EditorApplication.update-=Pump;steps=null;
    }
    static IEnumerator Steps()
    {
        clicks=0;var g=Game();Refresh();CheckText();
        if(Visible("BattleToolsPanel")||Visible("UpgradeHelp")||Visible("ExtraUpgrades"))throw new Exception("Closed HUD obscured");
        string before=JsonUtility.ToJson(g.Run.CaptureState());
        int oldSpeed=g.Speed;Click("Speed");yield return null;if(g.Speed==oldSpeed)throw new Exception("Speed");Click("Speed");yield return null;
        bool oldAuto=g.AutoRepeatEnabled;Click("Auto");yield return null;if(g.AutoRepeatEnabled==oldAuto)throw new Exception("Auto");Click("Auto");yield return null;
        Click("BattleTools");yield return null;if(!Visible("BattleToolsPanel"))throw new Exception("Open");
        Click("Save");yield return null;if(!File.Exists(g.SavePath))throw new Exception("Save file");
        bool reduced=g.ReducedMotion;Click("ReducedMotion");yield return null;if(g.ReducedMotion==reduced)throw new Exception("Motion");Click("ReducedMotion");yield return null;
        Click("UpgradeHelp");yield return null;if(!Visible("UpgradeHelpPanel")||Visible("BattleToolsPanel"))throw new Exception("Help navigation");Click("BackToTools");yield return null;
        Click("ExtraUpgrades");yield return null;if(!Visible("ExtraUpgradePanel")||Visible("BattleToolsPanel"))throw new Exception("Extra navigation");Click("BackToTools");yield return null;
        Click("CloseTools");yield return null;Click("BattleTools");yield return null;Click("BattleTools");yield return null;
        if(Visible("BattleToolsPanel")||Visible("ExtraUpgradePanel")||Visible("UpgradeHelpPanel"))throw new Exception("Close");
        Click("BattleTools");yield return null;Click("Home");yield return null;if(g.Page!=BokumyakuGame.GamePage.Home)throw new Exception("Home");
        g.SetPage(BokumyakuGame.GamePage.Battle);Refresh();
        if(before!=JsonUtility.ToJson(g.Run.CaptureState()))throw new Exception("Simulation changed");
        int frames=Time.frameCount-SessionState.GetInt("ToolsQA.frame",Time.frameCount);
        if(frames<35)throw new Exception("Frames have not advanced");
        var r=new{passed=true,width=Screen.width,height=Screen.height,frames,raycastClicks=clicks,unchanged=true,method="Isolated paused Wave5 fixture. Raycast + EventSystem pointer clicks, visible TMP glyph/overflow checks; desktop Editor only."};
        var path=Path.GetFullPath("../docs/evidence/battle-tools-"+Screen.width);
        File.WriteAllText(path+".json",Newtonsoft.Json.JsonConvert.SerializeObject(r,Newtonsoft.Json.Formatting.Indented));ScreenCapture.CaptureScreenshot(path+".png");yield break;
    }
    public static object Panel()
    {
        Game();Click("BattleTools");
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("../docs/evidence/battle-tools-open-"+Screen.width+".png"));
        return "Open panel captured";
    }
}
