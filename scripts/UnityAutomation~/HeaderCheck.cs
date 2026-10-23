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
public static class HeaderCheck
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
    static void Value(string property,int value)=>typeof(RunSimulator).GetProperty(property).SetValue(Game().Run,value);
    public static object Large()
    {
        var g=Game();Refresh();CheckText();
        SessionState.SetString("HeaderQA.snapshot",JsonUtility.ToJson(g.Run.CaptureState()));
        SessionState.SetInt("HeaderQA.wave",g.Run.Wave);SessionState.SetInt("HeaderQA.ink",g.Run.RunInk);SessionState.SetInt("HeaderQA.crystals",g.Meta.Crystals);
        Click("Pause");if(g.IsPaused)throw new Exception("Resume callback");Click("Pause");if(!g.IsPaused)throw new Exception("Pause callback");
        Value("Wave",100000);Value("RunInk",int.MaxValue);g.Meta.Crystals=int.MaxValue;Refresh();CheckText();
        var texts=Hud().GetComponentsInChildren<TextMeshProUGUI>();
        if(texts.Single(t=>t.name=="Wave").text!="100000"||texts.Single(t=>t.name=="Ink").text!=int.MaxValue.ToString("N0")||texts.Single(t=>t.name=="Crystals").text!=int.MaxValue.ToString("N0"))throw new Exception("Large amount display");
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("../docs/evidence/header-large-"+Screen.width+".png"));
        return "Large values displayed; restore with Normal before stopping";
    }
    public static object Normal()
    {
        var g=Game();Value("Wave",SessionState.GetInt("HeaderQA.wave",5));Value("RunInk",SessionState.GetInt("HeaderQA.ink",0));g.Meta.Crystals=SessionState.GetInt("HeaderQA.crystals",0);Refresh();CheckText();
        if(JsonUtility.ToJson(g.Run.CaptureState())!=SessionState.GetString("HeaderQA.snapshot",""))throw new Exception("Snapshot changed");
        var result=new{passed=true,width=Screen.width,height=Screen.height,waveLimit=100000,resourceLimit=int.MaxValue,pauseRaycast=true,unchanged=true,method="Isolated paused fixture; full numbers without abbreviation, glyph/overflow checked. Reflection supplies display limits, then restores original state; no long-wave balance claim."};
        var path=Path.GetFullPath("../docs/evidence/header-normal-"+Screen.width);File.WriteAllText(path+".json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));ScreenCapture.CaptureScreenshot(path+".png");return result;
    }
}