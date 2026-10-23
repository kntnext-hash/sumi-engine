using System;using System.IO;using System.Linq;using UnityEngine;using UnityEngine.UI;using TMPro;using Bokumyaku.Presentation;using Bokumyaku.Simulation;
public static class PlayStyleUiCheck {
public static object Run(){
var g=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();if(g==null||!g.SavePath.Contains("ink-qa-"))throw new Exception("Isolated QA required");
var menu=UnityEngine.Object.FindFirstObjectByType<BokumyakuMenuHud>();
var buttons=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None);
var farm=buttons.Single(b=>b.name=="MenuFarmStyle");var record=buttons.Single(b=>b.name=="MenuRecordStyle");
var policy=g.SelectedPolicy;bool repeat=g.AutoRepeatEnabled;int bank=g.Meta.Crystals;
record.onClick.Invoke();if(g.SelectedWeapon!=WeaponKind.Enso||g.SelectedBuild!=BuildPreset.Record)throw new Exception("Record selection failed");
farm.onClick.Invoke();if(g.SelectedWeapon!=WeaponKind.Point||g.SelectedBuild!=BuildPreset.Farming)throw new Exception("Farm selection failed");
if(g.SelectedPolicy!=policy||g.AutoRepeatEnabled!=repeat||g.Meta.Crystals!=bank)throw new Exception("Unrelated state changed");
if(!g.TryStartRun(42))throw new Exception("Start failed");g.SetPage(BokumyakuGame.GamePage.Home);menu.Refresh();
string before=JsonUtility.ToJson(g.Run.CaptureState());record.onClick.Invoke();
if(farm.interactable||record.interactable||before!=JsonUtility.ToJson(g.Run.CaptureState()))throw new Exception("Active run guard failed");
Canvas.ForceUpdateCanvases();foreach(var b in new[]{farm,record}){var t=b.GetComponentInChildren<TextMeshProUGUI>();t.ForceMeshUpdate();if(t.isTextOverflowing)throw new Exception("Text overflow");}
var report=new{width=Screen.width,height=Screen.height,recordSelect=true,farmSelect=true,policyAndRepeatPreserved=true,activeRunGuard=true,textOverflow=false,method="Isolated Editor Play uGUI callbacks; physical pointer not tested"};
File.WriteAllText(Path.GetFullPath("../docs/evidence/play-style-ui.json"),Newtonsoft.Json.JsonConvert.SerializeObject(report));return report;
}
public static object Capture(){ScreenCapture.CaptureScreenshot(Path.GetFullPath("../docs/evidence/play-style-ui.png"));return Time.frameCount;}
}
