using System;using System.IO;using System.Linq;using System.Reflection;using System.Collections;using UnityEngine;using Bokumyaku.Presentation;using Bokumyaku.Simulation;
public static class PointSplashCheck
{
 public static object Run()
 {
  var g=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();if(g==null||!g.SavePath.Contains("ink-qa-"))throw new Exception("Isolated Play required");
  g.TryStartRun(42);g.SetPaused(true);g.SetReducedMotion(false);g.PresentBattle();
  var flags=BindingFlags.Instance|BindingFlags.NonPublic;var w=(InkBattleWorld)typeof(BokumyakuGame).GetField("battleWorld",flags).GetValue(g);
  var list=(IList)typeof(InkBattleWorld).GetField("effects",flags).GetValue(w);var attack=typeof(InkBattleWorld).GetMethod("Attack",flags);
  var hit=new AttackEvent();typeof(AttackEvent).GetProperty("Weapon").SetValue(hit,WeaponKind.Point);
  typeof(AttackEvent).GetProperty("TargetX").SetValue(hit,160d);typeof(AttackEvent).GetProperty("TargetY").SetValue(hit,180d);
  string snapshot=JsonUtility.ToJson(g.Run.CaptureState());w.Reset(g.Run);attack.Invoke(w,new object[]{hit,g.Run,false});
  var drops=list.Cast<object>().Where(e=>(int)e.GetType().GetField("type").GetValue(e)==4).ToArray();if(drops.Length!=5)throw new Exception("Five droplets required");
  Action<float,bool> render=(age,reduced)=>{foreach(var e in drops)e.GetType().GetField("age").SetValue(e,age);w.Render(g.Run,g.BranchOverlay,1,true,reduced,true,BokumyakuBattleHud.GetBattleViewport());};
  Func<Vector3[]> positions=()=>drops.Select(e=>((SpriteRenderer)e.GetType().GetField("renderer").GetValue(e)).transform.localPosition).ToArray();
  render(.02f,false);var start=positions();render(.15f,false);var moved=positions();if(!start.Zip(moved,(a,b)=>Vector3.Distance(a,b)>.05f).All(x=>x))throw new Exception("Droplets did not travel");
  render(.02f,true);var quiet=positions();render(.15f,true);if(!quiet.Zip(positions(),(a,b)=>Vector3.Distance(a,b)<.0001f).All(x=>x))throw new Exception("Reduced motion moved droplets");
  if(drops.Any(e=>!((SpriteRenderer)e.GetType().GetField("renderer").GetValue(e)).enabled))throw new Exception("Reduced motion hid impact information");
  for(int i=0;i<50;i++)attack.Invoke(w,new object[]{hit,g.Run,false});if(list.Count>128)throw new Exception("Effect pool exceeded bound");
  w.Reset(g.Run);attack.Invoke(w,new object[]{hit,g.Run,false});foreach(var e in list)e.GetType().GetField("age").SetValue(e,.10f);w.Render(g.Run,g.BranchOverlay,1,true,false,true,BokumyakuBattleHud.GetBattleViewport());
  if(snapshot!=JsonUtility.ToJson(g.Run.CaptureState()))throw new Exception("Simulation changed");
  var result=new{passed=true,droplets=5,normalTravel=true,reducedStaticVisible=true,boundedEffects=128,snapshotUnchanged=true,width=Screen.width,height=Screen.height,note="Injected presentation event; not natural combat timing/performance."};
  File.WriteAllText("../docs/evidence/point-splash-check.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));ScreenCapture.CaptureScreenshot(Path.GetFullPath("../docs/evidence/point-splash-check.png"));return result;
 }
}

