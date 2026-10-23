using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Bokumyaku.Presentation;
using Bokumyaku.Simulation;
public static class BokumyakuRecordsCheck
{
 static BokumyakuGame game;static double start;static int wave;
 public static object Run(){
 game=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();if(!game.SavePath.Contains("ink-qa-"))throw new Exception("isolated only");
 game.SetAutoRepeat(false);game.SetBuild(BuildPreset.Farming);game.TryStartRun(42);
 for(int i=0;i<60000&&game.Run.State==RunStatus.Running;i++)game.Run.Tick();
 if(game.Run.State==RunStatus.Running)throw new Exception("No result in bounded simulation");
 wave=game.Run.Wave;game.SaveProgress();game.TryReloadProgress();game.SetPage(BokumyakuGame.GamePage.Records);
 start=EditorApplication.timeSinceStartup;EditorApplication.update+=Finish;return "settled and reloaded real simulated defeat";
 }
 static void Finish(){
 if(EditorApplication.timeSinceStartup-start<.5)return;EditorApplication.update-=Finish;
 var campaign=game.Campaign;
 var result=new{runs=campaign.TotalRuns,history=campaign.History.Count,crystals=game.Meta.Crystals,wave,passed=campaign.TotalRuns==1&&campaign.History.Count==1&&campaign.LastResult!=null&&!campaign.HasActiveRun,method="Bounded normal simulation ticks to defeat, SaveProgress, disk reload, portrait records display; not real-time duration test"};
 File.WriteAllText(Path.GetFullPath("../docs/evidence/records-populated.json"),Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
 ScreenCapture.CaptureScreenshot(Path.GetFullPath("../docs/evidence/records-populated.png"));
 }
}
