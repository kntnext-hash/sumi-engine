using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Bokumyaku.Presentation;
using Bokumyaku.Simulation;
using Bokumyaku.Persistence;
public static class BokumyakuPlayEvidence {
 static BokumyakuGame game; static EditorWindow view; static Queue<Action> actions;
 static double next,started; static int firstFrame,pauseTick,rounds; static MetaProgress original;
 static List<object> checks; static bool passed=true;
 const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
 public static string Run(){
  game=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>(); if(game==null)throw new Exception("Play game scene first");
  original=game.Meta;game.Meta=new MetaProgress();game.SavePathOverride=Path.GetFullPath("../logs/bokumyaku-qa/progress.json");
  view=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));view.Focus();
  firstFrame=Time.frameCount;started=EditorApplication.timeSinceStartup;next=started; checks=new List<object>();actions=new Queue<Action>();
  Click(271,451); actions.Enqueue(()=>Check("出撃ボタン",!game.AwaitingStart,game.Run.TickCount));
  Click(620,188);actions.Enqueue(()=>{pauseTick=game.Run.TickCount;Check("一時停止",game.IsPaused,pauseTick);});
  actions.Enqueue(()=>Check("停止中tick維持",game.Run.TickCount==pauseTick,game.Run.TickCount));
  Click(620,188);Click(800,188);actions.Enqueue(()=>Check("再開と2倍速",!game.IsPaused&&game.Speed==2,game.Speed));
  Click(980,262);actions.Enqueue(()=>Check("集中方針",game.Run.Policy==GrowthPolicy.Focus,game.Run.Policy.ToString()));
  EditorApplication.update+=Tick;return "UI probe started: button events then two live runs and meta purchase";
 }
 static void Check(string name,bool ok,object detail){checks.Add(new{name,passed=ok,detail});passed&=ok;}
 static void Click(float x,float y){actions.Enqueue(()=>Send(EventType.MouseDown,x,y));actions.Enqueue(()=>Send(EventType.MouseUp,x,y));}
 static void Send(EventType type,float x,float y){
  float scale=Mathf.Min(Screen.width/1080f,Screen.height/760f);
  var p=new Vector2((Screen.width-1080*scale)/2+x*scale,(Screen.height-760*scale)/2+y*scale);
  var r=(Rect)view.GetType().GetProperty("targetInParent",Flags).GetValue(view);
  p=new Vector2(r.x+p.x*r.width/Screen.width,r.y+p.y*r.height/Screen.height);
  view.SendEvent(new Event{type=type,button=0,mousePosition=p});
 }
 static void Tick(){
  if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.18;
  try{
   if(EditorApplication.timeSinceStartup-started>150)throw new TimeoutException("Live probe exceeded150s");
   if(actions.Count>0){actions.Dequeue()();return;}
   if(game.Run.State==RunStatus.Running){
    // Buy a branch as soon as affordable to exercise the distinguishing supply tradeoff.
    if(game.Run.UpgradeLevels[3]==0&&game.Run.RunInk>=22)Check("周回分岐購入",game.BuyUpgrade(UpgradeKind.Branch),game.Run.RunInk);
    return;
   }
   Check("実時間敗北周回"+rounds,game.Run.State==RunStatus.Defeated,new{game.Run.TickCount,game.Run.Kills,game.Meta.Crystals});
   string warning;var saved=MetaSaveStore.Load(game.SavePath,out warning);Check("報酬保存"+rounds,warning==""&&saved.Crystals==game.Meta.Crystals,saved.Crystals);
   if(++rounds<2){game.StartRun(43);game.SetSpeed(2);return;}
   Check("恒久強化購入",game.BuyMeta(MetaUpgrade.Damage),game.Meta.Damage);
   saved=MetaSaveStore.Load(game.SavePath,out warning);Check("恒久保存読戻し",saved.Damage==1&&saved.Crystals==game.Meta.Crystals,saved.Damage);
   game.StartRun(44);Check("再出撃",game.Run.State==RunStatus.Running&&game.Run.TickCount==0,game.Run.Seed);game.SetPaused(true);
  }catch(Exception e){Check("exception",false,e.ToString());}
  EditorApplication.update-=Tick;
  Check("フレーム進行",Time.frameCount>firstFrame,new{firstFrame,endFrame=Time.frameCount});
  File.WriteAllText(Path.GetFullPath("../docs/evidence/bokumyaku-play.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new{passed,method="GameView.SendEvent + live Update + public gameplay API; QA isolated save",checks},Newtonsoft.Json.Formatting.Indented));
  game.Meta=original;game.SavePathOverride=null;game.SetPaused(true);
 }
}
