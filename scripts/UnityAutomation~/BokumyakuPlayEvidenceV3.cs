using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Bokumyaku.Presentation;
using Bokumyaku.Simulation;
using Bokumyaku.Persistence;

public static class BokumyakuPlayEvidenceV3
{
    static BokumyakuGame game;
    static EditorWindow view;
    static Queue<Action> actions;
    static double next, started;
    static int firstFrame, pauseTick, stage, marker;
    static MetaProgress original;
    static string originalPath;
    static List<object> checks;
    static bool passed, sawField;
    static bool[] sawAttack;
    const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    public static string Run()
    {
        if (!EditorApplication.isPlaying) throw new Exception("Play game scene first");
        game = UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();
        if (game == null || !game.AwaitingStart) throw new Exception("Fresh game scene required; active runs are protected");
        original = game.Meta; originalPath = game.SavePathOverride;
        game.Meta = new MetaProgress();
        game.SavePathOverride = Path.GetFullPath("../logs/bokumyaku-qa-v3/progress.json");
        view = EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")); view.Focus();
        firstFrame = Time.frameCount; started = EditorApplication.timeSinceStartup; next = started;
        stage = 0; passed = true; sawField = false; sawAttack = new bool[3];
        checks = new List<object>(); actions = new Queue<Action>();
        Click(955, 247); Click(795, 355); Click(955, 458);
        actions.Enqueue(() => Check("出撃前の武器と構成", game.SelectedWeapon == WeaponKind.Bleed && game.SelectedBuild == BuildPreset.Farming && game.SelectedPolicy == GrowthPolicy.Focus, new { weapon=game.SelectedWeapon.ToString(),build=game.SelectedBuild.ToString(),policy=game.SelectedPolicy.ToString() }));
        Click(271, 500);
        actions.Enqueue(() => Check("出撃後も選択保持", !game.AwaitingStart && game.Run.Weapon == WeaponKind.Bleed && game.Run.Build == BuildPreset.Farming && game.Run.Policy == GrowthPolicy.Focus, game.Run.Weapon.ToString()));
        Click(620, 178);
        actions.Enqueue(() => { pauseTick=game.Run.TickCount; Check("一時停止",game.IsPaused,pauseTick); });
        actions.Enqueue(() => Check("停止中tick維持",game.Run.TickCount==pauseTick,game.Run.TickCount));
        Click(800, 178); Click(620, 178);
        actions.Enqueue(() => Check("再開と2倍速",!game.IsPaused && game.Speed==2,game.Speed));
        EditorApplication.update += Tick;
        return "v0.3 probe: three weapons via GameView, live combat, natural defeat/save/meta/retry; <=180 sec";
    }
    static void Check(string name,bool ok,object detail) { checks.Add(new{name,passed=ok,detail});passed&=ok; }
    static void Click(float x,float y) { actions.Enqueue(()=>Send(EventType.MouseDown,x,y));actions.Enqueue(()=>Send(EventType.MouseUp,x,y)); }
    static void Send(EventType type,float x,float y,float scroll=0)
    {
        float scale=Mathf.Min(Screen.width/1080f,Screen.height/760f);
        var p=new Vector2((Screen.width-1080*scale)/2+x*scale,(Screen.height-760*scale)/2+y*scale);
        var r=(Rect)view.GetType().GetProperty("targetInParent",Flags).GetValue(view);
        p=new Vector2(r.x+p.x*r.width/Screen.width,r.y+p.y*r.height/Screen.height);
        view.SendEvent(new Event{type=type,button=0,mousePosition=p,delta=new Vector2(0,scroll)});
    }
    static void Tick()
    {
        if(game!=null && game.Run!=null)
        {
            foreach(var a in game.Run.Attacks) sawAttack[(int)a.Weapon]=true;
            foreach(var f in game.Run.Fields) sawField |= f.Weapon==WeaponKind.Bleed;
        }
        if(EditorApplication.timeSinceStartup<next)return; next=EditorApplication.timeSinceStartup+.15;
        try
        {
            if(!EditorApplication.isPlaying || game==null)throw new Exception("Play stopped during probe");
            if(EditorApplication.timeSinceStartup-started>180)throw new TimeoutException("UI probe exceeded 180 seconds");
            if(actions.Count>0){actions.Dequeue()();return;}
            if(stage==0)
            {
                if(game.Run.TickCount<900)return;
                Check("滲墨の攻撃と実領域",sawAttack[2]&&sawField,new{attack=sawAttack[2],field=sawField,tick=game.Run.TickCount});
                game.SetPaused(true);
                ScreenCapture.CaptureScreenshot(Path.GetFullPath("../docs/evidence/bokumyaku-bleed-v3.png"));
                Click(795,247);
                actions.Enqueue(()=>{Check("貫筆へ切替",game.Run.Weapon==WeaponKind.Pierce,game.Run.Weapon.ToString());marker=game.Run.TickCount;game.SetPaused(false);});
                stage=1;return;
            }
            if(stage==1)
            {
                if(game.Run.TickCount<marker+600)return;
                Check("貫筆の実攻撃",sawAttack[1],game.Run.TickCount);
                Click(635,247);
                actions.Enqueue(()=>{Check("点墨へ切替",game.Run.Weapon==WeaponKind.Point,game.Run.Weapon.ToString());marker=game.Run.TickCount;});
                stage=2;return;
            }
            if(stage==2)
            {
                if(game.Run.TickCount<marker+600)return;
                Check("点墨の実攻撃",sawAttack[0],game.Run.TickCount);
                Check("自動購入と枝成長",game.Run.UpgradeLevels[3]>0 && game.Run.Branches.Count>4,new{levels=game.Run.UpgradeLevels,branches=game.Run.Branches.Count});
                game.SetPaused(true);
                actions.Enqueue(()=>Send(EventType.ScrollWheel,980,600,30));
                actions.Enqueue(()=>Check("下部強化へスクロール",((Vector2)typeof(BokumyakuGame).GetField("panelScroll",Flags).GetValue(game)).y>0,typeof(BokumyakuGame).GetField("panelScroll",Flags).GetValue(game).ToString()));
                actions.Enqueue(()=>Send(EventType.ScrollWheel,980,600,-100));
                actions.Enqueue(()=>{game.SetBuild(BuildPreset.Manual);game.SetWeapon(WeaponKind.Point);game.StartRun(43);game.SetSpeed(2);});
                stage=3;return;
            }
            if(stage==3)
            {
                if(game.Run.State==RunStatus.Running)return;
                Check("実時間の自然敗北",game.Run.State==RunStatus.Defeated,new{game.Run.TickCount,game.Run.CompletedWave,game.Run.CrystalsReward});
                string warning;var saved=MetaSaveStore.Load(game.SavePath,out warning);
                Check("自然敗北の報酬保存",warning=="" && saved.Crystals==game.Meta.Crystals && game.Meta.Crystals>0,saved.Crystals);
                game.SetPaused(true);
                ScreenCapture.CaptureScreenshot(Path.GetFullPath("../docs/evidence/bokumyaku-result-v3.png"));
                actions.Enqueue(()=>Check("恒久強化購入API",game.BuyMeta(MetaUpgrade.Damage),game.Meta.Damage));
                actions.Enqueue(()=>{string w;var m=MetaSaveStore.Load(game.SavePath,out w);Check("恒久強化読戻し",w=="" && m.Damage==1 && m.Crystals==game.Meta.Crystals,m.Damage);});
                Click(271,500);
                actions.Enqueue(()=>{Check("再出撃ボタンと選択保持",game.Run.State==RunStatus.Running && game.Run.TickCount<60 && game.Run.Weapon==WeaponKind.Point && game.Run.Policy==GrowthPolicy.Focus,new{game.Run.TickCount,weapon=game.Run.Weapon.ToString(),policy=game.Run.Policy.ToString()});game.SetPaused(true);});
                stage=4;return;
            }
        }
        catch(Exception e){Check("exception",false,e.ToString());}
        EditorApplication.update-=Tick;
        Check("フレーム進行",Time.frameCount>firstFrame,new{firstFrame,endFrame=Time.frameCount});
        File.WriteAllText(Path.GetFullPath("../docs/evidence/bokumyaku-play-v3.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new{passed,method="GameView.SendEvent + live Update; isolated save; public API sets natural-defeat fixture and purchases meta; physical mouse not tested",checks},Newtonsoft.Json.Formatting.Indented));
        if(game!=null){game.Meta=original;game.SavePathOverride=originalPath;game.SetPaused(true);}
    }
}
