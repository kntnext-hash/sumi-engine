using System;using System.IO;using System.Reflection;using UnityEngine;using UnityEditor;using Bokumyaku.Presentation;
public static class InkClockCheck{
 static Material Get(){var g=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();if(!g.SavePath.Contains("ink-qa-"))throw new Exception("Isolated only");var f=BindingFlags.Instance|BindingFlags.NonPublic;var w=typeof(BokumyakuGame).GetField("battleWorld",f).GetValue(g);return (Material)w.GetType().GetField("material",f).GetValue(w);}
 public static object Start(){var g=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();g.TryStartRun(42);g.SetPaused(false);return "running";}
 public static object Pause(){var g=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();g.SetPaused(true);g.PresentBattle();float t=Get().GetFloat("_InkTime");if(t<=0)throw new Exception("Clock did not advance");SessionState.SetFloat("InkClock.time",t);return t;}
 public static object Check(){float before=SessionState.GetFloat("InkClock.time",-1),after=Get().GetFloat("_InkTime");if(before<0||after!=before)throw new Exception("Paused clock advanced");var result=new{passed=true,before,after,scope="Shader material time advances during play and is unchanged across paused frames"};File.WriteAllText(Path.GetFullPath("../docs/evidence/ink-clock-check.json"),Newtonsoft.Json.JsonConvert.SerializeObject(result));return result;}
}
