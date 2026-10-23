using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
namespace Bokumyaku.Presentation {
 // Enabled only by explicit QA. No production allocations or timing calls when disabled.
 public static class BattleTiming {
  public static bool Enabled;
  [Serializable] public sealed class Sample { public string section; public int frame; public double milliseconds; }
  static readonly List<Sample> samples=new List<Sample>();
  public static Scope Measure(string section)=>new Scope(section);
  public static Sample[] Capture()=>samples.ToArray();
  public struct Scope:IDisposable {
   readonly string section;readonly long start;readonly int frame;
   public Scope(string name){section=Enabled?name:null;start=Enabled?Stopwatch.GetTimestamp():0;frame=Enabled?Time.frameCount:0;}
   public void Dispose(){if(section==null)return;double ms=(Stopwatch.GetTimestamp()-start)*1000d/Stopwatch.Frequency;if(ms>=10&&samples.Count<512)samples.Add(new Sample{section=section,frame=frame,milliseconds=ms});}
  }
 }
}
