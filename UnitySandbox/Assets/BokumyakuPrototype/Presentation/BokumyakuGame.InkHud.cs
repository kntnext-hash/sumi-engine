using UnityEngine;
namespace Bokumyaku.Presentation
{
 public sealed partial class BokumyakuGame
 {
  GUIStyle hudStyle;
  readonly Color hudGold=new Color(.78f,.62f,.32f), hudWhite=new Color(.96f,.93f,.84f), hudBlack=new Color(.035f,.032f,.026f,.97f);
  void HudText(Rect r,string value,int size,Color color)
  {
   if(hudStyle==null)hudStyle=new GUIStyle(body);
   hudStyle.fontSize=size;hudStyle.alignment=TextAnchor.MiddleCenter;hudStyle.wordWrap=false;
   SetLabelColor(hudStyle,color);GUI.Label(r,value,hudStyle);
  }
  void InkPanel(Rect r)
  {
   Fill(r,hudBlack);Fill(new Rect(r.x,r.y,r.width,1),hudGold);
   Fill(new Rect(r.x,r.yMax-1,r.width,1),hudGold);DrawArt(r,paperArt,.04f,ScaleMode.ScaleAndCrop);
  }
  bool InkControl(Rect r,string value,bool selected=false)
  {
   InkPanel(r);HudText(r,value,17,selected?hudGold:hudWhite);
   bool hit=GUI.Button(r,GUIContent.none,GUIStyle.none);if(hit)AudioClick();return hit;
  }
  void DrawBattleHud(float height)
  {
   InkPanel(new Rect(0,0,440,65));
   Dot(new Vector2(48,48),44,hudBlack);InkStamp(coreInk,new Vector2(48,48),106,hudWhite);
   HudText(new Rect(13,10,70,21),"WAVE",15,hudWhite);
   HudText(new Rect(8,30,80,42),Run.Wave.ToString(),38,new Color(.75f,.10f,.06f));
   HudText(new Rect(15,72,66,16),"墨 脈",11,hudGold);
   HudText(new Rect(102,10,145,28),"墨 "+Run.RunInk,22,hudWhite);
   HudText(new Rect(250,10,122,28),"結晶 "+Meta.Crystals,17,hudGold);
   HudText(new Rect(102,38,270,20),WeaponNames[(int)Run.Weapon]+" · "+FormatTime(Run.ElapsedSeconds),12,hudWhite);
   if(InkControl(new Rect(385,9,47,46),IsPaused?"▶":"Ⅱ"))SetPaused(!IsPaused);
   float y=height-198;
   InkPanel(new Rect(0,y,440,198));
   InkStamp(coreInk,new Vector2(43,y+34),85,hudGold);
   HudText(new Rect(15,y+8,56,19),"墨核",12,hudWhite);
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
    Fill(card,new Color(.9f,.86f,.76f));DrawArt(card,paperArt,.9f,ScaleMode.ScaleAndCrop);
    Fill(new Rect(card.x,card.y,82,2),hudGold);
    Vector2 center=new Vector2(card.center.x,card.y+30);
    Dot(center,27,hudBlack);InkStamp(coreInk,center,65,available?hudGold:hudWhite);
    var symbol=i==0?slashInk:i==1?burstInk:i==2?coreInk:i==3?enemyInk[3]:slashInk;
    InkStamp(symbol,center,43,hudWhite);
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
