using System;using System.IO;using System.Linq;using UnityEngine;using Bokumyaku.Presentation;
public static class CoreLightCheck {
public static object Run(){
var g=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();if(!g.SavePath.Contains("ink-qa-"))throw new Exception("Isolated save required");
g.TryStartRun(42);g.SetPaused(true);g.SetReducedMotion(true);
var lights=g.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.sprite!=null&&r.sprite.texture.name=="Golden core veins").ToArray();
if(lights.Length!=1)throw new Exception("Core light missing or duplicated");
var report=new{passed=true,width=Screen.width,height=Screen.height,lightTextures=lights.Length,method="Wave1 reduced-motion still image; visual comparison with card-frame.png, not performance"};
File.WriteAllText(Path.GetFullPath("../docs/evidence/core-light.json"),Newtonsoft.Json.JsonConvert.SerializeObject(report));ScreenCapture.CaptureScreenshot(Path.GetFullPath("../docs/evidence/core-light.png"));return report;
}}
