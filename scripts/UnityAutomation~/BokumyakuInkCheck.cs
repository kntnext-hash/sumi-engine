using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Bokumyaku.Presentation;
using Bokumyaku.Simulation;
public static class BokumyakuInkCheck
{
    static BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    public static object Arm()
    {
        if(EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Clean stopped scene required");
        var a=typeof(Editor).Assembly;var view=EditorWindow.GetWindow(a.GetType("UnityEditor.GameView"));
        SessionState.SetInt("InkQA.oldSize",(int)view.GetType().GetProperty("selectedSizeIndex",Flags).GetValue(view));
        var t=a.GetType("UnityEditor.GameViewSizes");var sizes=typeof(ScriptableSingleton<>).MakeGenericType(t).GetProperty("instance").GetValue(null);
        var group=t.GetMethod("GetGroup").Invoke(sizes,new object[]{0});
        var size=Activator.CreateInstance(a.GetType("UnityEditor.GameViewSize"),new object[]{Enum.ToObject(a.GetType("UnityEditor.GameViewSizeType"),1),360,640,"Ink QA"});
        group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});int index=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null)-1;
        SessionState.SetInt("InkQA.added",index);view.GetType().GetProperty("selectedSizeIndex",Flags).SetValue(view,index);view.maximized=true;view.Focus();
        SessionState.SetString("Bokumyaku.QaSavePath",Path.GetFullPath("../logs/ink-qa-"+Guid.NewGuid().ToString("N")+"/campaign.json"));
        EditorApplication.isPaused=false;EditorApplication.isPlaying=true;return "isolated portrait Play";
    }
    public static object Run()
    {
        var game=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();
        if(!game.SavePath.Contains("ink-qa-"))throw new Exception("Isolated save required");
        Application.runInBackground=true;EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
        game.SetBuild(BuildPreset.Farming);game.TryStartRun(42);
        for(int i=0;i<7000&&game.Run.State==RunStatus.Running;i++)game.Run.Tick();
        SessionState.SetInt("InkQA.frame",Time.frameCount);SessionState.SetInt("InkQA.tick",game.Run.TickCount);
        return new{game.Run.Wave,game.Run.TickCount};
    }
    public static object Capture()
    {
        var game=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();game.SetPaused(true);
        var sheet=new Texture2D(576,96,TextureFormat.RGBA32,false);
        var report=new System.Collections.Generic.List<object>();
        for(int i=0;i<6;i++)
        {
            var texture=i==5?ProceduralInkMotifs.CreateCore():ProceduralInkMotifs.CreateEnemy((EnemyKind)i);
            var pixels=texture.GetPixels32();int ink=0,clear=0;
            for(int p=0;p<pixels.Length;p++){if(pixels[p].a>20)ink++;if(pixels[p].a==0)clear++;var alpha=pixels[p].a/255f;pixels[p]=Color.Lerp(new Color(.94f,.92f,.86f),new Color(.06f,.05f,.04f),alpha);}
            if(ink==0||clear==0)throw new Exception("Empty or opaque mask");
            sheet.SetPixels32(i*96,0,96,96,pixels);report.Add(new{kind=i==5?"Core":((EnemyKind)i).ToString(),ink,clear});UnityEngine.Object.DestroyImmediate(texture);
        }
        sheet.Apply();File.WriteAllBytes(Path.GetFullPath("../docs/evidence/bokumyaku-ink-motifs.png"),sheet.EncodeToPNG());UnityEngine.Object.DestroyImmediate(sheet);
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("../docs/evidence/bokumyaku-ink-battle.png"));
        var result=new{frameAdvanced=Time.frameCount>SessionState.GetInt("InkQA.frame",int.MaxValue),tickAdvanced=game.Run.TickCount>SessionState.GetInt("InkQA.tick",int.MaxValue),report,game.Run.Wave,method="Limited Unity play/mask check; not device performance or full QA"};
        File.WriteAllText(Path.GetFullPath("../docs/evidence/bokumyaku-ink-check.json"),Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
    public static object Cleanup()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play first");
        var a=typeof(Editor).Assembly;var view=EditorWindow.GetWindow(a.GetType("UnityEditor.GameView"));view.GetType().GetProperty("selectedSizeIndex",Flags).SetValue(view,SessionState.GetInt("InkQA.oldSize",0));
        var t=a.GetType("UnityEditor.GameViewSizes");var sizes=typeof(ScriptableSingleton<>).MakeGenericType(t).GetProperty("instance").GetValue(null);var group=t.GetMethod("GetGroup").Invoke(sizes,new object[]{0});group.GetType().GetMethod("RemoveCustomSize").Invoke(group,new object[]{SessionState.GetInt("InkQA.added",-1)});
        view.maximized=false;Application.runInBackground=false;PlayerSettings.runInBackground=false;SessionState.EraseString("Bokumyaku.QaSavePath");AssetDatabase.SaveAssets();return "restored";
    }
}
