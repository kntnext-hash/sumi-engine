using System;
using System.IO;
using System.Linq;
using UnityEngine;
using TMPro;
using Bokumyaku.Presentation;
using Bokumyaku.Simulation;
public static class ResultRewardCheck
{
    public static object Run()
    {
        var g = UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();
        if (!g.SavePath.Contains("ink-qa-")) throw new Exception("Isolated save required");
        g.TryStartRun(42); g.SetPaused(true);
        for(int i=0;i<30000 && g.Run.State==RunStatus.Running;i++) g.Run.Tick();
        if(g.Run.State==RunStatus.Running) throw new Exception("Run did not end");
        string pending=g.BattleResultText;
        if(!pending.Contains("保存待ち")) throw new Exception("Unsettled reward presented as final");
        int before=g.Meta.Crystals;
        if(!g.SaveProgress()) throw new Exception("Settlement failed");
        var r=g.Campaign.LastResult;
        int total=r.Crystals+r.MilestoneCrystals;
        if(r.MilestoneCrystals<=0 || g.Meta.Crystals-before!=total || !g.BattleResultText.Contains("結晶 "+total.ToString("N0"))) throw new Exception("Reward mismatch");
        var label=UnityEngine.Object.FindFirstObjectByType<BokumyakuBattleHud>().GetComponentsInChildren<TextMeshProUGUI>(true).Single(t=>t.name=="Result");
        label.transform.parent.gameObject.SetActive(true); label.text=g.BattleResultText;
        Canvas.ForceUpdateCanvases(); label.ForceMeshUpdate();
        if(!label.font.HasCharacters(label.text)) throw new Exception("Missing result glyphs: "+label.text);
        if(label.isTextOverflowing) throw new Exception("Result overflows");
        var report=new{passed=true,pending,settled=g.BattleResultText,total,width=Screen.width,height=Screen.height};
        File.WriteAllText(Path.GetFullPath("../docs/evidence/result-reward.json"),Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("../docs/evidence/result-reward.png"));
        return report;
    }
}
