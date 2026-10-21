using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ConnectPiece.SumiEngine;
using ConnectPiece.SumiEngine.Core;
using UnityEditor;
using UnityEngine;

// Rendered wide-layout UI regression. Queues actual GameView mouse events across frames.
// Coordinates are checked against the captured reference view; this is not a physical mouse test.
public static class BrushLabUiEvidence
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static Queue<Action> actions;
    static EditorWindow view;
    static SumiBrushLab lab;
    static SumiCanvas canvas;
    static double next, started;
    [Serializable] public sealed class Check { public string name,detail; public bool passed; }
    [Serializable] public sealed class Report { public string method="GameView.SendEvent";public bool passed=true;public int startFrame,endFrame;public List<Check> checks=new List<Check>(); }
    static Report report;
    public static string Run()
    {
        if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play first.");
        if(Screen.width<1000||Screen.height<600)throw new InvalidOperationException("Maximize GameView first.");
        if(actions!=null)throw new InvalidOperationException("Probe already running.");
        lab=UnityEngine.Object.FindFirstObjectByType<SumiBrushLab>();canvas=lab.GetComponent<SumiCanvas>();
        view=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));view.Focus();
        canvas.diffusionFps=0;
        report=new Report{startFrame=Time.frameCount};actions=new Queue<Action>();
        var names=new[]{"Vein","Slash","Bloom","Dense","Dry","Split","Bleed","Pool","Mist","Splatter"};
        var brushes=new[]{InkBrushSettings.Vein,InkBrushSettings.Slash,InkBrushSettings.Bloom,InkBrushSettings.Dense,InkBrushSettings.Dry,InkBrushSettings.Split,InkBrushSettings.Bleed,InkBrushSettings.Pool,InkBrushSettings.Mist,InkBrushSettings.Splatter};
        for(int i=0;i<names.Length;i++)
        {
            int index=i;float x=i<6?50+(i%3)*90:35+(i-6)*66;
            float y=i<3?78:i<6?103:128;
            Click(x,y);actions.Enqueue(()=>{
                var actual=Field<InkBrushSettings>("current");var expected=brushes[index];
                Add(names[index],actual.texture==expected.texture&&Math.Abs(actual.radius-expected.radius)<.01f&&Math.Abs(actual.roughness-expected.roughness)<.001f,actual.texture.ToString());
            });
        }
        Click(230,203); // Colors open.
        Click(200,253);Click(130,303); // Paper / White.
        actions.Enqueue(()=>Add("Paper preset",ColorUtility.ToHtmlStringRGB(canvas.paperColor)=="FFFFFF",ColorUtility.ToHtmlStringRGB(canvas.paperColor)));
        Slider(150,323);actions.Enqueue(()=>Add("Paper R slider",canvas.paperColor.r>.1f&&canvas.paperColor.r<.9f,canvas.paperColor.r.ToString()));
        Slider(150,348);actions.Enqueue(()=>Add("Paper G slider",canvas.paperColor.g>.1f&&canvas.paperColor.g<.9f,canvas.paperColor.g.ToString()));
        Slider(150,373);actions.Enqueue(()=>Add("Paper B slider",canvas.paperColor.b>.1f&&canvas.paperColor.b<.9f,canvas.paperColor.b.ToString()));
        Slider(80,468);actions.Enqueue(()=>Add("Density slider",canvas.displayDensity>.1f&&canvas.displayDensity<6f,canvas.displayDensity.ToString()));
        Click(40,303);Click(50,253); // Washi / Ink.
        Click(130,279); // Vermilion.
        actions.Enqueue(()=>Add("Vermilion",ColorUtility.ToHtmlStringRGB(canvas.inkColor)=="BE3023",ColorUtility.ToHtmlStringRGB(canvas.inkColor)));
        Click(50,103); // Dense must preserve palette.
        actions.Enqueue(()=>Add("Preset preserves palette",ColorUtility.ToHtmlStringRGB(canvas.inkColor)=="BE3023",ColorUtility.ToHtmlStringRGB(canvas.inkColor)));
        Click(230,203); // Colors close.
        Click(50,203); // Clear.
        actions.Enqueue(()=>Add("Clear",canvas.Core.Session.strokes.Count==0&&canvas.Core.Paper.TotalPigment()==0,"paper and history"));
        Slider(80,273);actions.Enqueue(()=>Add("Pressure slider",Field<float>("pressure")>.05f&&Field<float>("pressure")<.8f,Field<float>("pressure").ToString()));
        Slider(80,313);actions.Enqueue(()=>Add("Radius slider",Field<InkBrushSettings>("current").radius>2&&Field<InkBrushSettings>("current").radius<20,Field<InkBrushSettings>("current").radius.ToString()));
        Slider(60,351);actions.Enqueue(()=>Add("Pigment slider",Field<InkBrushSettings>("current").pigment>.1f&&Field<InkBrushSettings>("current").pigment<.8f,Field<InkBrushSettings>("current").pigment.ToString()));
        Slider(200,351);actions.Enqueue(()=>Add("Water slider",Field<InkBrushSettings>("current").water>.3f&&Field<InkBrushSettings>("current").water<.8f,Field<InkBrushSettings>("current").water.ToString()));
        Slider(60,391);actions.Enqueue(()=>Add("Roughness slider",Field<InkBrushSettings>("current").roughness>.3f&&Field<InkBrushSettings>("current").roughness<.8f,Field<InkBrushSettings>("current").roughness.ToString()));
        Slider(200,391);actions.Enqueue(()=>Add("Bristles slider",Field<InkBrushSettings>("current").bristleCount>5&&Field<InkBrushSettings>("current").bristleCount<20,Field<InkBrushSettings>("current").bristleCount.ToString()));
        // Return Dense, reset pressure through its slider for the input test.
        Click(50,103);Slider(268,273);
        actions.Enqueue(()=>Send(EventType.MouseDown,Paper(.2f,.65f)));
        actions.Enqueue(()=>Send(EventType.MouseDrag,Paper(.55f,.4f)));
        actions.Enqueue(()=>Send(EventType.MouseUp,Paper(.8f,.25f)));
        actions.Enqueue(()=>{
            var strokes=canvas.Core.Session.strokes;
            bool valid=strokes.Count==1&&strokes[0].points.Count>3&&strokes[0].points[strokes[0].points.Count-1].pressure==0;
            Add("Mouse taper path",valid,"strokes="+strokes.Count+", active="+Field<bool>("drawing"));
        });
        actions.Enqueue(()=>Send(EventType.MouseDown,Paper(.3f,.3f)));
        actions.Enqueue(()=>Send(EventType.MouseDrag,Paper(.4f,.5f)));
        actions.Enqueue(()=>Send(EventType.MouseUp,new Vector2(10,10)));
        actions.Enqueue(()=>Add("Release outside paper",!Field<bool>("drawing")&&canvas.Core.Session.strokes.Count==2,"input ended"));
        actions.Enqueue(()=>Send(EventType.MouseDown,Paper(.4f,.7f)));
        actions.Enqueue(()=>lab.SendMessage("OnApplicationFocus",false));
        actions.Enqueue(()=>Add("Focus-loss handler",!Field<bool>("drawing")&&!Field<InkPointerStroke>("pointer").Active,"handler invoked; not OS focus simulation"));
        Click(140,203);
        actions.Enqueue(()=>{
            string status=Field<string>("status");bool matches=false;
            if(status.StartsWith("Saved: "))
            {
                var texture=new Texture2D(2,2);ImageConversion.LoadImage(texture,File.ReadAllBytes(status.Substring(7)));
                var a=texture.GetPixels32();var b=canvas.Texture.GetPixels32();matches=a.Length==b.Length;
                for(int i=0;matches&&i<a.Length;i++)matches=a[i].Equals(b[i]);UnityEngine.Object.Destroy(texture);
            }
            Add("PNG button round trip",matches,status);
        });
        Click(140,153);actions.Enqueue(()=>Add("Stroke study",canvas.Core.Session.strokes.Count==15,"count="+canvas.Core.Session.strokes.Count));
        Click(140,178);actions.Enqueue(()=>Add("Effects study",canvas.Core.Session.strokes.Count==50,"count="+canvas.Core.Session.strokes.Count));
        actions.Enqueue(()=>{canvas.inkColor=new Color32(12,12,13,255);canvas.displayDensity=7;canvas.Upload();});
        started=EditorApplication.timeSinceStartup;next=started;
        EditorApplication.update+=Tick;
        return "Queued rendered UI probe; await docs/evidence/release-ui.json";
    }
    static T Field<T>(string name)=>(T)typeof(SumiBrushLab).GetField(name,Flags).GetValue(lab);
    static Vector2 Panel(float x,float y)=>new Vector2(Screen.width-300+x,y);
    static Vector2 Paper(float x,float y){var r=Field<Rect>("paperRect");return new Vector2(r.x+x*r.width,r.y+y*r.height);}
    static void Click(float x,float y){actions.Enqueue(()=>Send(EventType.MouseDown,Panel(x,y)));actions.Enqueue(()=>Send(EventType.MouseUp,Panel(x,y)));}
    static void Slider(float x,float y)=>Click(x,y);
    static void Send(EventType type,Vector2 p)
    {
        var target=(Rect)view.GetType().GetProperty("targetInParent",Flags).GetValue(view);
        p=new Vector2(target.x+p.x*target.width/Screen.width,target.y+p.y*target.height/Screen.height);
        view.SendEvent(new Event{type=type,button=0,mousePosition=p});
    }
    static void Add(string name,bool pass,string detail){report.checks.Add(new Check{name=name,passed=pass,detail=detail});report.passed&=pass;}
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup<next)return;
        next=EditorApplication.timeSinceStartup+.14;
        try
        {
            if(EditorApplication.timeSinceStartup-started>60)throw new TimeoutException("UI probe exceeded60s");
            if(actions.Count>0){actions.Dequeue()();return;}
        }
        catch(Exception ex){Add("Probe exception",false,ex.ToString());}
        EditorApplication.update-=Tick;
        report.endFrame=Time.frameCount;report.passed&=report.endFrame>report.startFrame;
        File.WriteAllText(Path.GetFullPath("../docs/evidence/release-ui.json"),Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
        actions=null;
    }
}
