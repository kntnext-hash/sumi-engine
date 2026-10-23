using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Bokumyaku.Presentation;
using Bokumyaku.Simulation;
public static class BokumyakuRenderBenchmark
{
 static BokumyakuGame g;static double start;static int lastFrame,count,initialKills,oldFps,oldSync,startFull;static double sum,max;static string label;static bool started;
 public static object Before()=>Begin("before");
 public static object After()=>Begin("after");
 static object Begin(string phase)
 {
  g=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();if(g==null||!g.SavePath.Contains("ink-qa-"))throw new Exception("Isolated Play required");
  if(g.ResumeAvailable)g.AbandonRun();g.SetWeapon(WeaponKind.Point);g.SetBuild(BuildPreset.Farming);g.TryStartRun(42);
  for(int i=0;i<6000;i++)g.Run.Tick();g.SetPaused(true);
  Application.runInBackground=true;EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
  label=phase;oldFps=Application.targetFrameRate;oldSync=QualitySettings.vSyncCount;Application.targetFrameRate=60;QualitySettings.vSyncCount=0;

  start=EditorApplication.timeSinceStartup;lastFrame=Time.frameCount;count=0;sum=max=0;initialKills=g.Run.Kills;startFull=g.InkFullInitializations;started=false;
  EditorApplication.update+=Poll;return new{phase,durationSeconds=180,seed=42,wave=g.Run.Wave};
 }
 static void Poll()
 {
  if(g==null||!EditorApplication.isPlaying){EditorApplication.update-=Poll;return;}
  if(!started){if(EditorApplication.timeSinceStartup-start<5||ShaderUtil.anythingCompiling)return;ScreenCapture.CaptureScreenshot(Path.GetFullPath("../docs/evidence/render-"+label+"-wave17.png"));started=true;g.SetPaused(false);start=EditorApplication.timeSinceStartup;lastFrame=Time.frameCount;return;}
  if(g.IsPaused && g.Run.State==RunStatus.Running)g.SetPaused(false); // QA only: editor focus changes must not silently pause the sample.
  if(Time.frameCount!=lastFrame){lastFrame=Time.frameCount;double ms=Time.unscaledDeltaTime*1000;sum+=ms;max=Math.Max(max,ms);count++;}
  if(EditorApplication.timeSinceStartup-start<180)return;
  EditorApplication.update-=Poll;g.SetPaused(true);Application.targetFrameRate=oldFps;QualitySettings.vSyncCount=oldSync;
  var result=new{phase=label,durationSeconds=EditorApplication.timeSinceStartup-start,frames=count,averageMs=sum/Math.Max(1,count),maximumMs=max,width=Screen.width,height=Screen.height,seed=42,startWave=17,endWave=g.Run.Wave,kills=g.Run.Kills-initialKills,tick=g.Run.TickCount,branchRebuilds=g.BranchOverlayRebuilds,dirtyUploads=g.DirtyInkUploads,dirtyPixels=g.DirtyInkPixels,fullInitializations=g.InkFullInitializations,fullUpdatesDuringSample=g.InkFullInitializations-startFull,spriteDeaths=g.SpriteDeaths,spriteHits=g.SpriteHits,interpolatedFrames=g.SpriteInterpolatedFrames,method="Editor wall-frame intervals, target60, 5s+shader warmup, 180s; not GPU profiler/device performance"};
  File.WriteAllText(Path.GetFullPath("../docs/evidence/render-"+label+"-timing.json"),Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
 }
}




