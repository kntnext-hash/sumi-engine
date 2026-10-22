param([int]$Candidate=0)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$files=Get-ChildItem "$root/UnitySandbox/Assets/BokumyakuPrototype/Simulation/*.cs"|Sort-Object Name
$source="using System;`nusing System.Collections.Generic;`nusing System.IO;`nusing System.Reflection;`n"+(($files|ForEach-Object{[IO.File]::ReadAllText($_.FullName)-replace '(?m)^using [^;]+;\s*',''})-join "`n")
if($Candidate -lt 0 -or $Candidate -gt 2){throw 'Candidate must be 0..2'}
$gain=@('.12','.5','1.5')[$Candidate]
$source=$source.Replace('HarvestGainPerLevel = 1.5',"HarvestGainPerLevel = $gain")
if($Candidate -eq 0){$source=$source.Replace('Wave < 15 ? 0 : 1 + (Wave - 15) / 3','(Wave - 10) / 5')}
$runner=@'
namespace Bokumyaku.HarvestMeasurement {
 public sealed class Row { public int seed,wave,ticks,crystals; public string build,weapon,status; public int[] levels; public double seconds,rate; }
 public static class Runner { public static Row Run(int seed,Simulation.BuildPreset build,Simulation.WeaponKind weapon) {
 var r=new Simulation.RunSimulator(seed);r.SetBuild(build);r.SetWeapon(weapon);
 while(r.State==Simulation.RunStatus.Running)r.Tick();
 return new Row{status=r.State.ToString(),seed=seed,wave=r.CompletedWave,ticks=r.TickCount,crystals=r.CrystalsReward,build=build.ToString(),weapon=weapon.ToString(),levels=r.UpgradeLevels,seconds=r.ElapsedSeconds,rate=r.CrystalsReward*60/(r.ElapsedSeconds+3)};
 } }
}
'@
Add-Type -TypeDefinition ($source+$runner)
$rows=@(foreach($seed in 1..20){foreach($w in [Enum]::GetValues([Bokumyaku.Simulation.WeaponKind])){if($seed -gt 10 -and $w -ne 0){continue};foreach($b in @(1,2)){[Bokumyaku.HarvestMeasurement.Runner]::Run($seed,$b,$w)}}})
$pairs=@(foreach($w in [Enum]::GetValues([Bokumyaku.Simulation.WeaponKind])){
 $dw=@();$dr=@();foreach($seed in 1..10){$f=$rows|Where-Object{$_.seed -eq $seed -and $_.weapon -eq "$w" -and $_.build -eq 'Farming'};$r=$rows|Where-Object{$_.seed -eq $seed -and $_.weapon -eq "$w" -and $_.build -eq 'Record'};$dw+=($r.wave-$f.wave);$dr+=($f.rate-$r.rate)}
 [ordered]@{weapon="$w";recordDepthWins=@($dw|Where-Object{$_ -gt 0}).Count;farmingRateWins=@($dr|Where-Object{$_ -gt 0}).Count;meanWaveDifference=($dw|Measure-Object -Average).Average;meanRateDifference=($dr|Measure-Object -Average).Average}
})
$report=[ordered]@{candidate=$Candidate;classification='Actual standalone fixed-tick simulation, meta0 Balanced; no Unity performance claim';gain=[Bokumyaku.Simulation.RunSimulator]::HarvestGainPerLevel;cost=[Bokumyaku.Simulation.RunSimulator]::HarvestBaseCost;growth=[Bokumyaku.Simulation.RunSimulator]::HarvestCostGrowth;additionalValidationSeeds='11..20 Point only; inspected for all candidates, not an untouched holdout';schedule= $(if($Candidate -eq 0){'W15 then every5 waves'}else{'W15 then every3 waves'});sourceVariant='Gain and schedule substituted in memory by Candidate; listed hashes identify current source inputs';threshold='Point >=8/10 and all >=48/60, every weapon >=6/10 for both metrics';secondsWithin300to480=@($rows|Where-Object{$_.build -eq 'Farming' -and $_.weapon -eq 'Point' -and $_.seconds -ge 300 -and $_.seconds -le 480}).Count;pairs=$pairs;runs=$rows;sourceHashes=@($files|ForEach-Object{[ordered]@{name=$_.Name;sha256=(Get-FileHash $_.FullName).Hash}})}
function Median($v){$a=@($v|Sort-Object);($a[[int][Math]::Floor(($a.Count-1)/2)]+$a[[int][Math]::Floor($a.Count/2)])/2}
$report['summaries']=@(foreach($g in ($rows|Where-Object seed -le 10|Group-Object build,weapon)){[ordered]@{build=$g.Group[0].build;weapon=$g.Group[0].weapon;medianSeconds=Median $g.Group.seconds;medianWave=Median $g.Group.wave;medianRate=Median $g.Group.rate;meanHarvestLevel=($g.Group|ForEach-Object{$_.levels[6]}|Measure-Object -Average).Average;endStates=@($g.Group.status)}})
if($Candidate -eq 2){$report['postFreezeSeeds21to30']=@(foreach($seed in 21..30){[Bokumyaku.HarvestMeasurement.Runner]::Run($seed,1,0)})}
$report|ConvertTo-Json -Depth 12|Set-Content "$root/docs/evidence/bokumyaku-balance-v6-harvest-$Candidate.json" -Encoding utf8
$pairs|ForEach-Object{[pscustomobject]$_}|Format-Table
"Farming Point 300-480: $($report.secondsWithin300to480)/20"
