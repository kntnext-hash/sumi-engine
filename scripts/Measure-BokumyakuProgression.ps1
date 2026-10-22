[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$files=@(Get-ChildItem (Join-Path $root 'UnitySandbox/Assets/BokumyakuPrototype/Simulation') -Filter '*.cs'|Sort-Object Name)
# This historical diagnostic is pinned to baseline 397b666 (rules2).
# Refuse changed inputs before compiling or writing evidence; run from that checkout to reproduce.
$baselineHashes=@{
 'AutoRepeatController.cs'='D110E501CA532370504A4F5E1C09842769F0DE9CC4EA51DDAF1E640FE3B67ACE'
 'RunSimulator.cs'='F5F2A2ED719758A339D2F27033FE8AC3ADEB7CA4F6B48535E6A170F8A88105E1'
 'RunSimulator.ExactSnapshot.cs'='1D3DEC084E8811727E8DC333E7FB0937A3A06AC1CF4711F0CACA85D312CB4969'
 'RunSimulator.Snapshot.cs'='7A4BC736736B9F2DBE9BB9F0545517F04A2B725DDB87AE1D4FD770F13E0DB6FE'
 'RunSnapshot.cs'='21F9068A22BBDBCF877341E5B6459857F12164905971FA211F610AB429501FCB'
}
if($files.Count -ne $baselineHashes.Count){throw 'Baseline 397b666 source set differs; historical evidence was not changed.'}
foreach($file in $files){
 if(!$baselineHashes.ContainsKey($file.Name) -or (Get-FileHash $file.FullName -Algorithm SHA256).Hash -ne $baselineHashes[$file.Name]){
  throw "Baseline 397b666 input mismatch: $($file.Name). Use the original rules2 checkout; historical evidence was not changed."
 }
}
$source="using System;`nusing System.Collections.Generic;`nusing System.IO;`nusing System.Reflection;`n"+(($files|ForEach-Object{[IO.File]::ReadAllText($_.FullName)-replace '(?m)^using [^;]+;\s*',''})-join "`n")
$runner=@'
namespace Bokumyaku.ProgressionMeasurement {
 public sealed class Purchase { public string kind; public int cost,level; }
 public sealed class Row {
  public string build,policy,status; public int round,seed,rulesVersion,ticks,completedWave,normalCrystals,firstMilestoneCrystals,bestWave;
  public double seconds; public Simulation.MetaProgress before,after; public Purchase[] purchases;
 }
 public static class Runner {
  static Simulation.MetaProgress Copy(Simulation.MetaProgress m) { return new Simulation.MetaProgress {Crystals=m.Crystals,Damage=m.Damage,Supply=m.Supply,Growth=m.Growth,Harvest=m.Harvest}; }
  public static Row Compare(Simulation.MetaProgress shared,Simulation.BuildPreset build,int seed) {
   var meta=Copy(shared);var run=new Simulation.RunSimulator(seed,meta);run.SetBuild(build);run.SetWeapon(Simulation.WeaponKind.Point);run.SetPolicy(Simulation.GrowthPolicy.Balanced);
   while(run.State==Simulation.RunStatus.Running){run.Tick();if(run.TickCount>Simulation.RunSimulator.MaxTicks)throw new Exception("Comparison bound");}
   if(run.State==Simulation.RunStatus.CapacityExceeded)throw new Exception("Comparison capacity");
   return new Row{build=build.ToString(),policy="SharedMetaAfter19FarmingRounds",seed=seed,rulesVersion=run.RulesVersion,ticks=run.TickCount,seconds=run.ElapsedSeconds,completedWave=run.CompletedWave,normalCrystals=run.CrystalsReward,before=Copy(shared),after=meta,purchases=new Purchase[0],status=run.State.ToString()};
  }
  public static Row[] Execute(Simulation.BuildPreset build,bool buy) {
   var rows=new List<Row>(); var meta=new Simulation.MetaProgress(); int best=0,claimed=0;
   for(int seed=1;seed<=20;seed++) {
    var before=Copy(meta); var run=new Simulation.RunSimulator(seed,meta); run.SetBuild(build); run.SetWeapon(Simulation.WeaponKind.Point); run.SetPolicy(Simulation.GrowthPolicy.Balanced);
    while(run.State==Simulation.RunStatus.Running) {run.Tick();if(run.TickCount>Simulation.RunSimulator.MaxTicks||run.RunInk<0)throw new Exception("Run invariant failed");}
    if(run.State==Simulation.RunStatus.CapacityExceeded)throw new Exception("Capacity stop invalidates economy measurement");
    int normal=run.CrystalsReward;
    if(!run.ClaimReward(meta))throw new Exception("Reward claim failed");
    if(run.ClaimReward(meta))throw new Exception("Duplicate reward accepted");
    int milestone=run.CompletedWave/5*5,bonus=Math.Max(0,milestone-claimed); claimed=Math.Max(claimed,milestone);best=Math.Max(best,run.CompletedWave);meta.Crystals=checked(meta.Crystals+bonus);
    var purchases=new List<Purchase>();
    while(buy) {
     Simulation.MetaUpgrade chosen=Simulation.MetaUpgrade.Damage;int cost=int.MaxValue;
     foreach(Simulation.MetaUpgrade k in Enum.GetValues(typeof(Simulation.MetaUpgrade)))if(meta.Level(k)<20 && meta.Cost(k)<cost){chosen=k;cost=meta.Cost(k);}
     if(cost>meta.Crystals)break;
     if(!meta.TryBuy(chosen))throw new Exception("Selected purchase failed");
     purchases.Add(new Purchase{kind=chosen.ToString(),cost=cost,level=meta.Level(chosen)});
     if(purchases.Count>80)throw new Exception("Purchase loop bound exceeded");
    }
    int spent=0;foreach(var p in purchases)spent+=p.cost;
    if(meta.Crystals!=before.Crystals+normal+bonus-spent)throw new Exception("Ledger imbalance");
    rows.Add(new Row{build=build.ToString(),policy=buy?"CheapestThenEnum":"NoMetaPurchases",round=seed,seed=seed,rulesVersion=run.RulesVersion,ticks=run.TickCount,seconds=run.ElapsedSeconds,completedWave=run.CompletedWave,normalCrystals=normal,firstMilestoneCrystals=bonus,bestWave=best,before=before,after=Copy(meta),purchases=purchases.ToArray(),status=run.State.ToString()});
   }
   return rows.ToArray();
  }
 }
}
'@
Add-Type -TypeDefinition ($source+"`n"+$runner)
$rows=@()
foreach($build in @('Farming','Record')) { foreach($buy in @($true,$false)) { $rows+=@([Bokumyaku.ProgressionMeasurement.Runner]::Execute([Bokumyaku.Simulation.BuildPreset]$build,$buy)) } }
$summaries=@($rows|Group-Object build,policy|ForEach-Object {
 $r=@($_.Group);$count=($r|ForEach-Object {$_.purchases.Count}|Measure-Object -Sum).Sum
 [ordered]@{build=$r[0].build;policy=$r[0].policy;runs=$r.Count;totalPurchases=$count;meanPurchasesPerRun=$count/$r.Count;finalMeta=$r[-1].after;bestWave=$r[-1].bestWave;totalNormalCrystals=($r.normalCrystals|Measure-Object -Sum).Sum;totalFirstMilestoneCrystals=($r.firstMilestoneCrystals|Measure-Object -Sum).Sum;totalSeconds=($r.seconds|Measure-Object -Sum).Sum;testCompletedRuns=@($r|Where-Object status -eq 'TestCompleted').Count;anyMetaAtCap=(@($r[-1].after.Damage,$r[-1].after.Supply,$r[-1].after.Growth,$r[-1].after.Harvest)|Where-Object {$_ -eq 20}).Count -gt 0}
})
$sharedMeta=($rows|Where-Object {$_.build -eq 'Farming' -and $_.policy -eq 'CheapestThenEnum' -and $_.round -eq 19}).after
$paired=@()
foreach($seed in 1..10) {
 $f=[Bokumyaku.ProgressionMeasurement.Runner]::Compare($sharedMeta,[Bokumyaku.Simulation.BuildPreset]::Farming,$seed)
 $r=[Bokumyaku.ProgressionMeasurement.Runner]::Compare($sharedMeta,[Bokumyaku.Simulation.BuildPreset]::Record,$seed)
 $paired += [ordered]@{seed=$seed;farming=$f;record=$r;recordCompletedWaveDelta=$r.completedWave-$f.completedWave;farmingNormalCrystalsPerMinute=60*$f.normalCrystals/($f.seconds+3);recordNormalCrystalsPerMinute=60*$r.normalCrystals/($r.seconds+3)}
}
$sharedComparison=[ordered]@{definition='Same meta cloned after round19 Farming cheapest-purchase campaign; independent seed1..10 runs, no post-run purchases or milestone rewards';meta=$sharedMeta;conditions=20;recordDepthWins=@($paired|Where-Object {$_.recordCompletedWaveDelta -gt 0}).Count;depthTies=@($paired|Where-Object {$_.recordCompletedWaveDelta -eq 0}).Count;farmingEfficiencyWins=@($paired|Where-Object {$_.farmingNormalCrystalsPerMinute -gt $_.recordNormalCrystalsPerMinute}).Count;meanRecordCompletedWaveDelta=($paired.recordCompletedWaveDelta|Measure-Object -Average).Average;pairs=$paired}
$result=[ordered]@{classification='Standalone C# simulation measurement; not Unity, persistence transaction, device or product economy acceptance';generatedAtUtc=[DateTime]::UtcNow.ToString('o');conditions=100;rulesVersion=2;weapon='Point';growthPolicy='Balanced';seeds='1..20 sequential rounds for each independent campaign';purchasePolicy='Measurement-only policy, not a product auto-purchase feature. After settlement repeatedly buy cheapest uncapped meta upgrade; equal costs use enum order Damage,Supply,Growth,Harvest';milestonePolicy='5 crystals per first new five completed waves, separate from normal; equivalent arithmetic, not CampaignSaveStore execution';limitations=@('Only Point/Balanced and one fixed purchase policy','Twenty finite rounds, no offline income','TestCompleted at wave60 is finite test completion, not natural defeat; explicitly counted','No retuning or product economy completion claim');sourceHashes=@($files|ForEach-Object {[ordered]@{file=$_.Name;sha256=(Get-FileHash $_.FullName -Algorithm SHA256).Hash}});summaries=$summaries;runs=$rows;sharedMetaComparison=$sharedComparison}
$result|ConvertTo-Json -Depth 12|Set-Content (Join-Path $root 'docs/evidence/bokumyaku-progression-v7.json') -Encoding utf8
$summaries|ConvertTo-Json -Depth 5

[pscustomobject]$sharedComparison|Select-Object meta,conditions,recordDepthWins,depthTies,farmingEfficiencyWins,meanRecordCompletedWaveDelta|ConvertTo-Json -Depth 4
