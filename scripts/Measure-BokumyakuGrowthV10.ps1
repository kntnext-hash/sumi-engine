param([ValidateRange(1,3)][int]$Candidate=1,[ValidateSet('Screen','Extended','Initial','Verify')][string]$Mode='Screen')
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$commit=(& git -C $root rev-parse --verify 'bc04d5b^{commit}').Trim()
if($LASTEXITCODE -ne 0){throw 'Missing baseline'}
$paths=@(& git -C $root ls-tree -r --name-only $commit -- UnitySandbox/Assets/BokumyakuPrototype/Simulation)|Where-Object{$_ -like '*.cs'}|Sort-Object
$parts=@();$hashes=@()
function Hash([string]$text){$h=[Security.Cryptography.SHA256]::Create();try{([BitConverter]::ToString($h.ComputeHash([Text.Encoding]::UTF8.GetBytes($text)))).Replace('-','')}finally{$h.Dispose()}}
foreach($path in $paths){$text=(@(& git -C $root show "${commit}:$path") -join "`n")+"`n";if($LASTEXITCODE -ne 0){throw 'Blob failure'};$hashes += [ordered]@{path=$path;sha256=Hash $text};$parts += ($text -replace '(?m)^using [^;]+;\s*','')}
$source="using System;`nusing System.Collections.Generic;`nusing System.IO;`nusing System.Reflection;`n"+($parts -join "`n")
$baselineSource=$source
$budget=if($Mode -in @('Extended','Verify')){720000}else{180000}
$cap=if($Candidate -eq 1){80}else{100}
$source=$source.Replace('Damage > 20','Damage > 100').Replace('Level(kind) >= 20','Level(kind) >= (kind == MetaUpgrade.Damage ? 100 : 20)')
$source=$source.Replace('public int Cost(MetaUpgrade kind) { return','public int Cost(MetaUpgrade kind) { if(kind == MetaUpgrade.Damage && Damage >= 20) return (int)Math.Ceiling(20339 * Math.Pow(1+.05*(Damage-20),2)); return')
$source=$source.Replace('int[] caps = { 30,',"int[] caps = { $cap,")
$source=$source.Replace('int[] costs = { 10, 12, 15, 22, 12, 18 }; int l = levels[index];','int[] costs = { 10, 12, 15, 22, 12, 18 }; int l = levels[index]; if(index==0 && l>=30)return (int)Math.Ceiling(7040*Math.Pow(1+.03*(l-30),2));')
$source=$source.Replace('(1 + .025 * meta.Damage)','(meta.Damage<=20?1+.025*meta.Damage:1.5*Math.Pow(1+.025*(meta.Damage-20),2))')
if($Candidate -eq 3){$source=$source.Replace('Math.Pow(1.10, levels[0])','(levels[0]<=30?Math.Pow(1.10,levels[0]):Math.Pow(1.10,30)*Math.Pow(1.12,levels[0]-30))')}
if($Mode -eq 'Verify'){$productionFiles=Get-ChildItem "$root/UnitySandbox/Assets/BokumyakuPrototype/Simulation/*.cs"|Sort-Object Name;$source="using System;`nusing System.Collections.Generic;`nusing System.IO;`nusing System.Reflection;`n"+(($productionFiles|ForEach-Object{[IO.File]::ReadAllText($_.FullName)-replace '(?m)^using [^;]+;\s*',''})-join "`n")}
$runner=@'
namespace Bokumyaku.GrowthV10 {
 public class Row {public int seed,metaDamage,ticks,wave,completedWave,crystals,income,ink,tips,nodes;public string build,weapon,status;public bool censored;public int[] levels;public double seconds,provisionalCrystalRate,maxHit,maxField,effectiveDamage;}
 public static class Trial {public static Row Run(int metaDamage,int weapon,int build,int seed=1){int other=metaDamage==0?0:20;var r=new Simulation.RunSimulator(seed,new Simulation.MetaProgress{Damage=metaDamage,Supply=other,Growth=other,Harvest=other});r.SetWeapon((Simulation.WeaponKind)weapon);r.SetBuild((Simulation.BuildPreset)build);double hit=0,field=0;while(r.State==Simulation.RunStatus.Running && r.TickCount<180000){r.Tick();foreach(var a in r.Attacks)hit=Math.Max(hit,a.Damage);foreach(var f in r.Fields)field=Math.Max(field,f.DamagePerTick);if(r.RunInk<0 || r.IncomeEarned<0 || double.IsNaN(r.EffectiveDamage)||double.IsInfinity(r.EffectiveDamage))throw new Exception("Invalid arithmetic");}if(r.State==Simulation.RunStatus.CapacityExceeded)throw new Exception("Capacity failure");return new Row{seed=seed,metaDamage=metaDamage,ticks=r.TickCount,wave=r.Wave,completedWave=r.CompletedWave,crystals=r.CrystalsReward,income=r.IncomeEarned,ink=r.RunInk,tips=r.TipCount,nodes=r.Branches.Count,build=r.Build.ToString(),weapon=r.Weapon.ToString(),status=r.State.ToString(),censored=r.State==Simulation.RunStatus.Running,levels=r.UpgradeLevels,seconds=r.ElapsedSeconds,provisionalCrystalRate=Math.Floor(r.RewardPoints*.08)*60/(r.ElapsedSeconds+3),maxHit=hit,maxField=field,effectiveDamage=r.EffectiveDamage};}}
}
'@
$runner=$runner.Replace('180000',"$budget")
Add-Type -TypeDefinition ($source+$runner)
if($Mode -eq 'Verify'){
 $count=0
 foreach($name in @('growth-v10-candidate3-Initial.json','growth-v10-candidate3-Extended.json')){$expected=Get-Content "$root/docs/evidence/$name" -Raw|ConvertFrom-Json;foreach($e in $expected.runs){$w=[Enum]::Parse([Bokumyaku.Simulation.WeaponKind],$e.weapon);$b=[Enum]::Parse([Bokumyaku.Simulation.BuildPreset],$e.build);$r=[Bokumyaku.GrowthV10.Trial]::Run($e.metaDamage,$w,$b,$e.seed);foreach($key in @('ticks','completedWave','crystals','income','ink','status')){if($r.$key -ne $e.$key){throw "Production mismatch $name seed$($e.seed) $($e.weapon)/$($e.build) $key"}};if(($r.levels -join ',') -ne ($e.levels -join ',')){throw 'Production level mismatch'};$count++}}
 [ordered]@{classification='Actual rules6 compared with fixed candidateC; no new tuning';matchedConditions=$count;pass=$true;sourceHashes=@($productionFiles|ForEach-Object{[ordered]@{name=$_.Name;sha256=(Get-FileHash $_.FullName).Hash}});comparedFields=@('ticks','completedWave','crystals','income','ink','status','all7levels')}|ConvertTo-Json -Depth 8|Set-Content "$root/docs/evidence/growth-v10-production-match.json" -Encoding utf8
 "PASS production matches$count candidateC conditions";return
}
$baselineRows=@()
if($Mode -eq 'Initial'){
 $base=$baselineSource.Replace('namespace Bokumyaku.Simulation','namespace Bokumyaku.GrowthBaseline')
 $baseRunner=$runner.Replace('namespace Bokumyaku.GrowthV10','namespace Bokumyaku.GrowthBaselineTrial').Replace('Simulation.','Bokumyaku.GrowthBaseline.')
 Add-Type -TypeDefinition ($base+$baseRunner)
 $baselineRows=@(foreach($seed in 1..3){foreach($m in @(0,20)){foreach($w in 0..5){foreach($b in @(1,2)){[Bokumyaku.GrowthBaselineTrial.Trial]::Run($m,$w,$b,$seed)}}}})
 $rows=@(foreach($seed in 1..3){foreach($m in @(0,20)){foreach($w in 0..5){foreach($b in @(1,2)){[Bokumyaku.GrowthV10.Trial]::Run($m,$w,$b,$seed)}}}})
}else{
 $metaRange=if($Mode -eq 'Extended'){@(100)}else{@(20,60,100)}
$rows=@(foreach($m in $metaRange){foreach($w in @(0,4)){foreach($b in @(1,2)){[Bokumyaku.GrowthV10.Trial]::Run($m,$w,$b)}}})
}
$maxMultiplier=if($Candidate -eq 3){[Math]::Pow(1.10,30)*[Math]::Pow(1.12,70)}else{[Math]::Pow(1.10,$cap)}
$maxHit=7.5*$maxMultiplier*13.5*1.65
$report=[ordered]@{classification='In-memory screening trial, not production rules and not a reachability guarantee';baselineCommit=$commit;sourceHashes=$hashes;compilerInputSha256=Hash ($source+$runner);candidate=$Candidate;runDamageCap=$cap;runDamageGrowthAfter30= $(if($Candidate -eq 3){1.12}else{1.10});runCost='Lv>=30 ceil7040*(1+.03*(lv-30))^2';metaDamageCap=100;metaDamageMultiplier='Lv<=20 legacy; above20 1.5*(1+.025*(lv-20))^2';metaDamageCost='Lv>=20 ceil20339*(1+.05*(lv-20))^2';otherMetaLevels=20;mode=$Mode;tickBudget=$budget;waveBudget=$budget/360;maxPossibleSingleHit=$maxHit;conservativeEffectiveDamageBound=($maxHit*6*12*4.5*1200000+$maxHit*.85*2.2/30*2*96*36000000);limitations=@('Only seed1 and Point/Enso screened; no optimality claim','Running rows are observation-budget censored, not completed runs; provisional rate is not settled reward','Currency bounds unchanged because Supply/Growth/Harvest remain capped20 and eligible kill rewards unchanged','Snapshot validation is not modified in these experimental copies; adoption requires versioned caps and new damage bounds');baselineRuns=$baselineRows;runs=$rows}
$report|ConvertTo-Json -Depth 10|Set-Content "$root/docs/evidence/growth-v10-candidate$Candidate-$Mode.json" -Encoding utf8
$rows|Select-Object metaDamage,weapon,build,status,completedWave,@{n='damageLv';e={$_.levels[0]}},income,provisionalCrystalRate|Format-Table
