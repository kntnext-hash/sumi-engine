using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Bokumyaku.Presentation;
using Bokumyaku.Simulation;
public static class RootSilhouetteCheck
{
 static BokumyakuGame Game(){var g=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();if(g==null||!g.SavePath.Contains("ink-qa-"))throw new Exception("Isolated Play required");return g;}
 public static object Prepare()=>PrepareTicks(900);
 public static object PrepareMature()=>PrepareTicks(6000);
 static object PrepareTicks(int ticks){var g=Game();g.SetBuild(BuildPreset.Farming);if(!g.TryStartRun(42))throw new Exception("Start failed");g.SetPaused(true);for(int i=0;i<ticks&&g.Run.State==RunStatus.Running;i++)g.Run.Tick();SessionState.SetString("RootQA.snapshot",JsonUtility.ToJson(g.Run.CaptureState()));SessionState.SetInt("RootQA.frame",Time.frameCount);return new{g.Run.Wave,g.Run.TickCount};}
 public static object MatureAfter()=>Capture("mature-after");
 public static object MatureBefore()=>Capture("mature-before");
 public static object Before()=>Capture("before");
 public static object After()=>Capture("after");
 static object Capture(string phase)
 {
  var g=Game();var flags=BindingFlags.Instance|BindingFlags.NonPublic;
  int slices=(int)typeof(BokumyakuGame).GetField("drawnRootSlices",flags).GetValue(g);
  if(slices!=35||g.PaintedBranchCount<g.Run.Branches.Count)throw new Exception("Wait for completed paint");
  bool unchanged=SessionState.GetString("RootQA.snapshot","")==JsonUtility.ToJson(g.Run.CaptureState());
  if(!unchanged)throw new Exception("Paused simulation changed");
  string path=Path.GetFullPath("../docs/evidence/root-silhouette-"+phase);
  var result=new{phase,seed=42,g.Run.Wave,g.Run.TickCount,width=Screen.width,height=Screen.height,slices,painted=g.PaintedBranchCount,unchanged,frames=Time.frameCount-SessionState.GetInt("RootQA.frame",Time.frameCount),note="Paused seeded fixture; all root strokes and branches complete. Visual comparison, not runtime performance."};
  File.WriteAllText(path+".json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));ScreenCapture.CaptureScreenshot(path+".png");return result;
 }
}



