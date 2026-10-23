using System;using System.IO;using System.Linq;using UnityEngine;using Bokumyaku.Presentation;using Bokumyaku.Persistence;using Bokumyaku.Simulation;
public static class LandscapeDepthCheck
{
  public static object DenseNight()
 {
  Show(2);var g=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();
  var spawn=typeof(RunSimulator).GetMethod("Spawn",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
  for(int i=0;i<21;i++){
   spawn.Invoke(g.Run,new object[]{(EnemyKind?)(EnemyKind)(i%7)});
   var e=g.Run.Enemies.Last();typeof(EnemyState).GetProperty("X").SetValue(e,(double)(-320+(i%7)*105));
   typeof(EnemyState).GetProperty("Y").SetValue(e,(double)(350-(i/7)*210));
  }
  UnityEditor.SessionState.SetString("Landscape.snapshot",JsonUtility.ToJson(g.Run.CaptureState()));
  return new{enemies=g.Run.Enemies.Count,method="Forced silhouette grid, not natural encounter"};
 }
 public static object Day()=>Show(1);
 public static object Night()=>Show(2);
 public static object Red()=>Show(3);
 static object Show(int tier)
 {
  var g=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();if(g==null||!g.SavePath.Contains("ink-qa-"))throw new Exception("Isolated QA required");
  string path=Path.Combine(Path.GetDirectoryName(g.SavePath),"landscape-"+tier+"-"+Guid.NewGuid().ToString("N")+".json");
  var c=new CampaignProgress{BestWave=100,ClaimedMilestoneWave=100,BestWaveByTier=new[]{100,100,0},ClaimedMilestoneWaveByTier=new[]{100,100,0},SelectedTier=tier,Meta=new MetaProgress{Damage=20,Supply=20,Growth=20,Harvest=20}};
  CampaignSaveStore.Save(path,c);g.SavePathOverride=path;if(!g.TryReloadProgress()||!g.TryStartRun(42))throw new Exception("Start failed");g.SetPaused(true);
  while(g.Run.Wave<10&&g.Run.State==RunStatus.Running&&g.Run.TickCount<30000)g.Run.Tick();
  if(g.Run.Wave!=10)throw new Exception("Fixture did not reach wave10");
  UnityEditor.SessionState.SetString("Landscape.snapshot",JsonUtility.ToJson(g.Run.CaptureState()));
  return new{tier,wave=g.Run.Wave,ticks=g.Run.TickCount};
 }
 public static object Capture()
 {
  var g=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();int tier=g.Run.Tier;
  if(UnityEditor.SessionState.GetString("Landscape.snapshot","")!=JsonUtility.ToJson(g.Run.CaptureState()))throw new Exception("Paused rendering changed simulation");
  var paper=UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(r=>r.sortingOrder==-100).Single();var b=new MaterialPropertyBlock();paper.GetPropertyBlock(b);var top=b.GetColor("_WashTop");
  if(tier==2&&top.b<=top.r||tier==3&&top.r<=top.b)throw new Exception("Wrong palette");
  var shader=Resources.Load<Shader>("BokumyakuRendering/InkSprite");if(UnityEditor.ShaderUtil.ShaderHasError(shader))throw new Exception("Shader compile error");
  string name="../docs/evidence/landscape-depth-"+tier+(g.Run.Enemies.Count>20?"-dense":"");
  var report=new{tier,wave=g.Run.Wave,width=Screen.width,height=Screen.height,top=new[]{top.r,top.g,top.b},simulationUnchanged=true,shaderErrors=false,method=g.Run.Enemies.Count>20?"Paused Editor Play; artificial meta20, forced 21-enemy silhouette grid plus natural boss; not a natural encounter":"Paused Editor Play, artificial unlocked campaign and meta20 for same-wave visual comparison"};
  File.WriteAllText(Path.GetFullPath(name+".json"),Newtonsoft.Json.JsonConvert.SerializeObject(report));ScreenCapture.CaptureScreenshot(Path.GetFullPath(name+".png"));return report;
 }
}



