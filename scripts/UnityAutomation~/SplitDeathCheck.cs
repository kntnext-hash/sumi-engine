using System;using System.IO;using System.Linq;using System.Reflection;using UnityEngine;using Bokumyaku.Presentation;using Bokumyaku.Simulation;
public static class SplitDeathCheck {
public static object Run(){
var g=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();if(g==null||!g.SavePath.Contains("ink-qa-"))throw new Exception("Isolated QA required");
if(!g.TryStartRun(42))throw new Exception("Start failed");g.SetPaused(true);g.PresentBattle();
var f=BindingFlags.Instance|BindingFlags.NonPublic;
var world=(InkBattleWorld)typeof(BokumyakuGame).GetField("battleWorld",f).GetValue(g);
world.BeforeTick(g.Run);long pixels=g.DirtyInkPixels;int full=g.InkFullInitializations,fx=world.EffectsCreated;
typeof(RunSimulator).GetMethod("Spawn",f).Invoke(g.Run,new object[]{(EnemyKind?)EnemyKind.Splitter});
var parent=g.Run.Enemies.Single();typeof(EnemyState).GetProperty("X").SetValue(parent,0d);typeof(EnemyState).GetProperty("Y").SetValue(parent,130d);
var hurt=typeof(RunSimulator).GetMethod("Hurt",f);hurt.Invoke(g.Run,new object[]{parent,1e8d});
foreach(var child in g.Run.Enemies.ToArray())hurt.Invoke(g.Run,new object[]{child,1e8d});
world.AfterTick(g.Run,g.ReducedMotion);g.PresentBattle();
if(g.SpriteDeaths!=3||world.EffectsCreated-fx!=3||g.DirtyInkPixels<=pixels||g.InkFullInitializations!=full)throw new Exception("Death VFX/dirty update missing");
var report=new{kills=g.Run.Kills,notifications=g.Run.DefeatedThisTick.Count,presentationDeaths=g.SpriteDeaths,bursts=world.EffectsCreated-fx,updatedPixels=g.DirtyInkPixels-pixels,fullInitializationUnchanged=true,method="Isolated Editor Play: parent and children created/killed between BeforeTick and AfterTick via production Hurt; synthetic fixture"};
File.WriteAllText(Path.GetFullPath("../docs/evidence/split-death-presentation.json"),Newtonsoft.Json.JsonConvert.SerializeObject(report));return report;
}}
