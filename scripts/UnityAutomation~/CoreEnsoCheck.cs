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
public static class CoreEnsoCheck
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
    static void AssertGauge(int hp,bool enabled)
    {
        Refresh();CheckText();
        var h=Hud();var button=h.GetComponentsInChildren<UnityEngine.UI.Button>().Single(x=>x.name=="Repair");
        var ring=h.GetComponentsInChildren<UnityEngine.UI.Image>().Single(x=>x.name=="CoreRingFill");
        var text=h.GetComponentsInChildren<TextMeshProUGUI>().Single(x=>x.name=="CoreValue");
        var rect=((RectTransform)button.transform).rect;
        var brush=button.transform.Find("CoreBrushRim").GetComponent<UnityEngine.UI.Image>();
        if(brush.sprite==null||brush.sprite.name!="core_enso_09"||brush.raycastTarget||!brush.preserveAspect)throw new Exception("Brush frame binding");
        if(brush.sprite.texture.width>512||brush.sprite.texture.isReadable)throw new Exception("Brush import settings");
        var disc=h.GetComponentInChildren<Bokumyaku.Presentation.Drawing.UiDiscGraphic>();
        if(disc.canvasRenderer==null)throw new Exception("Gauge backing renderer missing");
        if(Mathf.Abs(rect.width-rect.height)>.1f||text.text!=hp.ToString()||Mathf.Abs(ring.fillAmount-hp/10f)>.001f||button.interactable!=enabled)throw new Exception("Gauge mismatch "+hp);
        if(hp<=3&&text.color.r<=text.color.g)throw new Exception("Low HP color");
    }
    public static object Run()
    {
        var g=Game();AssertGauge(10,false);
        int ink=g.Run.RunInk;Value("CoreHp",3);Value("RunInk",0);AssertGauge(3,false);
        Value("RunInk",ink);AssertGauge(3,true);
        int cost=g.Run.RepairCost;Click("Repair");AssertGauge(5,true);
        if(g.Run.RunInk!=ink-cost||g.Run.RepairCount!=1)throw new Exception("Repair price/count");
        typeof(RunSimulator).GetField("repairCount",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(g.Run,5);AssertGauge(5,false);
        typeof(RunSimulator).GetField("repairCount",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(g.Run,1);Refresh();
        var result=new{passed=true,width=Screen.width,height=Screen.height,fullDisabled=true,lowEmptyDisabled=true,radialAmounts=true,repairRaycast=true,hpAfter=g.Run.CoreHp,cost,limitDisabled=true,method="Isolated paused fixture; reflection creates damaged/empty/capped conditions. Repair invoked through screen raycast and EventSystem. No simulation code changes."};
        var path=Path.GetFullPath("../docs/evidence/core-enso-"+Screen.width);
        File.WriteAllText(path+".json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));ScreenCapture.CaptureScreenshot(path+".png");return result;
    }
}

