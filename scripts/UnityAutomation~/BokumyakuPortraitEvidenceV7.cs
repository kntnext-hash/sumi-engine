using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Bokumyaku.Presentation;
using Bokumyaku.Simulation;

// Temporary GameView size through Editor API; does not edit project or scene YAML.
public static class BokumyakuPortraitEvidenceV7
{
    const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;
    static EditorWindow view;
    static object group;
    static int previousIndex, addedIndex;
    static float previousScale;
    static double releaseAt;
    static bool active, weaponClicked;
    public static object Begin()
    {
        var game=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();
        if(active || game==null || !game.IsPaused)throw new Exception("Use the completed paused QA run");
        var asm=typeof(Editor).Assembly;
        view=EditorWindow.GetWindow(asm.GetType("UnityEditor.GameView"));
        previousIndex=(int)view.GetType().GetProperty("selectedSizeIndex",Flags).GetValue(view);
        previousScale=((Vector2)view.GetType().GetProperty("zoomAreaScale",Flags).GetValue(view)).x;
        var sizesType=asm.GetType("UnityEditor.GameViewSizes");
        var sizes=sizesType.GetProperty("instance",Flags).GetValue(null);
        group=sizesType.GetProperty("currentGroup",Flags).GetValue(sizes);
        addedIndex=(int)group.GetType().GetMethod("GetTotalCount",Flags).Invoke(group,null);
        var kind=Enum.Parse(asm.GetType("UnityEditor.GameViewSizeType"),"FixedResolution");
        var size=Activator.CreateInstance(asm.GetType("UnityEditor.GameViewSize"),Flags,null,new object[]{kind,440,1060,"Codex Bokumyaku portrait QA"},null);
        group.GetType().GetMethod("AddCustomSize",Flags).Invoke(group,new[]{size});
        view.GetType().GetProperty("selectedSizeIndex",Flags).SetValue(view,addedIndex);
        typeof(BokumyakuGame).GetField("panelScroll",Flags).SetValue(game,Vector2.zero);
        active=true;weaponClicked=false;view.Focus();view.Repaint();
        return new{previousIndex,addedIndex};
    }
    static void Send(EventType type,float x,float y,float scroll=0)
    {
        float s=Mathf.Min(Screen.width/440f,Screen.height/1060f);
        var p=new Vector2((Screen.width-440*s)/2+x*s,(Screen.height-1060*s)/2+y*s);
        var r=(Rect)view.GetType().GetProperty("targetInParent",Flags).GetValue(view);
        p=new Vector2(r.x+p.x*r.width/Screen.width,r.y+p.y*r.height/Screen.height);
        view.SendEvent(new Event{type=type,button=0,mousePosition=p,delta=new Vector2(0,scroll)});
    }
    public static object ClickWeapon()
    {
        if(!active || Screen.width!=440 || Screen.height!=1060)throw new Exception("Wait for the 440x1060 GameView");
        Send(EventType.MouseDown,337,919);
        releaseAt=EditorApplication.timeSinceStartup+.25;
        EditorApplication.update += ReleaseWeapon;
        weaponClicked=true;return "Sent portrait Resonance button events";
    }
    // Call after Begin has changed resolution; allow a repaint before ClickWeapon.
    public static object Fit()
    {
        view.GetType().GetMethod("SnapZoom",Flags).Invoke(view,new object[]{(float)view.GetType().GetProperty("minScale",Flags).GetValue(view)});
        view.Repaint();return "Fit portrait to visible GameView";
    }
    static void ReleaseWeapon()
    {
        if(EditorApplication.timeSinceStartup<releaseAt)return;
        EditorApplication.update-=ReleaseWeapon;
        if(active)Send(EventType.MouseUp,337,919);
    }
    public static object Scroll()
    {
        if(!active)throw new Exception("Begin first");
        Send(EventType.ScrollWheel,300,980,50);return "Sent portrait scroll";
    }
    public static object Finish()
    {
        if(!active)throw new Exception("Begin first");
        var game=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();
        var scroll=(Vector2)typeof(BokumyakuGame).GetField("panelScroll",Flags).GetValue(game);
        var report=new{passed=weaponClicked&&game.Run.Weapon==WeaponKind.Resonance&&scroll.y>0,width=Screen.width,height=Screen.height,weapon=game.Run.Weapon.ToString(),scrollY=scroll.y,method="Editor GameView fixed resolution + synthetic button/wheel; not mobile hardware or physical input"};
        File.WriteAllText(Path.GetFullPath("../docs/evidence/bokumyaku-portrait-v7.json"),Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
        view.GetType().GetProperty("selectedSizeIndex",Flags).SetValue(view,previousIndex);
        group.GetType().GetMethod("RemoveCustomSize",Flags).Invoke(group,new object[]{addedIndex});
        view.GetType().GetMethod("SnapZoom",Flags).Invoke(view,new object[]{previousScale});
        active=false;view.Repaint();return report;
    }
}
