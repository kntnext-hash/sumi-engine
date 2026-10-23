using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Bokumyaku.Presentation;
public static class BokumyakuUguiCheck
{
 public static object Run()
 {
  var g=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();if(g==null||!g.SavePath.Contains("ink-qa-"))throw new Exception("Isolated only");
  var rows=new List<object>();Canvas.ForceUpdateCanvases();
  int oldSpeed=g.Speed;Click("Speed");rows.Add(new{name="speed raycast",passed=g.Speed!=oldSpeed});
  bool pause=g.IsPaused;Click("Pause");rows.Add(new{name="pause raycast",passed=g.IsPaused!=pause});
  bool auto=g.AutoRepeatEnabled;Click("Auto");rows.Add(new{name="auto raycast",passed=g.AutoRepeatEnabled!=auto});
  bool motion=g.ReducedMotion;Click("ReducedMotion");rows.Add(new{name="reduced motion raycast",passed=g.ReducedMotion!=motion});g.SetReducedMotion(motion);
  Click("Save");rows.Add(new{name="save raycast",passed=File.Exists(g.SavePath)&&!g.BattleStorageBlocked});
  var viewport=BokumyakuBattleHud.GetBattleViewport();rows.Add(new{name="portrait field >=60percent",passed=viewport.height>=.6f,width=viewport.width,height=viewport.height});
  rows.Add(new{name="one EventSystem",passed=UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length==1});
  g.SetPaused(true);g.SetAutoRepeat(false);Click("Home");rows.Add(new{name="home raycast",passed=g.Page==BokumyakuGame.GamePage.Home});g.SetPage(BokumyakuGame.GamePage.Battle);
  File.WriteAllText(Path.GetFullPath("../docs/evidence/render-ugui-controls.json"),Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));return rows;
 }
 static void Click(string name)
 {
  UnityEngine.UI.Button button=null;foreach(var b in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))if(b.name==name){button=b;break;}
  if(button==null)throw new Exception("Missing button "+name);
  var rect=(RectTransform)button.transform;var p=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
  var data=new PointerEventData(EventSystem.current){position=p,button=PointerEventData.InputButton.Left};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
  if(hits.Count==0||hits[0].gameObject.GetComponentInParent<UnityEngine.UI.Button>()!=button)throw new Exception("Button blocked "+name);
  ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerClickHandler);
 }
}

