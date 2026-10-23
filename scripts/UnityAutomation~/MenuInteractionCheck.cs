using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEditor;using UnityEngine;using UnityEngine.UI;using UnityEngine.EventSystems;using TMPro;using Bokumyaku.Presentation;using Bokumyaku.Simulation;
public static class MenuInteractionCheck {
static BokumyakuGame g;static BokumyakuMenuHud hud;static int frame,step;static double next;static List<string> results=new List<string>();
static void Require(bool ok,string msg){if(!ok)throw new Exception(msg);results.Add(msg);}
public static object Run(){g=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();if(!g.SavePath.Contains("ink-qa-menu-"))throw new Exception("isolated save required");hud=UnityEngine.Object.FindFirstObjectByType<BokumyakuMenuHud>();g.SavePathOverride=Path.GetFullPath("../logs/ink-qa-menu-"+Guid.NewGuid().ToString("N")+"/campaign.json");g.TryReloadProgress();g.SetPage(BokumyakuGame.GamePage.Research);hud.Refresh();g.SetPage(BokumyakuGame.GamePage.Home);hud.Refresh();Application.runInBackground=true;step=0;next=0;frame=Time.frameCount;EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();EditorApplication.update+=Tick;return "running";}
static void Click(string name){Canvas.ForceUpdateCanvases();var b=hud.GetComponentsInChildren<Button>(true).First(x=>x.name==name);Require(b.gameObject.activeInHierarchy&&b.interactable,name+" enabled");var scroll=b.GetComponentInParent<ScrollRect>();if(scroll!=null){scroll.StopMovement();var local=scroll.content.InverseTransformPoint(b.transform.position);float height=scroll.content.rect.height-scroll.viewport.rect.height;scroll.verticalNormalizedPosition=height<=0?1:1-Mathf.Clamp01((-local.y-scroll.viewport.rect.height*.5f)/height);Canvas.ForceUpdateCanvases();}var r=(RectTransform)b.transform;var pos=RectTransformUtility.WorldToScreenPoint(null,r.position);var eventData=new PointerEventData(EventSystem.current){position=pos};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(eventData,hits);Require(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==b,name+" raycast");ExecuteEvents.Execute(b.gameObject,eventData,ExecuteEvents.pointerClickHandler);}
static void Capture(string page){Canvas.ForceUpdateCanvases();foreach(var t in hud.GetComponentsInChildren<TextMeshProUGUI>()){t.ForceMeshUpdate();Require(t.font==Resources.Load<TMP_FontAsset>("BokumyakuFonts/BattleJapanese"),"font "+t.name);Require(t.font.HasCharacters(t.text),"glyphs "+t.text);Require(!t.isTextOverflowing,"fits "+t.text);}ScreenCapture.CaptureScreenshot(Path.GetFullPath("../docs/evidence/menu-"+page+"-1080x2400.png"));}
static void Tick(){EditorApplication.QueuePlayerLoopUpdate();if(Time.frameCount==frame||EditorApplication.timeSinceStartup<next)return;frame=Time.frameCount;next=EditorApplication.timeSinceStartup+.4;try{switch(step++){
case 0:Require(Screen.width==1080&&Screen.height==2400,"1080x2400");Capture("home");break;
case 1:Click("MenuWeapon1");Require(g.SelectedWeapon==WeaponKind.Pierce,"weapon selection");Click("MenuBuild1");Click("MenuPolicy1");Click("MenuSlotSave0");Click("MenuWeapon0");Click("MenuSlotLoad0");Require(g.SelectedWeapon==WeaponKind.Pierce,"slot restored");break;
case 2:var old=g.AudioSettings.Muted;Click("MenuAudio0");Require(g.AudioSettings.Muted!=old,"mute changed");Click("MenuAudio0");Click("MenuSave");Click("MenuTab1");break;
case 3:g.Meta.Crystals=100000;hud.Refresh();int before=g.Meta.Damage;Click("MenuResearch0");Require(g.Meta.Damage==before+1,"research purchase");break;
case 4:Capture("research");break;

case 5:Click("MenuTab0");break;

case 6:Click("MenuStart");Require(g.Page==BokumyakuGame.GamePage.Battle&&g.ResumeAvailable,"start run");g.SetPaused(true);g.SetPage(BokumyakuGame.GamePage.Home);hud.Refresh();break;
case 7:Click("MenuAbandon");break;
case 8:Click("CancelAbandon");Require(g.ResumeAvailable,"cancel preserved run");break;
case 9:Click("MenuAbandon");break;
case 10:Click("ConfirmAbandon");Require(!g.ResumeAvailable,"abandon confirmed");break;
case 11:Click("MenuStart");g.SetPaused(true);for(int i=0;i<30000&&g.Run.State==RunStatus.Running;i++)g.Run.Tick();break;
case 12:Require(g.Run.State!=RunStatus.Running,"QA run ended");g.SetPage(BokumyakuGame.GamePage.Records);hud.Refresh();break;
case 13:Capture("records");Require(g.Campaign.History.Count>0,"history populated");break;
case 14:Click("MenuTab0");break;
case 15:Click("MenuSave");Require(g.TryReloadProgress(),"save reload");Finish(true,null);break;}}catch(Exception e){Finish(false,e.ToString());}}
static void Finish(bool pass,string error){EditorApplication.update-=Tick;File.WriteAllText(Path.GetFullPath("../docs/evidence/menu-migration-check.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new{pass,error,step,width=Screen.width,height=Screen.height,results},Newtonsoft.Json.Formatting.Indented));}
}
