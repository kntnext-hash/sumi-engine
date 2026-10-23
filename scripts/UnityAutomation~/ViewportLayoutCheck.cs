using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using Bokumyaku.Presentation;
public static class ViewportLayoutCheck
{
    public static object Run()
    {
        var game=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();
        if(game==null||!game.SavePath.Contains("ink-qa-"))throw new Exception("Isolated Play required");
        var portrait=new Rect(0,0,360,800);
        if(BokumyakuViewport.Fit(portrait)!=portrait)throw new Exception("Portrait changed");
        var inset=new Rect(30,40,1000,700);var fit=BokumyakuViewport.Fit(inset);
        if(fit.xMin<inset.xMin||fit.xMax>inset.xMax||fit.yMin!=inset.yMin||fit.yMax!=inset.yMax||Mathf.Abs(fit.center.x-inset.center.x)>.01f)throw new Exception("Safe area containment");
        Canvas.ForceUpdateCanvases();
        var area=BokumyakuViewport.Current;
        foreach(var type in new[]{typeof(BokumyakuBattleHud),typeof(BokumyakuMenuHud)})
        {
            var hud=UnityEngine.Object.FindFirstObjectByType(type);
            var safe=(RectTransform)type.GetField("safe",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(hud);
            var corners=new Vector3[4];safe.GetWorldCorners(corners);
            if(Vector2.Distance(corners[0],area.min)>.6f||Vector2.Distance(corners[2],area.max)>.6f)throw new Exception("UI bounds mismatch "+type.Name);
            if(Mathf.Abs(safe.rect.width-440)>.5f)throw new Exception("Logical width mismatch "+type.Name);
        }
        var battle=BokumyakuBattleHud.GetBattleViewport();
        if(Mathf.Abs(battle.x*Screen.width-area.x)>.5f||Mathf.Abs(battle.width*Screen.width-area.width)>.5f)throw new Exception("Camera/HUD mismatch");
        var r=new{passed=true,width=Screen.width,height=Screen.height,fitted=area.ToString(),battle=battle.ToString(),portraitUnchanged=true,safeInsetContained=true,logicalWidth=440};
        File.WriteAllText(Path.GetFullPath("../docs/evidence/viewport-layout-"+Screen.width+".json"),Newtonsoft.Json.JsonConvert.SerializeObject(r,Newtonsoft.Json.Formatting.Indented));return r;
    }
}
