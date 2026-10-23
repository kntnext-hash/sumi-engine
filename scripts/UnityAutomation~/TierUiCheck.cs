using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Bokumyaku.Presentation;
using Bokumyaku.Persistence;
public static class TierUiCheck
{
    public static object Run()
    {
        var g=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();
        if(g==null||!g.SavePath.Contains("ink-qa-"))throw new Exception("Isolated QA required");
        var menu=UnityEngine.Object.FindFirstObjectByType<BokumyakuMenuHud>();
        menu.Refresh();var buttons=menu.GetComponentsInChildren<Button>(true);
        if(buttons.Single(b=>b.name=="MenuTier2").interactable||buttons.Single(b=>b.name=="MenuTier3").interactable)throw new Exception("Initial lock failed");
        if(g.SelectTier(0)||g.SelectTier(4))throw new Exception("Invalid tier accepted");
        // Artificial completed-wave fixture tests presentation/migration, not real progression balance.
        string isolated=Path.Combine(Path.GetDirectoryName(g.SavePath),"tier-v9.json");g.SavePathOverride=isolated;
        var old=new CampaignProgress{SchemaVersion=3,Config="bokumyaku-campaign-v3",BestWave=100,ClaimedMilestoneWave=100};
        Directory.CreateDirectory(Path.GetDirectoryName(isolated));string source=JsonUtility.ToJson(old);File.WriteAllText(g.PreviousV8CampaignPath,source);
        File.WriteAllText(g.PreviousV8CampaignPath+".audio.json","{\"Master\":0.25,\"Music\":0.5,\"Effects\":0.75,\"Muted\":true}");
        if(!g.TryReloadProgress())throw new Exception("Migration failed");g.ReloadAudioSettings();menu.Refresh();
        if(!g.AudioSettings.Muted||Math.Abs(g.AudioSettings.Master-.25f)>.001)throw new Exception("Audio preference migration failed");
        var second=buttons.Single(b=>b.name=="MenuTier2");if(!second.interactable)throw new Exception("Tier2 not unlocked");second.onClick.Invoke();
        if(g.SelectedTier!=2||buttons.Single(b=>b.name=="MenuTier3").interactable)throw new Exception("Selection failed");
        Canvas.ForceUpdateCanvases();
        foreach(var b in buttons.Where(b=>b.name.StartsWith("MenuTier")))foreach(var t in b.GetComponentsInChildren<TextMeshProUGUI>()){t.ForceMeshUpdate();if(!t.font.HasCharacters(t.text)||t.isTextOverflowing)throw new Exception("Tier text overflow/glyph: "+t.text);}
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("../docs/evidence/tier-home.png"));
        return new{selected=g.SelectedTier,oldSourceIntact=File.ReadAllText(g.PreviousV8CampaignPath)==source,width=Screen.width,height=Screen.height};
    }
    public static object Battle()
    {
        var g=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();if(!g.SavePath.Contains("ink-qa-"))throw new Exception("Isolation");
        if(!g.TryStartRun(42))throw new Exception("Start failed");g.SetPaused(true);if(g.Run.Tier!=2||g.SelectTier(1))throw new Exception("Active tier changed");
        g.SaveProgress();int tick=g.Run.TickCount;if(!g.TryReloadProgress()||g.Run.Tier!=2||g.Run.TickCount!=tick)throw new Exception("Resume tier failed");g.ResumeRun();g.SetPaused(true);
        var h=UnityEngine.Object.FindFirstObjectByType<BokumyakuBattleHud>();typeof(BokumyakuBattleHud).GetMethod("Refresh",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(h,null);
        var label=h.GetComponentsInChildren<TextMeshProUGUI>(true).Single(t=>t.name=="Brand");label.ForceMeshUpdate();if(!label.text.Contains("二段")||label.isTextOverflowing||!label.font.HasCharacters(label.text))throw new Exception("HUD rank failed");
        var report=new{passed=true,width=Screen.width,height=Screen.height,tier=g.Run.Tier,rules=g.Run.RulesVersion,tick,initialLocks=true,selection=true,activeSwitchRejected=true,saveResume=true,oldAudioPreserved=true,method="Editor uGUI callbacks at1080x2400; synthetic100-wave migration fixture; no real touch or progression balance claim"};
        File.WriteAllText(Path.GetFullPath("../docs/evidence/tier-ui.json"),Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));ScreenCapture.CaptureScreenshot(Path.GetFullPath("../docs/evidence/tier-battle.png"));return report;
    }
}
