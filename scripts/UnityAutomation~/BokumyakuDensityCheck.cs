using System;
using System.IO;
using System.Reflection;
using System.Collections;
using UnityEditor;
using UnityEngine;
using Bokumyaku.Presentation;
using Bokumyaku.Simulation;
public static class BokumyakuDensityCheck
{
    public static object Run()
    {
        var game=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();
        if(game==null||!game.SavePath.Contains("ink-qa-"))throw new Exception("Isolated Play required");
        game.SetPaused(true);
        var enemies=(IList)typeof(RunSimulator).GetField("enemies",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(game.Run);
        enemies.Clear();
        for(int i=0;i<40;i++)
        {
            var e=new EnemyState(); double angle=i*2.39996;double radius=125+(i%4)*38;
            Set(e,"Id",10000+i);Set(e,"Kind",(EnemyKind)(i%5));Set(e,"X",Math.Cos(angle)*radius);Set(e,"Y",Math.Sin(angle)*radius);
            Set(e,"Hp",75d);Set(e,"MaxHp",100d);enemies.Add(e);
        }
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("../docs/evidence/contrast-density.png"));
        var result=new{enemies=enemies.Count,method="Synthetic paused 40-enemy fixture, all five kinds; visual overlap review only, not gameplay balance or performance"};
        File.WriteAllText(Path.GetFullPath("../docs/evidence/contrast-density.json"),Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return result;
    }
    static void Set(EnemyState e,string name,object value){typeof(EnemyState).GetProperty(name).SetValue(e,value);}
}
