using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Bokumyaku.Presentation;
using Bokumyaku.Simulation;
using Bokumyaku.Persistence;

// Isolated Editor integration probe. Final defeat ticks and restart timers run in live Update.
// The combat fixture is accelerated; this is not an unattended endurance/device test.
public static class BokumyakuPlayEvidenceV6
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static BokumyakuGame game;
    static EditorWindow view;
    static Queue<Action> actions;
    static List<object> checks;
    static FileStream locked;
    static double started,next,remaining;
    static int stage,firstFrame,pausedTick,round;
    static bool passed;
    static string saved;
    public static object Arm()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play first");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.isDirty || !scene.path.EndsWith("Bokumyaku.unity"))throw new Exception("Clean Bokumyaku scene required");
        string path=Path.GetFullPath("../logs/bokumyaku-qa-v6/"+Guid.NewGuid().ToString("N")+"/campaign.json");
        SessionState.SetString("Bokumyaku.QaSavePath",path);
        Application.runInBackground=true;
        view=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));view.maximized=true;view.Focus();
        EditorApplication.isPlaying=true;return new{path};
    }
    public static string Run()
    {
        game=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();
        if(!EditorApplication.isPlaying||game==null||!game.AwaitingStart||!game.SavePath.Contains("bokumyaku-qa-v6"))throw new Exception("Fresh isolated game required");
        view=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));view.Focus();
        actions=new Queue<Action>();checks=new List<object>();passed=true;stage=0;round=0;
        firstFrame=Time.frameCount;started=next=EditorApplication.timeSinceStartup;
        Check("isolated fresh home",game.Page==BokumyakuGame.GamePage.Home&&game.Meta.Crystals==0&&!game.AutoRepeatEnabled,game.SavePath);
        Check("fresh recommended farming and seven upgrades",game.SelectedBuild==BuildPreset.Farming&&game.Run.RulesVersion==2&&game.Run.UpgradeLevels.Length==7,null);
        for(int i=0;i<6;i++){int kind=i;Click(634+160.7f*(i%3),367+78*(i/3));actions.Enqueue(()=>Check("weapon button "+kind,(int)game.SelectedWeapon==kind,null));}
        actions.Enqueue(()=>{game.SetBuild(BuildPreset.Farming);game.SetPolicy(GrowthPolicy.Intercept);game.SetSpeed(2);Scroll(660);});
        Click(902,535);actions.Enqueue(()=>Check("slot save button",game.Campaign.Slots[0].Weapon==WeaponKind.Resonance&&game.Campaign.Slots[0].Speed==2,null));
        actions.Enqueue(()=>{game.SetWeapon(WeaponKind.Point);game.SetBuild(BuildPreset.Manual);game.SetPolicy(GrowthPolicy.Focus);game.SetSpeed(1);});
        Click(988,535);actions.Enqueue(()=>Check("slot load button",game.SelectedWeapon==WeaponKind.Resonance&&game.SelectedBuild==BuildPreset.Farming&&game.SelectedPolicy==GrowthPolicy.Intercept&&game.Speed==2,null));
        actions.Enqueue(()=>Scroll(0));Click(271,380);Click(800,214);
        actions.Enqueue(()=>Check("home sortie button",!game.AwaitingStart&&game.Page==BokumyakuGame.GamePage.Battle&&game.Speed==2,null));
        EditorApplication.update+=Tick;return "Started v6 UI/campaign probe (120 second bound)";
    }
    static void Check(string name,bool ok,object detail){checks.Add(new{name,passed=ok,detail});passed&=ok;}
    static void Scroll(float y){typeof(BokumyakuGame).GetField("panelScroll",Flags).SetValue(game,new Vector2(0,y));}
    static void Click(float x,float y){actions.Enqueue(()=>Send(EventType.MouseDown,x,y));actions.Enqueue(()=>Send(EventType.MouseUp,x,y));}
    static void Send(EventType type,float x,float y)
    {
        float s=Mathf.Min(Screen.width/1080f,Screen.height/760f);
        var p=new Vector2((Screen.width-1080*s)/2+x*s,(Screen.height-760*s)/2+y*s);
        var r=(Rect)view.GetType().GetProperty("targetInParent",Flags).GetValue(view);
        p=new Vector2(r.x+p.x*r.width/Screen.width,r.y+p.y*r.height/Screen.height);
        view.SendEvent(new Event{type=type,button=0,mousePosition=p});
    }
    static void Fixture(int seed,bool fail)
    {
        game.SetAutoRepeat(false);game.SetWeapon(WeaponKind.Point);game.SetBuild(BuildPreset.Manual);game.SetPolicy(GrowthPolicy.Focus);
        if(!game.TryStartRun(seed))throw new Exception("Fixture start failed");game.SetSpeed(2);game.SetAutoRepeat(true);
        var expected=RunSimulator.Restore(game.Run.CaptureState());while(expected.State==RunStatus.Running)expected.Tick();
        if(expected.State!=RunStatus.Defeated)throw new Exception("Fixture expected defeat");
        while(game.Run.TickCount<expected.TickCount-1)game.Run.Tick();
        if(fail)locked=new FileStream(game.SavePath,FileMode.Open,FileAccess.Read,FileShare.None);
    }
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.20;
        try
        {
            if(game==null||!EditorApplication.isPlaying)throw new Exception("Play interrupted");
            if(EditorApplication.timeSinceStartup-started>120)throw new TimeoutException("v6 integration timeout");
            if(actions.Count>0){actions.Dequeue()();return;}
            if(stage==0)
            {
                if(game.Run.TickCount<900)return;
                Click(637,214);actions.Enqueue(()=>{
                    pausedTick=game.Run.TickCount;saved=JsonUtility.ToJson(game.Run.CaptureState());
                    Check("pause button and checkpoint",game.IsPaused&&game.Campaign.HasActiveRun,null);
                    Check("reload exact paused run",game.TryReloadProgress()&&game.IsPaused&&game.ResumeAvailable&&game.Speed==2&&JsonUtility.ToJson(game.Run.CaptureState())==saved,null);
                });
                actions.Enqueue(()=>Check("paused time stays fixed",game.Run.TickCount==pausedTick,pausedTick));
                Click(271,380);actions.Enqueue(()=>Check("home continue button",!game.IsPaused&&game.Page==BokumyakuGame.GamePage.Battle,null));
                actions.Enqueue(()=>{game.SetPaused(true);ScreenCapture.CaptureScreenshot(Path.GetFullPath("../docs/evidence/bokumyaku-gameplay-v6.png"));});
                actions.Enqueue(()=>{Check("abandon has no award",game.AbandonRun()&&game.Meta.Crystals==0&&game.Campaign.TotalRuns==0,null);Fixture(42,false);});
                stage=1;return;
            }
            if(stage==1)
            {
                if(!game.AutoRepeatPending)return;
                game.SetPaused(true);remaining=game.AutoRepeatSeconds;round++;
                Check("settlement round "+round,game.Campaign.TotalRuns==round&&game.CompletedRuns==round&&game.LastResult!=null&&game.Meta.Crystals>0,new{game.CompletedRuns,game.Meta.Crystals,game.BestWave});
                int money=game.Meta.Crystals;long revision=game.Campaign.Revision;
                actions.Enqueue(()=>Check("repeat save pays once "+round,game.SaveProgress()&&game.Meta.Crystals==money&&game.Campaign.TotalRuns==round&&game.AutoRepeatSeconds==remaining,new{revision,after=game.Campaign.Revision}));
                if(round==1)
                {
                    Click(867,124);Click(980,395);actions.Enqueue(()=>Check("research tab and purchase button",game.Page==BokumyakuGame.GamePage.Research&&game.Meta.Damage==1,null));
                    Click(992,124);actions.Enqueue(()=>Check("records tab",game.Page==BokumyakuGame.GamePage.Records&&game.Campaign.History.Count==1,null));
                    Click(742,124);Click(630,253);actions.Enqueue(()=>Check("auto OFF cancels",!game.AutoRepeatEnabled&&!game.AutoRepeatPending,null));
                    Click(630,253);actions.Enqueue(()=>{Check("auto ON arms saved result",game.AutoRepeatPending,null);game.SetPaused(false);});stage=2;
                }
                else if(round==2){actions.Enqueue(()=>Fixture(44,true));stage=3;}
                else {actions.Enqueue(()=>{game.SetAutoRepeat(false);Check("reload three completed runs",game.TryReloadProgress()&&game.Campaign.TotalRuns==3&&game.Campaign.History.Count==3&&game.Meta.Damage==1&&!game.ResumeAvailable,null);});stage=4;}
                return;
            }
            if(stage==2)
            {
                if(game.Run.Seed==42)return;game.SetPaused(true);
                Check("live auto restart uses permanent upgrade",game.Run.Seed==43&&game.Speed==2&&game.Run.CaptureState().meta.Damage==1&&game.CompletedRuns==1,null);
                // Use this automatically created second run, accelerated to its final live tick.
                var expected=RunSimulator.Restore(game.Run.CaptureState());while(expected.State==RunStatus.Running)expected.Tick();
                while(game.Run.TickCount<expected.TickCount-1)game.Run.Tick();game.SetPaused(false);stage=1;return;
            }
            if(stage==3)
            {
                if(game.Run.State==RunStatus.Running)return;
                Check("locked save stops and withholds reward",game.IsPaused&&!game.AutoRepeatPending&&game.CompletedRuns==2&&game.Campaign.HasActiveRun,null);
                locked.Dispose();locked=null;
                Check("save retry settles once",game.SaveProgress()&&game.CompletedRuns==3&&game.Campaign.TotalRuns==3&&game.AutoRepeatPending,null);
                round=2;stage=1;return;
            }
            if(stage==4)
            {
                // Real core damage and earned currency, then exercise repair through the presentation API.
                game.SetBuild(BuildPreset.Manual);game.SetWeapon(WeaponKind.Point);game.SetPolicy(GrowthPolicy.Focus);
                if(!game.TryStartRun(47))throw new Exception("repair fixture start");game.SetPaused(true);
                while(game.Run.State==RunStatus.Running&&(game.Run.CoreHp==10||game.Run.RunInk<game.Run.RepairCost))game.Run.Tick();
                int hp=game.Run.CoreHp;
                Check("repair and persisted checkpoint",game.TryRepair()&&game.Run.CoreHp>hp&&game.Campaign.ActiveRun.repairCount==1,null);
                int ink=game.Run.RunInk,cost=game.Run.UpgradeCost(UpgradeKind.Harvest);double points=game.Run.RewardPoints;
                game.SetPage(BokumyakuGame.GamePage.Battle);Scroll(660);
                Click(980,439);actions.Enqueue(()=>Check("harvest button pays and does not rewrite past kills",game.Run.UpgradeLevels[6]==1&&game.Run.RunInk==ink-cost&&game.Run.RewardPoints==points,null));
                actions.Enqueue(()=>{string exact=JsonUtility.ToJson(game.Run.CaptureState());Check("seven-upgrade checkpoint reload",game.SaveProgress()&&game.TryReloadProgress()&&game.Run.UpgradeLevels[6]==1&&JsonUtility.ToJson(game.Run.CaptureState())==exact,null);});
                actions.Enqueue(()=>{game.SetPage(BokumyakuGame.GamePage.Records);ScreenCapture.CaptureScreenshot(Path.GetFullPath("../docs/evidence/bokumyaku-records-v6.png"));});
                stage=5;return;
            }
            if(stage==5)
            {
                string v3=Path.Combine(Path.GetDirectoryName(game.SavePath),"migration-v3.json");
                var old=JsonUtility.FromJson<RunSnapshot>(File.ReadAllText("Assets/BokumyakuPrototype/Tests/Fixtures/v5-run-tick1800.json"));
                var campaign=new CampaignProgress{SchemaVersion=2,Config="bokumyaku-campaign-v2",Revision=3,Meta=old.meta,HasActiveRun=true,ActiveRun=old,ActiveRunId=Guid.NewGuid().ToString("N"),ActiveSpeed=2};
                string original=JsonUtility.ToJson(campaign,true);File.WriteAllText(v3+".v2",original);
                game.SavePathOverride=v3;
                Check("old real snapshot migrates without changing original",game.TryReloadProgress()&&game.Campaign.SchemaVersion==3&&game.Run.RulesVersion==1&&game.IsPaused&&game.Speed==2&&File.ReadAllText(v3+".v2")==original,null);
                game.SetPage(BokumyakuGame.GamePage.Battle);Scroll(660);int before=game.Run.RunInk;
                Click(980,439);actions.Enqueue(()=>Check("legacy harvest is disabled",game.Run.UpgradeLevels.Length==6&&game.Run.RunInk==before&&game.Run.RulesVersion==1,null));
                actions.Enqueue(()=>{Check("next sortie upgrades rules only after leaving old run",game.AbandonRun()&&game.TryStartRun(52)&&game.Run.RulesVersion==2&&game.Run.UpgradeLevels.Length==7,null);game.SetPaused(true);});
                stage=6;return;
            }
        }
        catch(Exception e){Check("exception",false,e.ToString());}
        Finish();
    }
    static void Finish()
    {
        EditorApplication.update-=Tick;locked?.Dispose();locked=null;
        Check("live frames",Time.frameCount>firstFrame,new{firstFrame,endFrame=Time.frameCount});
        File.WriteAllText(Path.GetFullPath("../docs/evidence/bokumyaku-play-v6.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new{passed,method="Synthetic GameView input + live Update; accelerated combat fixtures; isolated saves; one intentional locked-save warning; not physical/mobile/endurance input",checks},Newtonsoft.Json.Formatting.Indented));
        if(game!=null){game.SetAutoRepeat(false);game.SetPaused(true);}
    }
    // Call after leaving Play (the Editor restores its pre-Play settings on exit).
    public static object Cleanup()
    {
        if(EditorApplication.isPlaying)throw new Exception("Leave Play before cleanup");
        PlayerSettings.runInBackground=false;Application.runInBackground=false;
        SessionState.EraseString("Bokumyaku.QaSavePath");
        EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).maximized=false;
        AssetDatabase.SaveAssets();return "Restored non-background Editor/Player settings";
    }
}
