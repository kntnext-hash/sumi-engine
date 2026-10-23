using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Bokumyaku.Presentation;
public static class BokumyakuHudCheck
{
 static BokumyakuGame g;static EditorWindow v;static Queue<Action> q;static List<object> rows;static double next;
 static BindingFlags f=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 public static object Run(){
 g=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();if(!g.SavePath.Contains("ink-qa-"))throw new Exception("isolated only");
 v=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));v.Focus();q=new Queue<Action>();rows=new List<object>();next=0;
 float h=Screen.height/(Screen.width/440f);int speed=g.Speed;bool repeat=g.AutoRepeatEnabled;bool pause=g.IsPaused;
 Click(242,h-166,()=>rows.Add(new{name="speed",passed=g.Speed!=speed}));
 Click(314,h-166,()=>rows.Add(new{name="auto",passed=g.AutoRepeatEnabled!=repeat}));
 Click(398,h-166,()=>rows.Add(new{name="details",passed=(bool)typeof(BokumyakuGame).GetField("portraitDetails",f).GetValue(g)}));
 Click(398,h-166,()=>rows.Add(new{name="close",passed=!(bool)typeof(BokumyakuGame).GetField("portraitDetails",f).GetValue(g)}));
 Click(408,30,()=>rows.Add(new{name="pause",passed=g.IsPaused!=pause}));
 EditorApplication.update+=Tick;return "five GUI checks";
 }
 static void Click(float x,float y,Action check){q.Enqueue(()=>Send(x,y,EventType.MouseDown));q.Enqueue(()=>Send(x,y,EventType.MouseUp));q.Enqueue(check);}
 static void Send(float x,float y,EventType type){var r=(Rect)v.GetType().GetProperty("targetInParent",f).GetValue(v);v.SendEvent(new Event{type=type,button=0,mousePosition=new Vector2(r.x+x/440*r.width,r.y+y/(Screen.height/(Screen.width/440f))*r.height)});}
 static void Tick(){if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.15;if(q.Count>0){q.Dequeue()();return;}EditorApplication.update-=Tick;g.SetPaused(true);g.SetAutoRepeat(false);File.WriteAllText(Path.GetFullPath("../docs/evidence/ink-hud-controls.json"),Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));}
}
