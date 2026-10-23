using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEditor.Profiling;
using Newtonsoft.Json;
public static class InspectPlayerProfile
{
    public static object Load()
    {
        if (ProfilerDriver.enabled) throw new Exception("Preserve active profiler recording; stop explicitly before loading.");
        var manifest=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText("../docs/evidence/profile-capture-v27.json"));
        if(ProfilerDriver.lastFrameIndex>=0)
            ProfilerDriver.SaveProfile(Path.GetFullPath("../logs/profiler-editor-backup-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+".raw"));
        ProfilerDriver.LoadProfile((string)manifest["raw"],false);
        return new {first=ProfilerDriver.firstFrameIndex,last=ProfilerDriver.lastFrameIndex};
    }
    public static object Analyze()
    {
        var frames=new List<object>();
        var samples=new List<object>();
        int first=ProfilerDriver.firstFrameIndex,last=ProfilerDriver.lastFrameIndex;
        if(first<0||last<first)throw new Exception("No capture loaded");
        for(int f=first;f<=last;f++)
        {
            using(var main=ProfilerDriver.GetRawFrameDataView(f,0))
            { if(main.valid&&main.frameTimeMs>40)frames.Add(new{frame=f,ms=main.frameTimeMs}); }
            for(int t=0;t<256;t++)using(var view=ProfilerDriver.GetRawFrameDataView(f,t))
            {
                if(!view.valid)break;
                if(view.threadName!="Main Thread"&&view.threadName!="Render Thread")continue;
                for(int s=0;s<view.sampleCount;s++)
                {
                    float ms=view.GetSampleTimeMs(s);
                    if(ms<40)continue;
                    samples.Add(new{frame=f,thread=view.threadName,index=s,name=view.GetSampleName(s),ms,startMs=view.GetSampleStartTimeMs(s),children=view.GetSampleChildrenCount(s)});
                }
            }
        }
        var result=new{first,last,frames,samples,note="Inclusive samples overlap. Development capture, not release performance certification."};
        File.WriteAllText("../docs/evidence/profile-analysis-v27.json",JsonConvert.SerializeObject(result,Formatting.Indented));
        return new{first,last,slowFrames=frames.Count,longSamples=samples.Count};
    }
}
