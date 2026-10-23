using System;using System.IO;using UnityEditor;using UnityEngine;using Bokumyaku.Presentation;
public static class RootBudgetCheck {
public static object Setup(){
var g=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();if(g==null||!g.SavePath.Contains("ink-qa-"))throw new Exception("Isolated only");
if(g.ResumeAvailable)g.AbandonRun();if(!g.TryStartRun(42))throw new Exception("Start failed");g.SetPaused(true);for(int i=0;i<900;i++)g.Run.Tick();
typeof(BokumyakuGame).GetField("branchDrawFrame",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(g,-1);int before=g.PaintedRootSlices;g.PresentBattle();int first=g.PaintedRootSlices;g.PresentBattle();int second=g.PaintedRootSlices;
if(first-before>1||first!=second)throw new Exception("Per-frame budget violated"); // root has no stroke
SessionState.SetString("RootBudget.snapshot",JsonUtility.ToJson(g.Run.CaptureState()));SessionState.SetInt("RootBudget.full",g.InkFullInitializations);
return new{before,first,second,total=g.Run.Branches.Count};
}
public static object Verify(){
var g=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();
if(g.PaintedRootSlices!=35 || g.PaintedBranchCount!=g.Run.Branches.Count)throw new Exception("Painting did not finish");
if(SessionState.GetString("RootBudget.snapshot","")!=JsonUtility.ToJson(g.Run.CaptureState()))throw new Exception("Simulation changed");
if(g.InkFullInitializations!=SessionState.GetInt("RootBudget.full",0))throw new Exception("Full initialization changed");
var r=new{rootSlices=g.PaintedRootSlices,painted=g.PaintedBranchCount,total=g.Run.Branches.Count,snapshotUnchanged=true,fullInitializationUnchanged=true,method="Paused 900tick fixture; repeated calls within one frame and completion across subsequent frames"};
File.WriteAllText(Path.GetFullPath("../docs/evidence/root-stroke-budget.json"),Newtonsoft.Json.JsonConvert.SerializeObject(r));return r;
}}
