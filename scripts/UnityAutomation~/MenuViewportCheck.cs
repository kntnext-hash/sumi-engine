using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using Bokumyaku.Presentation;
public static class MenuViewportCheck
{
    static BokumyakuGame Game(){var g=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();if(!g.SavePath.Contains("ink-qa-"))throw new Exception("Isolated required");return g;}
    public static object Home(){Game().SetPage(BokumyakuGame.GamePage.Home);return "Home opened";}
    public static object Run()
    {
        var g=Game();var menu=UnityEngine.Object.FindFirstObjectByType<BokumyakuMenuHud>();
        var pages=new[]{BokumyakuGame.GamePage.Research,BokumyakuGame.GamePage.Records,BokumyakuGame.GamePage.Home};
        foreach(int i in new[]{1,2,0})
        {
            Canvas.ForceUpdateCanvases();
            var b=menu.GetComponentsInChildren<UnityEngine.UI.Button>().Single(x=>x.name=="MenuTab"+i);
            var rect=(RectTransform)b.transform;
            var pos=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
            if(!BokumyakuViewport.Current.Contains(pos))throw new Exception("Outside portrait area");
            var p=new PointerEventData(EventSystem.current){position=pos,button=PointerEventData.InputButton.Left};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(p,hits);
            if(hits.Count==0||ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)!=b.gameObject)throw new Exception("Blocked tab");
            ExecuteEvents.Execute(b.gameObject,p,ExecuteEvents.pointerClickHandler);
            if(g.Page!=(i==0?BokumyakuGame.GamePage.Home:i==1?BokumyakuGame.GamePage.Research:BokumyakuGame.GamePage.Records))throw new Exception("Navigation");
            Canvas.ForceUpdateCanvases();
            foreach(var t in menu.GetComponentsInChildren<TextMeshProUGUI>()){t.ForceMeshUpdate();if(t.isTextOverflowing)throw new Exception("Overflow "+t.name);}
        }
        var path=Path.GetFullPath("../docs/evidence/viewport-menu-"+Screen.width);
        File.WriteAllText(path+".json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=true,width=Screen.width,height=Screen.height,raycastTabs=3,note="Editor isolated Home/Research/Records callbacks and text; no purchase/device claim."}));ScreenCapture.CaptureScreenshot(path+".png");return "3 menu tabs passed";
    }
}
