using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Bokumyaku.Presentation;
using Bokumyaku.Simulation;

// Temporary GameView size through Editor API; does not edit project or scene YAML.
public static class BokumyakuPortraitEvidence
{
    const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;
    static EditorWindow view;
    static object group;
    static int previousIndex, addedIndex;
    static bool active, weaponClicked;
    public static object Begin()
    {
        var game=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();
        if(active || game==null || !game.IsPaused)throw new Exception("Use the completed paused QA run");
        var asm=typeof(Editor).Assembly;
        view=EditorWindow.GetWindow(asm.GetType("UnityEditor.GameView"));
        previousIndex=(int)view.GetType().GetProperty("selectedSizeIndex",Flags).GetValue(view);
        var sizesType=asm.GetType("UnityEditor.GameViewSizes");
        var sizes=sizesType.GetProperty("instance",Flags).GetValue(null);
        group=sizesType.GetProperty("currentGroup",Flags).GetValue(sizes);
        addedIndex=(int)group.GetType().GetMethod("GetTotalCount",Flags).Invoke(group,null);
        var kind=Enum.Parse(asm.GetType("UnityEditor.GameViewSizeType"),"FixedResolution");
        var size=Activator.CreateInstance(asm.GetType("UnityEditor.GameViewSize"),Flags,null,new object[]{kind,440,1060,"Codex Bokumyaku portrait QA"},null);
        group.GetType().GetMethod("AddCustomSize",Flags).Invoke(group,new[]{size});
        view.GetType().GetProperty("selectedSizeIndex",Flags).SetValue(view,addedIndex);
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
        Send(EventType.MouseDown,337,785);Send(EventType.MouseUp,337,785);
        weaponClicked=true;return "Sent portrait Bleed button events";
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
        var report=new{passed=weaponClicked&&game.Run.Weapon==WeaponKind.Bleed&&scroll.y>0,width=Screen.width,height=Screen.height,weapon=game.Run.Weapon.ToString(),scrollY=scroll.y,method="Editor GameView fixed resolution + synthetic button/wheel; not mobile hardware or physical input"};
        File.WriteAllText(Path.GetFullPath("../docs/evidence/bokumyaku-portrait-v3.json"),Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
        view.GetType().GetProperty("selectedSizeIndex",Flags).SetValue(view,previousIndex);
        group.GetType().GetMethod("RemoveCustomSize",Flags).Invoke(group,new object[]{addedIndex});
        active=false;view.Repaint();return report;
    }
}
