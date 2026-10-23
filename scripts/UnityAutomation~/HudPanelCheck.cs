using System;using System.IO;using System.Linq;using UnityEngine;using UnityEngine.UI;using TMPro;using Bokumyaku.Presentation;
public static class HudPanelCheck {
public static object Run(){
var g=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();if(!g.SavePath.Contains("ink-qa-"))throw new Exception("Isolated save required");
g.TryStartRun(42);g.SetPaused(true);
var h=UnityEngine.Object.FindFirstObjectByType<BokumyakuBattleHud>();
typeof(BokumyakuBattleHud).GetMethod("Refresh",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(h,null);
var edges=h.GetComponentsInChildren<Image>().Where(x=>x.name=="BrushEdge").ToArray();
if(edges.Length!=5||edges.Any(x=>x.sprite.name!="card_ink_gold_04"||x.fillCenter||x.type!=Image.Type.Sliced))throw new Exception("Frame setup");
var panels=h.GetComponentsInChildren<Image>().Where(x=>x.sprite!=null && x.sprite.name=="hud_ink_panel_06").ToArray(); if(panels.Length<4||panels.Any(x=>x.type!=Image.Type.Sliced||!x.fillCenter||x.color!=Color.white))throw new Exception("Ink panel setup"); Canvas.ForceUpdateCanvases();foreach(var t in h.GetComponentsInChildren<TextMeshProUGUI>()){t.ForceMeshUpdate();if(t.isTextOverflowing)throw new Exception("Overflow "+t.name);}
var report=new{passed=true,width=Screen.width,height=Screen.height,frames=edges.Length,method="Five disabled cards at zero ink; visual frame and text check"};
File.WriteAllText(Path.GetFullPath("../docs/evidence/hud-panel.json"),Newtonsoft.Json.JsonConvert.SerializeObject(report));ScreenCapture.CaptureScreenshot(Path.GetFullPath("../docs/evidence/hud-panel.png"));return report;
}}
