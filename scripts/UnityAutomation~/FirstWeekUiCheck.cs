using System;using System.IO;using System.Linq;using UnityEngine;using UnityEngine.UI;using TMPro;using Bokumyaku.Presentation;using Bokumyaku.Persistence;using Bokumyaku.Simulation;
public static class FirstWeekUiCheck {
public static object Run(){
var g=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();if(g==null||!g.SavePath.Contains("ink-qa-"))throw new Exception("Isolated QA required");
g.SavePathOverride=Path.Combine(Path.GetDirectoryName(g.SavePath),"research-v11.json");
var c=new CampaignProgress();c.Meta.Crystals=500;CampaignSaveStore.Save(g.PreviousV10CampaignPath,c);
var original=File.ReadAllBytes(g.PreviousV10CampaignPath);
File.WriteAllText(g.PreviousV10CampaignPath+".audio.json","{\"Master\":0.25,\"Muted\":true}");
if(!g.TryReloadProgress())throw new Exception("Reload failed");g.ReloadAudioSettings();
if(!g.AudioSettings.Muted||g.AudioSettings.Master!=.25f)throw new Exception("Audio migration failed");
g.SetPage(BokumyakuGame.GamePage.Research);
var menu=UnityEngine.Object.FindFirstObjectByType<BokumyakuMenuHud>();menu.Refresh();
var button=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(b=>b.name=="MenuResearch0");
string before=button.GetComponentInChildren<TextMeshProUGUI>().text;if(!before.Contains("500")||!button.interactable)throw new Exception("Wrong initial price");
button.onClick.Invoke();menu.Refresh();string after=button.GetComponentInChildren<TextMeshProUGUI>().text;
if(g.Meta.Damage!=1||g.Meta.Crystals!=0||!after.Contains("740")||button.interactable)throw new Exception("Purchase failed");
if(!g.TryReloadProgress()||g.Meta.Damage!=1||g.Meta.Crystals!=0)throw new Exception("Persistence failed");
if(!original.SequenceEqual(File.ReadAllBytes(g.PreviousV10CampaignPath)))throw new Exception("Source changed");
g.SetPage(BokumyakuGame.GamePage.Research);menu.Refresh();Canvas.ForceUpdateCanvases();
var report=new{before,after,damage=g.Meta.Damage,crystals=g.Meta.Crystals,multiplier=g.Meta.DamageMultiplier,width=Screen.width,height=Screen.height,sourcePreserved=true,audioMigrated=true,method="Isolated Editor Play; synthetic 500 crystal fixture; actual uGUI button callback and disk reload"};
File.WriteAllText(Path.GetFullPath("../docs/evidence/first-week-research-ui.json"),Newtonsoft.Json.JsonConvert.SerializeObject(report));
return report;
}
public static object Capture(){ScreenCapture.CaptureScreenshot(Path.GetFullPath("../docs/evidence/first-week-research-ui.png"));return new{frame=Time.frameCount,width=Screen.width,height=Screen.height};}
}
