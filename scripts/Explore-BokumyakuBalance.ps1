[CmdletBinding()]
param([string]$BaselineRef='ec15566',[switch]$ValidateSourceOnly)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
# Read immutable Git blobs; never use the current rules2 worktree for the v5 comparison.
$baselineSha=(& git -C $root rev-parse --verify "$BaselineRef^{commit}")
if($LASTEXITCODE -ne 0){throw "Baseline commit unavailable: $BaselineRef"}
$baselineSha="$baselineSha".Trim()
$paths=@(& git -C $root ls-tree -r --name-only $baselineSha -- 'UnitySandbox/Assets/BokumyakuPrototype/Simulation')
if($LASTEXITCODE -ne 0){throw 'Cannot list baseline Simulation sources'}
$paths=@($paths|Where-Object{$_ -like '*.cs'}|Sort-Object)
if($paths.Count -eq 0){throw 'Baseline contains no Simulation C# sources'}
$sourceHashes=@();$sourceParts=@()
foreach($path in $paths){
 $lines=@(& git -C $root show "${baselineSha}:$path")
 if($LASTEXITCODE -ne 0){throw "Cannot read baseline blob: $path"}
 # Hash the exact compiler input for each file, normalized to UTF-8 with LF and one final LF.
 $content=($lines -join "`n")+"`n"
 $hash=[Security.Cryptography.SHA256]::Create()
 try{$digest=([BitConverter]::ToString($hash.ComputeHash([Text.Encoding]::UTF8.GetBytes($content)))).Replace('-','')}finally{$hash.Dispose()}
 $sourceHashes += [ordered]@{name=[IO.Path]::GetFileName($path);path=$path;sha256=$digest}
 $sourceParts += ($content -replace '(?m)^using [^;]+;\s*','')
}
$source="using System;`nusing System.Collections.Generic;`nusing System.IO;`nusing System.Reflection;`n"+($sourceParts -join "`n")
if($ValidateSourceOnly){[ordered]@{baselineRef=$BaselineRef;baselineCommit=$baselineSha;hashFormat='UTF-8, LF, one final LF';sourceHashes=$sourceHashes}|ConvertTo-Json -Depth 5;return}
$runner=@'
namespace Bokumyaku.BalanceV6 {
 public sealed class Row {
  public string condition,weapon,status;
  public int seed,ticks,wave,completedWave,kills,leaks,tips,repairs,crystals,income,remainingInk;
  public int[] levels;
  public double seconds,crystalsPerMinute,incomePerMinute;
 }
 public static class Experiment {
  static readonly Simulation.UpgradeKind[][] Cycles={
   new[]{Simulation.UpgradeKind.Damage,Simulation.UpgradeKind.Supply,Simulation.UpgradeKind.Speed,Simulation.UpgradeKind.Damage,Simulation.UpgradeKind.Width},
   new[]{Simulation.UpgradeKind.Supply,Simulation.UpgradeKind.Speed,Simulation.UpgradeKind.Damage,Simulation.UpgradeKind.Width,Simulation.UpgradeKind.Bleed,Simulation.UpgradeKind.Damage},
   new[]{Simulation.UpgradeKind.Damage,Simulation.UpgradeKind.Speed,Simulation.UpgradeKind.Supply,Simulation.UpgradeKind.Width,Simulation.UpgradeKind.Supply,Simulation.UpgradeKind.Damage}};
  static void Buy(Simulation.RunSimulator r,int candidate,ref int cursor) {
   if(r.State!=Simulation.RunStatus.Running)return;
   // One paid action per second; repair and branch reserve do not advance the combat cycle.
   if(candidate==2&&r.CoreHp<=4&&r.RepairCount<5){r.TryRepair();return;}
   var lv=r.UpgradeLevels;int next=lv[3]+1,target=candidate==0?3:candidate==1?6:9;
   bool ready=candidate==0?lv[2]>=2*next&&lv[0]>=2*next:
     candidate==1?lv[2]>=2*next&&lv[0]>=next:
     lv[2]>=2*next&&lv[0]>=2*next&&lv[1]>=(next+1)/2;
   if(lv[3]<target&&ready){r.TryBuy(Simulation.UpgradeKind.Branch);return;}
   int[] caps={30,25,25,9,10,10};var cycle=Cycles[candidate];
   for(int i=0;i<cycle.Length;i++){
    var kind=cycle[cursor%cycle.Length];if(lv[(int)kind]>=caps[(int)kind]){cursor++;continue;}
    if(r.TryBuy(kind))cursor++;return;
   }
  }
  public static Row Run(int seed,Simulation.WeaponKind weapon,int candidate) {
   var r=new Simulation.RunSimulator(seed);r.SetWeapon(weapon);
   if(candidate<0)r.SetBuild(candidate==-1?Simulation.BuildPreset.Farming:Simulation.BuildPreset.Record);
   int cursor=0;
   while(r.State==Simulation.RunStatus.Running){
    // Same purchase boundary as production AutoBuy: immediately before combat on tick30,60,...
    if(candidate>=0&&(r.TickCount+1)%30==0)Buy(r,candidate,ref cursor);
    r.Tick();
    if(r.TickCount>Simulation.RunSimulator.MaxTicks||r.TipCount>12||r.RunInk<0||r.CoreHp>10)throw new Exception("Invariant failure");
   }
   return new Row{condition=candidate==-1?"BaselineFarming":candidate==-2?"BaselineRecord":new[]{"A_Concentrated6","B_SupplyLed9","C_Reserved12Repair"}[candidate],weapon=weapon.ToString(),seed=seed,status=r.State.ToString(),ticks=r.TickCount,seconds=r.ElapsedSeconds,wave=r.Wave,completedWave=r.CompletedWave,kills=r.Kills,leaks=r.Leaks,tips=r.TipCount,repairs=r.RepairCount,crystals=r.CrystalsReward,income=r.IncomeEarned,remainingInk=r.RunInk,levels=r.UpgradeLevels,crystalsPerMinute=r.CrystalsReward*60/(r.ElapsedSeconds+3),incomePerMinute=r.IncomePerMinute};
  }
 }
}
'@
Add-Type -TypeDefinition ($source+"`n"+$runner)
$rows=@()
foreach($seed in 1..10){foreach($weapon in [Enum]::GetValues([Bokumyaku.Simulation.WeaponKind])){foreach($candidate in @(-1,-2,0,1,2)){$rows += [Bokumyaku.BalanceV6.Experiment]::Run($seed,$weapon,$candidate)}}}
function Stats($values){$s=@($values|Sort-Object);$n=$s.Count;[ordered]@{min=$s[0];median=($s[[int][Math]::Floor(($n-1)/2)]+$s[[int][Math]::Floor($n/2)])/2;max=$s[-1];mean=($s|Measure-Object -Average).Average}}
$summaries=@(foreach($g in ($rows|Group-Object condition,weapon)){[ordered]@{condition=$g.Group[0].condition;weapon=$g.Group[0].weapon;count=$g.Count;wave=Stats $g.Group.wave;seconds=Stats $g.Group.seconds;crystalsPerMinute=Stats $g.Group.crystalsPerMinute;tips=Stats $g.Group.tips;repairs=Stats $g.Group.repairs;within300to480=@($g.Group|Where-Object{$_.seconds -ge 300 -and $_.seconds -le 480}).Count;testCompleted=@($g.Group|Where-Object status -eq TestCompleted).Count}})
$paired=@(foreach($candidate in @('A_Concentrated6','B_SupplyLed9','C_Reserved12Repair')){foreach($weapon in [Enum]::GetValues([Bokumyaku.Simulation.WeaponKind])){
 $waveWins=0;$timeWins=0;$farmRateWins=0;$roleSplit=0
 foreach($seed in 1..10){$a=$rows|Where-Object{$_.seed -eq $seed -and $_.weapon -eq $weapon.ToString() -and $_.condition -eq $candidate};$b=$rows|Where-Object{$_.seed -eq $seed -and $_.weapon -eq $weapon.ToString() -and $_.condition -eq 'BaselineFarming'}
 if($a.wave -gt $b.wave){$waveWins++};if($a.ticks -gt $b.ticks){$timeWins++};if($b.crystalsPerMinute -gt $a.crystalsPerMinute){$farmRateWins++};if($a.wave -gt $b.wave -and $b.crystalsPerMinute -gt $a.crystalsPerMinute){$roleSplit++}}
 [ordered]@{candidate=$candidate;weapon=$weapon.ToString();pairedSeeds=10;candidateHigherWave=$waveWins;candidateLongerRun=$timeWins;baselineFarmingHigherCrystalRate=$farmRateWins;desiredRoleSplit=$roleSplit}
}})
$report=[ordered]@{classification='Standalone C# counterfactual purchase-controller experiment; production Simulation is unchanged';generatedAtUtc=[DateTime]::UtcNow.ToString('o');baselineRef=$BaselineRef;baselineCommit=$baselineSha;sourceHashFormat='UTF-8, LF, one final LF';sourceHashes=$sourceHashes;conditions=300;candidateConditions=180;baselineConditions=120;independentSeeds=10;metaLevels=@(0,0,0,0);policy='Balanced';candidateCount=3;rules=@('All conditions share production combat/enemy/reward equations and costs. No build-specific multipliers.','Baselines and candidates purchase before combat on ticks30,60,...; one paid action per second and identical available income at the purchase boundary.','Each candidate has independent combat-purchase cursor; unsuccessful purchases wait; capped upgrades skipped. Branch or repair purchase does not move combat cursor.','A: max6 tips; next branch requires Supply>=2*nextBranchLv and Damage>=2*nextBranchLv. Cycle Damage,Supply,Speed,Damage,Width.','B: max9 tips; next branch requires Supply>=2*nextBranchLv and Damage>=nextBranchLv. Cycle Supply,Speed,Damage,Width,Bleed,Damage.','C: max12 tips; next branch requires Supply>=2*nextBranchLv, Damage>=2*nextBranchLv, Speed>=ceil(nextBranchLv/2). Cycle Damage,Speed,Supply,Width,Supply,Damage. At HP<=4 reserve repair cost and try repair first; production cap5/paid cost applies.','Crystal rate includes 3sec retry wait, excludes PB milestone rewards. TestCompleted is censored, not natural defeat.');limitations=@('No production change, no optimality claim, no seed selection.','Ten paired seeds reused across six weapons; not 300 independent seeds.','Three candidates are a bounded exploration; repair is an explicit additional cost/action in C.');summaries=$summaries;pairedAgainstFarming=$paired;runs=$rows}
$out=Join-Path $root 'docs/evidence/bokumyaku-balance-v6-candidates.json';$report|ConvertTo-Json -Depth 12|Set-Content $out -Encoding utf8
$summaries|ForEach-Object{[pscustomobject]@{condition=$_.condition;weapon=$_.weapon;wave=$_.wave.median;seconds=$_.seconds.median;rate=$_.crystalsPerMinute.median;tips=$_.tips.median}}|Format-Table
$paired|ForEach-Object{[pscustomobject]$_}|Format-Table
"Saved $out"
