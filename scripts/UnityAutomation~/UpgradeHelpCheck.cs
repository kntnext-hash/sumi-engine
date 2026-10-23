using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Bokumyaku.Presentation;
public static class UpgradeHelpCheck
{
    public static object Run()
    {
        var g=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();
        if(!g.SavePath.Contains("ink-qa-"))throw new Exception("Isolated save required");
        g.TryStartRun(42);g.SetPaused(true);
        var h=UnityEngine.Object.FindFirstObjectByType<BokumyakuBattleHud>();
        // Allow Refresh to observe the new run before toggling help.
        typeof(BokumyakuBattleHud).GetMethod("Refresh",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(h,null);
        var buttons=h.GetComponentsInChildren<Button>(true);
        var help=buttons.Single(b=>b.name=="UpgradeHelp");
        int ink=g.Run.RunInk;
        help.onClick.Invoke();
        var texts=h.GetComponentsInChildren<TextMeshProUGUI>().Where(t=>t.name.StartsWith("UpgradeDescription")).ToArray();
        if(texts.Length!=7)throw new Exception("Descriptions missing");
        Canvas.ForceUpdateCanvases();
        foreach(var t in texts){t.ForceMeshUpdate();if(!t.font.HasCharacters(t.text)||t.isTextOverflowing)throw new Exception("Text fails: "+t.text);}
        help.onClick.Invoke();
        if(texts[0].gameObject.activeInHierarchy)throw new Exception("Close failed");
        help.onClick.Invoke();buttons.Single(b=>b.name=="ExtraUpgrades").onClick.Invoke();
        if(texts[0].gameObject.activeInHierarchy)throw new Exception("Panels overlap");
        help.onClick.Invoke();
        if(g.Run.RunInk!=ink)throw new Exception("Help spent ink");
        var report=new{passed=true,count=texts.Length,width=Screen.width,height=Screen.height,method="Editor button callbacks, glyph/overflow and panel exclusivity; physical touch not tested"};
        File.WriteAllText(Path.GetFullPath("../docs/evidence/upgrade-help.json"),Newtonsoft.Json.JsonConvert.SerializeObject(report));
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("../docs/evidence/upgrade-help.png"));return report;
    }
}
