using UnityEngine;
namespace Bokumyaku.Presentation
{
 public sealed partial class BokumyakuGame
 {
  readonly System.Collections.Generic.Dictionary<string,GUIStyle> hudStyles=new System.Collections.Generic.Dictionary<string,GUIStyle>();
  readonly Color hudGold=new Color(.78f,.62f,.32f), hudWhite=new Color(.96f,.93f,.84f), hudBlack=new Color(.035f,.032f,.026f,.97f);
  void HudText(Rect r,string value,int size,Color color)
  {
   string key=size+":"+ColorUtility.ToHtmlStringRGBA(color);
   if(!hudStyles.TryGetValue(key,out var style))
   {
    style=new GUIStyle(body) { padding=new RectOffset(), margin=new RectOffset(), fixedHeight=0, fixedWidth=0, clipping=TextClipping.Overflow, fontSize=size, alignment=TextAnchor.MiddleCenter, wordWrap=false };
    SetLabelColor(style,color);hudStyles.Add(key,style);
   }
   GUI.Label(r,value,style);
  }
  void InkPanel(Rect r)
  {
   Fill(r,hudBlack);Fill(new Rect(r.x,r.y,r.width,1),hudGold);
   Fill(new Rect(r.x,r.yMax-1,r.width,1),hudGold);DrawArt(r,paperArt,.04f,ScaleMode.ScaleAndCrop);
  }
  // Deterministic dry edges: no random state or per-frame texture allocations.
  void DryEdge(Rect r,Color color,int seed)
  {
   int count=Mathf.CeilToInt(r.width/5);
   for(int i=0;i<count;i++)
   {
    float x=r.x+r.width*i/count;
    float size=5+(i*13+seed)%8;
    InkStamp(burstInk,new Vector2(x,r.y+1),size,color);
    InkStamp(burstInk,new Vector2(x,r.yMax-1),size,color);
   }
  }
  bool InkControl(Rect r,string value,bool selected=false)
  {
   Dot(r.center,22,hudBlack);
   if(selected)InkStamp(lightHalo,r.center,65,new Color(1,.65f,.15f,.45f));
   InkStamp(hudRing,r.center,53,selected?hudGold:new Color(.63f,.58f,.46f));
   HudText(r,value,16,selected?hudGold:hudWhite);
   bool hit=GUI.Button(r,GUIContent.none,GUIStyle.none);if(hit)AudioClick();return hit;
  }
  void DrawBattleHud(float height)
  {
   InkPanel(new Rect(0,0,440,65));
   DryEdge(new Rect(0,0,440,65),hudBlack,3);
   Dot(new Vector2(48,48),44,hudBlack);InkStamp(hudRing,new Vector2(48,48),106,hudWhite);
   Fill(new Rect(20,15,56,20),hudBlack);
   HudText(new Rect(20,15,56,20),"WAVE",14,hudWhite);
   HudText(new Rect(8,30,80,42),Run.Wave.ToString(),38,new Color(.75f,.10f,.06f));
   Fill(new Rect(24,68,48,18),hudBlack);
   HudText(new Rect(24,68,48,18),"墨 脈",11,hudGold);
   HudText(new Rect(102,10,145,28),"墨 "+Run.RunInk,22,hudWhite);
   HudText(new Rect(250,10,122,28),"結晶 "+Meta.Crystals,17,hudGold);
   HudText(new Rect(102,38,270,20),WeaponNames[(int)Run.Weapon]+" · "+FormatTime(Run.ElapsedSeconds),12,hudWhite);
   if(InkControl(new Rect(385,9,47,46),IsPaused?"▶":"Ⅱ"))SetPaused(!IsPaused);
   float y=height-198;
   InkPanel(new Rect(0,y,440,198));
   DryEdge(new Rect(0,y,440,198),hudBlack,9);
   InkStamp(lightHalo,new Vector2(43,y+34),100,new Color(1,.6f,.12f,.35f));
   Dot(new Vector2(43,y+34),36,hudBlack);
   InkStamp(hudRing,new Vector2(43,y+34),85,hudGold);
   Fill(new Rect(24,y+8,38,19),hudBlack);
   HudText(new Rect(24,y+8,38,19),"墨核",12,hudWhite);
   HudText(new Rect(10,y+27,66,28),Run.CoreHp+"/10",23,hudWhite);
   Fill(new Rect(88,y+20,112,5),new Color(.23f,.20f,.15f));
   Fill(new Rect(88,y+20,112*Mathf.Clamp01(Run.CoreHp/10f),5),hudGold);
   HudText(new Rect(88,y+32,112,20),"守り、育てる。",12,hudGold);
   if(InkControl(new Rect(212,y+10,62,44),"×"+Speed))SetSpeed(Speed==1?2:1);
   if(InkControl(new Rect(280,y+10,70,44),"自動",AutoRepeatEnabled))SetAutoRepeat(!AutoRepeatEnabled);
   if(InkControl(new Rect(356,y+10,76,44),portraitDetails?"閉じる":"詳細",portraitDetails))portraitDetails=!portraitDetails;
   for(int i=0;i<5;i++)
   {
    var kind=(Bokumyaku.Simulation.UpgradeKind)i;
    int level=Run.UpgradeLevels[i],cap=Run.UpgradeMaxLevel(kind),cost=Run.UpgradeCost(kind);
    bool available=!loadBlocked&&!storageFailed&&!AwaitingStart&&Run.State==Bokumyaku.Simulation.RunStatus.Running&&level<cap&&Run.RunInk>=cost;
    Rect card=new Rect(7+i*86,y+72,82,103);
    Fill(new Rect(card.x+3,card.y+4,card.width,card.height),Color.black);
    Fill(card,new Color(.83f,.78f,.65f));DrawArt(card,paperArt,.9f,ScaleMode.ScaleAndCrop);
    Fill(new Rect(card.x+3,card.y+3,76,1),new Color(.98f,.91f,.73f,.7f));
    DryEdge(card,new Color(.09f,.07f,.04f,.9f),i*7);
    Fill(new Rect(card.x,card.y+3,2,97),new Color(.24f,.19f,.11f,.6f));
    Fill(new Rect(card.xMax-2,card.y+3,2,97),new Color(.24f,.19f,.11f,.6f));
    Vector2 center=new Vector2(card.center.x,card.y+30);
    Dot(center,27,hudBlack);InkStamp(hudRing,center,65,available?hudGold:hudWhite);
    if(available)InkStamp(lightHalo,center,62,new Color(1,.65f,.12f,.5f));
    InkStamp(upgradeGlyphs[i],center,49,hudWhite);
    HudText(new Rect(card.x,card.y-17,82,16),"Lv."+level,11,hudGold);
    HudText(new Rect(card.x,card.y+58,82,22),UpgradeNames[i],15,ink);
    Fill(new Rect(card.x+5,card.y+82,72,18),hudBlack);
    HudText(new Rect(card.x+5,card.y+81,72,20),level>=cap?"MAX":"墨 "+cost,12,available?hudGold:new Color(.66f,.64f,.59f));
    GUI.enabled=available;if(GUI.Button(card,GUIContent.none,GUIStyle.none))BuyUpgrade(kind);GUI.enabled=true;
   }
   HudText(new Rect(10,height-21,420,18),string.IsNullOrEmpty(saveWarning)?notice:"保存待ち："+saveWarning,10,hudWhite);
  }
 }
}
