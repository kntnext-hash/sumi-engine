using System;using System.IO;using System.Linq;using System.Reflection;using UnityEngine;using Bokumyaku.Presentation;using Bokumyaku.Persistence;using Bokumyaku.Simulation;
public static class TierEnemyVisualCheck
{
 public static object Setup()
 {
  var g=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();if(g==null||!g.SavePath.Contains("ink-qa-"))throw new Exception("Isolated QA required");
  string path=Path.Combine(Path.GetDirectoryName(g.SavePath),"enemy-v10.json");g.SavePathOverride=path;
  var c=new CampaignProgress{BestWave=100,ClaimedMilestoneWave=100,BestWaveByTier=new[]{100,0,0},ClaimedMilestoneWaveByTier=new[]{100,0,0},SelectedTier=2};
  CampaignSaveStore.Save(g.PreviousV9CampaignPath,c);
  File.WriteAllText(g.PreviousV9CampaignPath+".audio.json","{\"Master\":0.25,\"Music\":0.5,\"Effects\":0.75,\"Muted\":true}");
  File.WriteAllText(g.PreviousV8CampaignPath+".audio.json","{\"Master\":1,\"Muted\":false}");
  if(!g.TryReloadProgress())throw new Exception("Migration failed");g.ReloadAudioSettings();if(!g.AudioSettings.Muted||g.AudioSettings.Master!=.25f)throw new Exception("v9 audio priority failed");
  if(!g.TryStartRun(42))throw new Exception("Start failed");g.SetPaused(true);
  var spawn=typeof(RunSimulator).GetMethod("Spawn",BindingFlags.NonPublic|BindingFlags.Instance);
  spawn.Invoke(g.Run,new object[]{(EnemyKind?)EnemyKind.Leech});spawn.Invoke(g.Run,new object[]{(EnemyKind?)EnemyKind.ShieldHalo});
  var leech=g.Run.Enemies.Single(e=>e.Kind==EnemyKind.Leech);var shield=g.Run.Enemies.Single(e=>e.Kind==EnemyKind.ShieldHalo);
  typeof(EnemyState).GetProperty("X").SetValue(leech,36d);typeof(EnemyState).GetProperty("Y").SetValue(leech,0d);
  typeof(EnemyState).GetProperty("X").SetValue(shield,-150d);typeof(EnemyState).GetProperty("Y").SetValue(shield,140d);
  foreach(var b in g.Run.Branches)typeof(BranchState).GetField("NextAttack",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(b,1000);
  return new{rules=g.Run.RulesVersion,attached=g.Run.IsLeechAttached(leech),shield=g.Run.IsShieldActive(shield)};
 }
 public static object OpenShield()
 {
  var g=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();var shield=g.Run.Enemies.Single(e=>e.Kind==EnemyKind.ShieldHalo);
  while(g.Run.IsShieldActive(shield))g.Run.Tick();return new{phase=g.Run.EnemyAbilityPhase(shield),shield=g.Run.IsShieldActive(shield)};
 }
 public static object Capture()
 {
  var g=UnityEngine.Object.FindFirstObjectByType<BokumyakuGame>();var shield=g.Run.Enemies.Single(e=>e.Kind==EnemyKind.ShieldHalo);bool active=g.Run.IsShieldActive(shield);
  var sprites=UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(r=>r.enabled&&r.sortingOrder==3).ToArray();if(sprites.Length<2)throw new Exception("Ability sprites missing");
  string name="../docs/evidence/tier-enemies-"+(active?"guarded":"open");
  var report=new{rules=g.Run.RulesVersion,tier=g.Run.Tier,shieldActive=active,abilitySprites=sprites.Length,width=Screen.width,height=Screen.height,v9AudioPriority=true,method="Forced two-enemy fixture in isolated Editor Play; normal spawn and combat verified separately in EditMode"};
  File.WriteAllText(Path.GetFullPath(name+".json"),Newtonsoft.Json.JsonConvert.SerializeObject(report));ScreenCapture.CaptureScreenshot(Path.GetFullPath(name+".png"));return report;
 }
}
