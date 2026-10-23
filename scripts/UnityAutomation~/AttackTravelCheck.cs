using System;using System.IO;using System.Reflection;using System.Collections;using UnityEngine;using Bokumyaku.Presentation;using Bokumyaku.Simulation;
public static class AttackTravelCheck{
 public static object Run(){
 var g=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();if(!g.SavePath.Contains("ink-qa-"))throw new Exception("Isolated save required");g.TryStartRun(42);g.SetPaused(true);g.SetReducedMotion(false);g.PresentBattle();
 var flags=BindingFlags.Instance|BindingFlags.NonPublic;var w=(InkBattleWorld)typeof(BokumyakuGame).GetField("battleWorld",flags).GetValue(g);
 var hit=new AttackEvent();foreach(var pair in new[]{new{n="X",v=0d},new{n="Y",v=0d},new{n="TargetX",v=200d},new{n="TargetY",v=250d}})typeof(AttackEvent).GetProperty(pair.n).SetValue(hit,pair.v);
 var attack=typeof(InkBattleWorld).GetMethod("Attack",flags);var list=(IList)typeof(InkBattleWorld).GetField("effects",flags).GetValue(w);
 string before=JsonUtility.ToJson(g.Run.CaptureState());int checkedWeapons=0;
 foreach(var kind in new[]{WeaponKind.Point,WeaponKind.Pierce}){
 w.Reset(g.Run);typeof(AttackEvent).GetProperty("Weapon").SetValue(hit,kind);attack.Invoke(w,new object[]{hit,g.Run,false});object moving=null;foreach(var e in list)if((int)e.GetType().GetField("type").GetValue(e)==3)moving=e;
 if(moving==null)throw new Exception("No moving brush");var t=moving.GetType();var renderer=(SpriteRenderer)t.GetField("renderer").GetValue(moving);g.PresentBattle();var start=renderer.transform.position;
 t.GetField("age").SetValue(moving,.05f);g.PresentBattle();if(Vector3.Distance(start,renderer.transform.position)<.1f)throw new Exception("Brush did not travel");
 g.SetReducedMotion(true);g.PresentBattle();if(renderer.enabled)throw new Exception("Reduced motion did not hide travel");g.SetReducedMotion(false);checkedWeapons++;
 }
 if(before!=JsonUtility.ToJson(g.Run.CaptureState()))throw new Exception("Presentation changed simulation");g.PresentBattle();
 var report=new{passed=true,weapons=checkedWeapons,scope="Injected Point/Pierce presentation events: travel positions, reduced-motion hiding, simulation state unchanged. Not gameplay timing or performance."};File.WriteAllText(Path.GetFullPath("../docs/evidence/attack-travel.json"),Newtonsoft.Json.JsonConvert.SerializeObject(report));ScreenCapture.CaptureScreenshot(Path.GetFullPath("../docs/evidence/attack-travel.png"));return report;
 }}
