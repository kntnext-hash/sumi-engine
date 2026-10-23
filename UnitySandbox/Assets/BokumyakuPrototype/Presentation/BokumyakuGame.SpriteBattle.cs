using Bokumyaku.Simulation;
using Bokumyaku.Presentation.Drawing;
using ConnectPiece.SumiEngine.Core;
using UnityEngine;
namespace Bokumyaku.Presentation
{
 public sealed partial class BokumyakuGame
 {
  DirtyInkSurface persistentInk;InkBattleWorld battleWorld;BokumyakuBattleHud battleHud;
  int previousFrameRate; bool ownsFrameRate;
  public bool ReducedMotion {get;private set;}
  public string BattleNotice=>string.IsNullOrEmpty(saveWarning)?notice:"保存待ち: "+saveWarning;
  public bool BattleStorageBlocked=>loadBlocked||storageFailed;
  public long DirtyInkPixels=>persistentInk?.UpdatedPixels??0;
  public int DirtyInkUploads=>persistentInk?.DirtyUploads??0;
  public int InkFullInitializations=>persistentInk?.FullInitializations??0;
  public int SpriteDeaths=>battleWorld?.Deaths??0;
  public int SpriteHits=>battleWorld?.HitFlashes??0;
  public int SpriteInterpolatedFrames=>battleWorld?.InterpolatedFrames??0;
  public void SetReducedMotion(bool value){ReducedMotion=value;PlayerPrefs.SetInt("Bokumyaku.ReducedMotion",value?1:0);PlayerPrefs.Save();}
  void EnsureBattlePresentation()
  {
   if(battleWorld!=null)return;EnsureInkMotifs();
   persistentInk=new DirtyInkSurface(canvas.width,canvas.height,2026);
   ReducedMotion=PlayerPrefs.GetInt("Bokumyaku.ReducedMotion",0)!=0;
   battleWorld=new InkBattleWorld(transform,enemyInk,slashInk,burstInk,hudRing,lightHalo,paperArt,portraitLandscapeArt??landscapeArt);
   battleWorld.DeathMark=DrawDeathMark;
   var hudObject=new GameObject("Battle HUD",typeof(RectTransform));hudObject.transform.SetParent(transform,false);battleHud=hudObject.AddComponent<BokumyakuBattleHud>();battleHud.Initialize(this,hudRing,upgradeGlyphs,paperArt);
   var driver=gameObject.AddComponent<BokumyakuBattlePresenter>();driver.Owner=this;
   previousFrameRate=Application.targetFrameRate;Application.targetFrameRate=60;ownsFrameRate=true;
  }
  public void PresentBattle()
  {
   if(battleWorld==null||Run==null)return;
   DrawNewBranches();persistentInk.Flush();
   battleWorld.Render(Run,persistentInk.Texture,tickClock*RunSimulator.TickRate,IsPaused,ReducedMotion,Page==GamePage.Battle,BokumyakuBattleHud.GetBattleViewport());
  }
  void DrawDeathMark(EnemyState enemy)
  {
   float sx=persistentInk.Width/384f,sy=persistentInk.Height/512f;
   var brush=InkBrushSettings.Splatter;brush.radius=enemy.Kind==EnemyKind.Boss?13:7;brush.pigment=.65f;brush.water=.2f;
   var point=new InkPoint((float)enemy.ViewX*sx,(float)enemy.ViewY*sy,0,.8f);
   persistentInk.Begin(point,brush,unchecked(Run.Seed+enemy.Id*113));persistentInk.Move(new InkPoint(point.x+1,point.y+1,.1f,.3f));persistentInk.End();
  }
  void ReleaseBattlePresentation(){if(ownsFrameRate){Application.targetFrameRate=previousFrameRate;ownsFrameRate=false;}battleWorld?.Dispose();battleWorld=null;persistentInk?.Dispose();persistentInk=null;if(battleHud!=null)Destroy(battleHud.gameObject);}
 }
 public sealed class BokumyakuBattlePresenter:MonoBehaviour
 {
  public BokumyakuGame Owner;void LateUpdate(){if(Owner!=null)Owner.PresentBattle();}
 }
}


