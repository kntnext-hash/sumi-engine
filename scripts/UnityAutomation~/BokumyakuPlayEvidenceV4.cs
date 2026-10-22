using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Bokumyaku.Presentation;
using Bokumyaku.Simulation;
using Bokumyaku.Persistence;
using ConnectPiece.SumiEngine;

// Combat fixture is advanced to one tick before natural defeat; live Update owns
// the final tick, reward, save, countdown and next run. Saves are isolated.
public static class BokumyakuPlayEvidenceV4
{
    static BokumyakuGame game;
    static EditorWindow view;
    static Queue<Action> actions;
    static List<object> checks;
    static MetaProgress original;
    static string originalPath, path, healthy;
    static double started, next, pausedRemaining;
    static int stage, firstFrame, rebuilds, seed;
    static bool passed;
    const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    public static string Run()
    {
        game=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();
        if(!EditorApplication.isPlaying||game==null||!game.AwaitingStart)throw new Exception("Fresh game required");
        original=game.Meta;originalPath=game.SavePathOverride;
        path=Path.GetFullPath("../logs/bokumyaku-qa-v4/"+Guid.NewGuid().ToString("N")+"/progress.json");
        game.Meta=new MetaProgress();game.SavePathOverride=path;
        view=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));view.Focus();
        checks=new List<object>();actions=new Queue<Action>();passed=true;stage=0;
        started=next=EditorApplication.timeSinceStartup;firstFrame=Time.frameCount;
        Check("initial auto repeat OFF",!game.AutoRepeatEnabled,null);
        Click(630,217);actions.Enqueue(()=>Check("auto repeat ON button",game.AutoRepeatEnabled&&!game.AutoRepeatPending,null));
        Click(630,217);actions.Enqueue(()=>Check("auto repeat OFF button",!game.AutoRepeatEnabled,null));
        Click(955,331);Click(795,461);Click(955,564);Click(271,500);Click(800,178);
        actions.Enqueue(()=>Check("art weapon/build/policy buttons",game.Run.Weapon==WeaponKind.Bleed&&game.Run.Build==BuildPreset.Farming&&game.Run.Policy==GrowthPolicy.Focus&&game.Speed==2,new{weapon=game.Run.Weapon.ToString(),build=game.Run.Build.ToString(),policy=game.Run.Policy.ToString(),game.Speed}));
        EditorApplication.update+=Tick;return "Started v0.4 isolated live UI/art/repeat probe; bounded 90 seconds";
    }
    static void Check(string name,bool ok,object detail){checks.Add(new{name,passed=ok,detail});passed&=ok;}
    static void Click(float x,float y){actions.Enqueue(()=>Send(EventType.MouseDown,x,y));actions.Enqueue(()=>Send(EventType.MouseUp,x,y));}
    static void Send(EventType type,float x,float y)
    {
        float s=Mathf.Min(Screen.width/1080f,Screen.height/760f);
        var p=new Vector2((Screen.width-1080*s)/2+x*s,(Screen.height-760*s)/2+y*s);
        var r=(Rect)view.GetType().GetProperty("targetInParent",Flags).GetValue(view);
        p=new Vector2(r.x+p.x*r.width/Screen.width,r.y+p.y*r.height/Screen.height);
        view.SendEvent(new Event{type=type,button=0,mousePosition=p});
    }
    static void DefeatFixture(int runSeed)
    {
        game.SetBuild(BuildPreset.Manual);game.SetWeapon(WeaponKind.Point);game.SetPolicy(GrowthPolicy.Focus);
        game.StartRun(runSeed);game.SetSpeed(2);game.SetAutoRepeat(true);
        var expected=new RunSimulator(runSeed,game.Meta);expected.SetPolicy(GrowthPolicy.Focus);
        while(expected.State==RunStatus.Running)expected.Tick();
        if(expected.State!=RunStatus.Defeated)throw new Exception("Fixture did not naturally lose");
        while(game.Run.TickCount<expected.TickCount-1)game.Run.Tick();
        seed=runSeed;
    }
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.18;
        try
        {
            if(game==null||!EditorApplication.isPlaying)throw new Exception("Play interrupted");
            if(EditorApplication.timeSinceStartup-started>90)throw new TimeoutException("v4 probe timeout");
            if(actions.Count>0){actions.Dequeue()();return;}
            if(stage==0)
            {
                if(game.Run.TickCount<900)return;
                game.SetPaused(true);rebuilds=game.BranchOverlayRebuilds;
                var pixels=game.BranchOverlay.GetPixels32();var canvas=game.GetComponent<SumiCanvas>();int mismatches=0;
                for(int y=0;y<canvas.Core.Paper.Height;y++)for(int x=0;x<canvas.Core.Paper.Width;x++)
                {
                    var alpha=(byte)Mathf.RoundToInt(Mathf.Clamp01(1-Mathf.Exp(-canvas.displayDensity*canvas.Core.Paper.PigmentAt(x,y)))*255);
                    if(pixels[y*canvas.Core.Paper.Width+x].a!=alpha)mismatches++;
                }
                Check("six art textures and transparent ink",game.LoadedArtCount==6&&pixels.Any(p=>p.a==0)&&pixels.Any(p=>p.a>0)&&mismatches==0,new{game.LoadedArtCount,transparent=pixels.Count(p=>p.a==0),ink=pixels.Count(p=>p.a>0),mismatches});
                ScreenCapture.CaptureScreenshot(Path.GetFullPath("../docs/evidence/bokumyaku-gameplay-v4.png"));
                actions.Enqueue(()=>Check("paused overlay does not rebuild",game.BranchOverlayRebuilds==rebuilds,rebuilds));
                actions.Enqueue(()=>DefeatFixture(42));stage=1;return;
            }
            if(stage==1)
            {
                if(!game.AutoRepeatPending)return;
                game.SetPaused(true);pausedRemaining=game.AutoRepeatSeconds;
                string warning;var saved=MetaSaveStore.Load(path,out warning);
                Check("defeat reward saved before countdown",game.Run.State==RunStatus.Defeated&&game.CompletedRuns==1&&game.SessionCrystals>0&&saved.Crystals==game.SessionCrystals&&warning=="",new{game.CompletedRuns,game.SessionCrystals,game.AutoRepeatSeconds});
                actions.Enqueue(()=>Check("pause freezes countdown",game.AutoRepeatSeconds==pausedRemaining,game.AutoRepeatSeconds));
                actions.Enqueue(()=>Check("repeat save neither resets nor duplicates",game.SaveProgress()&&game.AutoRepeatSeconds==pausedRemaining&&game.CompletedRuns==1&&game.Meta.Crystals==game.SessionCrystals,null));
                Click(630,217);actions.Enqueue(()=>Check("OFF cancels pending",!game.AutoRepeatEnabled&&!game.AutoRepeatPending&&game.Run.Seed==42,null));
                Click(630,217);actions.Enqueue(()=>{Check("ON re-arms saved defeat",game.AutoRepeatPending,game.AutoRepeatSeconds);game.SetPaused(false);});
                stage=2;return;
            }
            if(stage==2)
            {
                if(game.Run.Seed==seed)return;
                game.SetPaused(true);
                Check("live auto repeat preserves settings",game.Run.Seed==43&&game.Run.State==RunStatus.Running&&game.Speed==2&&game.SelectedPolicy==GrowthPolicy.Focus&&game.SelectedBuild==BuildPreset.Manual&&game.SelectedWeapon==WeaponKind.Point&&game.CompletedRuns==1&&!game.AutoRepeatPending,new{game.Run.Seed,game.Speed,game.CompletedRuns});
                healthy=File.ReadAllText(path);var invalid=Path.Combine(Path.GetDirectoryName(path),"directory-as-save");Directory.CreateDirectory(invalid);
                game.SavePathOverride=invalid;DefeatFixture(44);stage=3;return;
            }
            if(stage==3)
            {
                if(game.Run.State==RunStatus.Running||game.CompletedRuns<2)return;
                game.SetPaused(true);
                Check("failed save stops automatic restart",!game.AutoRepeatPending&&game.Run.Seed==44&&File.ReadAllText(path)==healthy&&game.CompletedRuns==2,new{game.AutoRepeatEnabled,game.AutoRepeatPending,game.CompletedRuns});
                actions.Enqueue(()=>Check("failed save stays stopped",!game.AutoRepeatPending&&game.Run.Seed==44,null));
                actions.Enqueue(()=>{game.SavePathOverride=path;Check("explicit save retry re-arms",game.SaveProgress()&&game.AutoRepeatPending,null);pausedRemaining=game.AutoRepeatSeconds;});
                actions.Enqueue(()=>Check("successful retry counted once",game.SaveProgress()&&game.CompletedRuns==2&&game.Meta.Crystals==game.SessionCrystals&&game.AutoRepeatSeconds==pausedRemaining,new{game.CompletedRuns,game.SessionCrystals}));
                Click(271,500);actions.Enqueue(()=>{Check("manual restart cancels pending",game.Run.State==RunStatus.Running&&!game.AutoRepeatPending&&game.CompletedRuns==2,game.Run.Seed);game.SetPaused(true);});
                stage=4;return;
            }
        }
        catch(Exception e){Check("exception",false,e.ToString());}
        EditorApplication.update-=Tick;Check("live frames",Time.frameCount>firstFrame,new{firstFrame,endFrame=Time.frameCount});
        File.WriteAllText(Path.GetFullPath("../docs/evidence/bokumyaku-play-v4.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new{passed,method="GameView synthetic clicks; live Update; combat accelerated to final natural-defeat tick for repeat fixtures; isolated saves; one intentional save-failure warning; no physical input/mobile test",checks},Newtonsoft.Json.Formatting.Indented));
        if(game!=null){game.SetAutoRepeat(false);game.SetPaused(true);game.Meta=original;game.SavePathOverride=originalPath;}
    }
}
