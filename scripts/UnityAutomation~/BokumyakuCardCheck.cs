using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Bokumyaku.Presentation;
public static class BokumyakuCardCheck
{
 static BokumyakuGame g;static EditorWindow v;static Queue<Action> q;static List<object> rows;static double next;
 static BindingFlags f=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 public static object Run(){
 g=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();if(!g.SavePath.Contains("ink-qa-"))throw new Exception("isolated only");
 v=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));v.Focus();q=new Queue<Action>();rows=new List<object>();next=0;
 if(g.ResumeAvailable)g.AbandonRun();
 g.SetBuild(Bokumyaku.Simulation.BuildPreset.Manual);g.TryStartRun(42);g.SetPaused(true);

 float h=Screen.height/(Screen.width/440f);
 for(int i=0;i<5;i++){
  int index=i,level=0,cost=0,balance=0;
  q.Enqueue(()=>{g.AbandonRun();g.TryStartRun(42);g.SetPaused(true);level=g.Run.UpgradeLevels[index];cost=g.Run.UpgradeCost((Bokumyaku.Simulation.UpgradeKind)index);for(int tick=0;tick<10000 && g.Run.RunInk<cost && g.Run.State==Bokumyaku.Simulation.RunStatus.Running;tick++)g.Run.Tick();balance=g.Run.RunInk;});
  Click(48+86*i,h-75,()=>rows.Add(new{name="card-"+index,passed=g.Run.UpgradeLevels[index]==level+1&&g.Run.RunInk==balance-cost,cost}));
 }
 q.Enqueue(()=>{g.AbandonRun();g.TryStartRun(42);g.SetPaused(true);});
 int before=0;q.Enqueue(()=>before=g.Run.UpgradeLevels[0]);
 Click(48,h-75,()=>rows.Add(new{name="insufficient-ink",passed=g.Run.RunInk==0&&g.Run.UpgradeLevels[0]==before}));
 EditorApplication.update+=Tick;return "five purchases plus insufficient ink";
 }
 static void Click(float x,float y,Action check){q.Enqueue(()=>Send(x,y,EventType.MouseDown));q.Enqueue(()=>Send(x,y,EventType.MouseUp));q.Enqueue(check);}
 static void Send(float x,float y,EventType type){var r=(Rect)v.GetType().GetProperty("targetInParent",f).GetValue(v);v.SendEvent(new Event{type=type,button=0,mousePosition=new Vector2(r.x+x/440*r.width,r.y+y/(Screen.height/(Screen.width/440f))*r.height)});}
 static void Tick(){if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.15;if(q.Count>0){q.Dequeue()();return;}EditorApplication.update-=Tick;g.SetPaused(true);g.SetAutoRepeat(false);File.WriteAllText(Path.GetFullPath("../docs/evidence/ink-card-purchases.json"),Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));}
}
