[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$files=Get-ChildItem (Join-Path $root 'UnitySandbox/Assets/BokumyakuPrototype/Simulation/*.cs')|Sort-Object Name
$source="using System;`nusing System.Collections.Generic;`nusing System.IO;`nusing System.Reflection;`n"+(($files|ForEach-Object{[IO.File]::ReadAllText($_.FullName)-replace '(?m)^using [^;]+;\s*',''})-join "`n")
$runner=@'
namespace Bokumyaku.MeasurementV5 {
 public sealed class Row {
  public string suite,build,weapon,policy,strategy,status;
  public int seed,ticks,wave,completedWave,kills,leaks,tips,income,crystals;
  public double seconds,incomePerMinute,crystalsPerMinute,rewardPoints;
  public int[] spawned,killed,leaked,levels;
 }
 public static class Systems {
  static string Fingerprint(object value) {
   if(value==null)return "null"; if(value is string)return (string)value;
   if(value is Array){var s=new System.Text.StringBuilder("[");foreach(var item in (Array)value)s.Append(Fingerprint(item)).Append(';');return s.Append(']').ToString();}
   var type=value.GetType();if(type.IsPrimitive||type.IsEnum)return Convert.ToString(value,System.Globalization.CultureInfo.InvariantCulture);
   var result=new System.Text.StringBuilder();foreach(var f in type.GetFields())result.Append(f.Name).Append('=').Append(Fingerprint(f.GetValue(value))).Append('|');return result.ToString();
  }
  static void Purchase(Simulation.RunSimulator r,int strategy) {
   if(r.TickCount%30!=0||r.State!=Simulation.RunStatus.Running)return;
   Simulation.UpgradeKind[][] cycles={
    new[]{Simulation.UpgradeKind.Speed,Simulation.UpgradeKind.Branch,Simulation.UpgradeKind.Width,Simulation.UpgradeKind.Supply,Simulation.UpgradeKind.Damage,Simulation.UpgradeKind.Speed},
    new[]{Simulation.UpgradeKind.Damage,Simulation.UpgradeKind.Supply,Simulation.UpgradeKind.Damage,Simulation.UpgradeKind.Speed,Simulation.UpgradeKind.Width,Simulation.UpgradeKind.Branch},
    new[]{Simulation.UpgradeKind.Width,Simulation.UpgradeKind.Bleed,Simulation.UpgradeKind.Supply,Simulation.UpgradeKind.Branch,Simulation.UpgradeKind.Damage,Simulation.UpgradeKind.Speed},
    new[]{Simulation.UpgradeKind.Damage,Simulation.UpgradeKind.Speed,Simulation.UpgradeKind.Supply,Simulation.UpgradeKind.Branch,Simulation.UpgradeKind.Width,Simulation.UpgradeKind.Bleed}};
   int[] caps={30,25,25,9,10,10},levels=r.UpgradeLevels;int sum=0;foreach(int x in levels)sum+=x;
   for(int offset=0;offset<6;offset++){var kind=cycles[strategy][(sum+offset)%6];if(levels[(int)kind]>=caps[(int)kind])continue;r.TryBuy(kind);break;}
  }
  static Simulation.RunSimulator Execute(int seed,Simulation.BuildPreset build,Simulation.WeaponKind weapon,Simulation.GrowthPolicy policy,int strategy,int metaLevel) {
   var r=new Simulation.RunSimulator(seed,new Simulation.MetaProgress{Damage=metaLevel,Supply=metaLevel,Growth=metaLevel,Harvest=metaLevel});r.SetBuild(build);r.SetWeapon(weapon);r.SetPolicy(policy);
   while(r.State==Simulation.RunStatus.Running){r.Tick();if(strategy>=0)Purchase(r,strategy);
    if(r.TickCount>Simulation.RunSimulator.MaxTicks||r.RunInk<0||r.Enemies.Count>96||r.TipCount>12||r.Branches.Count>64||r.Fields.Count>96)throw new Exception("Invariant failed");}
   Simulation.RunSimulator.Restore(r.CaptureState());return r;
  }
  public static Row Run(string suite,int seed,Simulation.BuildPreset build,Simulation.WeaponKind weapon,Simulation.GrowthPolicy policy,int strategy,int metaLevel) {
   var a=Execute(seed,build,weapon,policy,strategy,metaLevel);var b=Execute(seed,build,weapon,policy,strategy,metaLevel);
   if(Fingerprint(a.CaptureState())!=Fingerprint(b.CaptureState()))throw new Exception("Full snapshot determinism failed");
   return new Row{suite=suite,seed=seed,build=build.ToString(),weapon=weapon.ToString(),policy=policy.ToString(),strategy=strategy<0?build.ToString():new[]{"Swift","Power","Bleed","Balanced"}[strategy],status=a.State.ToString(),ticks=a.TickCount,seconds=a.ElapsedSeconds,wave=a.Wave,completedWave=a.CompletedWave,kills=a.Kills,leaks=a.Leaks,tips=a.TipCount,income=a.IncomeEarned,crystals=a.CrystalsReward,incomePerMinute=a.IncomePerMinute,crystalsPerMinute=a.CrystalsReward*60/(a.ElapsedSeconds+3),rewardPoints=a.RewardPoints,spawned=a.SpawnedByType,killed=a.KilledByType,leaked=a.LeakedByType,levels=a.UpgradeLevels};
  }
 }
}
'@
Add-Type -TypeDefinition ($source+"`n"+$runner)
$rows=@()
foreach($seed in 1..10){
 foreach($build in [Enum]::GetValues([Bokumyaku.Simulation.BuildPreset])){foreach($weapon in [Enum]::GetValues([Bokumyaku.Simulation.WeaponKind])){
 $rows += [Bokumyaku.MeasurementV5.Systems]::Run('weapons180',$seed,$build,$weapon,[Bokumyaku.Simulation.GrowthPolicy]::Balanced,-1,0)
 }}
 foreach($policy in [Enum]::GetValues([Bokumyaku.Simulation.GrowthPolicy])){foreach($strategy in 0..3){
 $rows += [Bokumyaku.MeasurementV5.Systems]::Run('strategies120',$seed,[Bokumyaku.Simulation.BuildPreset]::Manual,[Bokumyaku.Simulation.WeaponKind]::Point,$policy,$strategy,0)
 }}
 $rows += [Bokumyaku.MeasurementV5.Systems]::Run('meta5_10',$seed,[Bokumyaku.Simulation.BuildPreset]::Farming,[Bokumyaku.Simulation.WeaponKind]::Point,[Bokumyaku.Simulation.GrowthPolicy]::Balanced,-1,5)
}
function Stats($values){$s=@($values|Sort-Object);$n=$s.Count;[ordered]@{min=$s[0];median=($s[[int][Math]::Floor(($n-1)/2)]+$s[[int][Math]::Floor($n/2)])/2;max=$s[-1];mean=($s|Measure-Object -Average).Average}}
$summaries=@(foreach($g in ($rows|Group-Object suite,build,weapon,policy,strategy)){[ordered]@{condition=$g.Name;count=$g.Count;seconds=Stats $g.Group.seconds;wave=Stats $g.Group.wave;incomePerMinute=Stats $g.Group.incomePerMinute;crystalsPerMinute=Stats $g.Group.crystalsPerMinute;tips=Stats $g.Group.tips;defeated=@($g.Group|Where-Object status -eq 'Defeated').Count}})
$standard=@($rows|Where-Object{$_.suite -eq 'weapons180' -and $_.build -eq 'Farming' -and $_.weapon -eq 'Point'})
$meta=@($rows|Where-Object suite -eq 'meta5_10')
$rewardCandidates=@(foreach($coefficient in @(.05,.08,.10)){$all=@($rows|Where-Object suite -eq 'weapons180');$rates=@($all|ForEach-Object{[Math]::Floor($_.rewardPoints*$coefficient)*60/($_.seconds+3)});[ordered]@{coefficient=$coefficient;formula='floor(weightedEligibleKillPoints * coefficient), no fixed reward or PB bonus';meanRate=($rates|Measure-Object -Average).Average;zeroRewardConditions=@($all|Where-Object{[Math]::Floor($_.rewardPoints*$coefficient) -eq 0}).Count}})
$report=[ordered]@{classification='Independent C# standalone execution; not Unity visual or device evidence';generatedAtUtc=[DateTime]::UtcNow.ToString('o');sourceHashes=@($files|ForEach-Object{[ordered]@{name=$_.Name;sha256=(Get-FileHash $_.FullName).Hash}});conditions=310;exactDeterministicReplays=310;independentSeeds=10;metaComparison=[ordered]@{baselineMeanWave=($standard.wave|Measure-Object -Average).Average;meta5MeanWave=($meta.wave|Measure-Object -Average).Average};standardFirstRun=[ordered]@{definition='Farming/Point/Balanced/meta0';seconds=Stats $standard.seconds;in300to480Seconds=@($standard|Where-Object{$_.seconds -ge 300 -and $_.seconds -le 480}).Count};rewardCandidates=$rewardCandidates;selectedReward='0.08: no flat reward, weighted eligible kills only; ~12.5 normal kills per crystal. PB milestone bonus excluded and handled separately by persistence.';strategyRules=@('Every 30 ticks after combat, one purchase attempt; wait for selected item, skip capped levels; six-item cycle indexed by sum of levels. Same costs/rewards for all strategies.','Swift: Speed,Branch,Width,Supply,Damage,Speed','Power: Damage,Supply,Damage,Speed,Width,Branch','Bleed: Width,Bleed,Supply,Branch,Damage,Speed','Balanced: Damage,Speed,Supply,Branch,Width,Bleed');weaponRules=@('Sweep: 90-degree cone, nearest then ID, max5, .85 damage,1.2 interval','Enso: annulus20..150+4Width around tip,max6,.8 damage,1.35 interval','Resonance: nearest chain within70+2Width,max4,1.0 initial damage,.75 falloff,1.15 interval');enemyRules=@('Boss: one at start of each tenth wave,12xHP,.55 speed,4 contact','Splitter:20% fromWave15 replaces normal high-roll band,.9HP;2 children .35 parent maxHP,no recursive split,no child rewards/growth;capacity overflow ends run');limitations=@('310 conditions use 10 shared seeds, not 310 independent seeds.','No forced winner, no weapon/enemy-specific reward bonuses beyond GDD type weights.','Full 12-sector policy scoring remains the earlier simplified heuristic.','Regular crystals/min includes 3sec retry wait and excludes first-time milestone bonuses.');summaries=$summaries;runs=$rows}
$out=Join-Path $root 'docs/evidence/bokumyaku-systems-v5.json';$report|ConvertTo-Json -Depth 12|Set-Content $out -Encoding utf8
$report.standardFirstRun|ConvertTo-Json -Depth 4
$report.metaComparison|ConvertTo-Json
$summaries|Where-Object{$_.condition -like 'weapons180*'}|ForEach-Object{[pscustomobject]@{condition=$_.condition;medianSeconds=$_.seconds.median;medianWave=$_.wave.median;income=$_.incomePerMinute.median;crystals=$_.crystalsPerMinute.median}}|Format-Table
"310 conditions + 310 exact replays PASS; $out"
