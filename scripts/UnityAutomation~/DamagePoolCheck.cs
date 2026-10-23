using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using TMPro;
using Bokumyaku.Presentation;
using Bokumyaku.Simulation;
public static class DamagePoolCheck
{
 public static object Run()
 {
  var game=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();
  if(!game.SavePath.Contains("ink-qa-"))throw new Exception("Isolated save required");
  game.TryStartRun(42);game.SetPaused(true);game.PresentBattle();
  var flags=BindingFlags.Instance|BindingFlags.NonPublic;
  var world=(InkBattleWorld)typeof(BokumyakuGame).GetField("battleWorld",flags).GetValue(game);
  var labels=(IList)typeof(InkBattleWorld).GetField("damageLabels",flags).GetValue(world);
  var show=typeof(InkBattleWorld).GetMethod("ShowDamage",flags);
  world.Reset(game.Run);
  for(int cycle=0;cycle<30;cycle++)
  {
   for(int i=0;i<100;i++)
   {
    var enemy=new EnemyState();typeof(EnemyState).GetProperty("Id").SetValue(enemy,cycle*100+i);
    show.Invoke(world,new object[]{enemy,2d});show.Invoke(world,new object[]{enemy,3d});
   }
   if(labels.Count!=24)throw new Exception("Pool exceeded cap");
   foreach(var label in labels)
   {
    var type=label.GetType();
    if((double)type.GetField("damage").GetValue(label)!=5d)throw new Exception("Aggregation mismatch");
   }
   world.Reset(game.Run);
   foreach(var label in labels)
    if(((TextMeshPro)label.GetType().GetField("text").GetValue(label)).gameObject.activeSelf)throw new Exception("Reset left label visible");
  }
  var result=new{passed=true,cycles=30,attemptedEnemiesPerCycle=100,allocated=labels.Count,aggregation=5,resetHidden=true,scope="Synthetic presentation calls; not performance or natural combat soak"};
  File.WriteAllText(Path.GetFullPath("../docs/evidence/damage-pool-check.json"),Newtonsoft.Json.JsonConvert.SerializeObject(result));return result;
 }
}
