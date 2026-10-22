param([string]$BaselineRef='03628c2',[ValidateRange(1,3)][int]$Candidate=1,[switch]$PostSelectionValidation)
$ErrorActionPreference='Stop'
if($PostSelectionValidation -and $Candidate -ne 2){throw 'Post-selection validation is fixed to adopted candidate2; no further tuning'}
$root=Split-Path -Parent $PSScriptRoot
$commit=(& git -C $root rev-parse --verify "$BaselineRef^{commit}").Trim()
if($LASTEXITCODE -ne 0){throw 'Baseline unavailable'}
$paths=@(& git -C $root ls-tree -r --name-only $commit -- UnitySandbox/Assets/BokumyakuPrototype/Simulation)|Where-Object{$_ -like '*.cs'}|Sort-Object
$parts=@();$hashes=@()
function HashText([string]$s){$sha=[Security.Cryptography.SHA256]::Create();try{([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($s)))).Replace('-','')}finally{$sha.Dispose()}}
foreach($path in $paths){$lines=@(& git -C $root show "${commit}:$path");if($LASTEXITCODE -ne 0){throw "Cannot read $path"};$content=($lines -join "`n")+"`n";$hashes += [ordered]@{path=$path;sha256=HashText $content};$parts += ($content -replace '(?m)^using [^;]+;\s*','')}
$original="using System;`nusing System.Collections.Generic;`nusing System.IO;`nusing System.Reflection;`n"+($parts -join "`n")
$needle='levels[3] < 6 && levels[2] >= 2 * next && levels[0] >= next'
if(([regex]::Matches($original,[regex]::Escape($needle))).Count -ne 1){throw 'Record branch condition drifted'}
$runner=@'
namespace VARIANT {
 public class Purchase {public int tick,branchLevel,supplyLevel,tips,nodes;}
 public class Row {public int nodes;public double meanPressure;public Purchase[] branchPurchases;public int seed,wave,ticks,crystals,tips,income,ink,repairs;public string variant,meta,weapon,build,status;public int[] levels;public double seconds,rate;}
 public static class Measurement {public static Row Run(int seed,int weapon,int build,bool grown,string variant){var m=grown?new MetaProgress{Damage=14,Supply=13,Growth=14,Harvest=12}:new MetaProgress();var r=new RunSimulator(seed,m);r.SetWeapon((WeaponKind)weapon);r.SetBuild((BuildPreset)build);var purchases=new List<Purchase>();int branchLevel=0;while(r.State==RunStatus.Running){r.Tick();if(r.TickCount%30==0 && r.UpgradeLevels[3]!=branchLevel){branchLevel=r.UpgradeLevels[3];purchases.Add(new Purchase{tick=r.TickCount,branchLevel=branchLevel,supplyLevel=r.UpgradeLevels[2],tips=r.TipCount,nodes=r.Branches.Count});}}double pressure=0;foreach(var tip in r.Branches)if(tip.IsTip)pressure+=r.Pressure(tip);return new Row{nodes=r.Branches.Count,meanPressure=pressure/r.TipCount,branchPurchases=purchases.ToArray(),seed=seed,wave=r.CompletedWave,ticks=r.TickCount,crystals=r.CrystalsReward,tips=r.TipCount,income=r.IncomeEarned,ink=r.RunInk,repairs=r.RepairCount,variant=variant,meta=grown?"14/13/14/12":"0/0/0/0",weapon=r.Weapon.ToString(),build=r.Build.ToString(),status=r.State.ToString(),levels=r.UpgradeLevels,seconds=r.ElapsedSeconds,rate=r.CrystalsReward*60/(r.ElapsedSeconds+3)};}}
}
'@
$variantName=@('Branch9','PrioritySupply','SupplyCycle')[$Candidate-1]
$rows=@();$compiledHashes=@()
$seeds=if($PostSelectionValidation){11..20}else{1..10}
foreach($variant in @('Baseline',$variantName)){
 $namespace="Bokumyaku.RecordV8$variant"
 $src=$original;if($variant -eq $variantName){$src=$src.Replace($needle,'levels[3] < 9 && levels[2] >= 2 * next && levels[0] >= next')}
 if($variant -ne 'Baseline' -and $Candidate -eq 2){$src=$src.Replace('int next = levels[3] + 1;', 'int next = levels[3] + 1; if (levels[3] >= 6 && levels[3] < 9 && levels[0] >= next && levels[2] < 2 * next) { TryBuy(UpgradeKind.Supply); return; }')}
 if($variant -ne 'Baseline' -and $Candidate -eq 3){$src=$src.Replace('UpgradeKind.Supply, UpgradeKind.Speed, UpgradeKind.Damage, UpgradeKind.Width, UpgradeKind.Bleed, UpgradeKind.Damage','UpgradeKind.Supply, UpgradeKind.Speed, UpgradeKind.Damage, UpgradeKind.Width, UpgradeKind.Supply, UpgradeKind.Damage')}
 $src=$src.Replace('namespace Bokumyaku.Simulation',"namespace $namespace")
 $compiledHashes += [ordered]@{variant=$variant;compilerInputSha256=HashText ($src+$runner.Replace('VARIANT',$namespace))}
 Add-Type -TypeDefinition ($src+$runner.Replace('VARIANT',$namespace))
 $type="$namespace.Measurement" -as [type]
 foreach($grown in @($false,$true)){foreach($seed in $seeds){foreach($weapon in 0..5){foreach($build in @(1,2)){$rows += $type::Run($seed,$weapon,$build,$grown,$variant)}}}}
}
if(@($rows|Where-Object{$_.status -ne 'Defeated' -and $_.status -ne 'TestCompleted'}).Count -gt 0){throw 'Unexpected terminal state'}
$paired=@(foreach($meta in @('0/0/0/0','14/13/14/12')){foreach($variant in @('Baseline',$variantName)){foreach($weapon in @('Point','Pierce','Bleed','Sweep','Enso','Resonance')){
 $dw=@();$dr=@();foreach($seed in $seeds){$f=$rows|Where-Object{$_.meta -eq $meta -and $_.variant -eq $variant -and $_.weapon -eq $weapon -and $_.seed -eq $seed -and $_.build -eq 'Farming'};$r=$rows|Where-Object{$_.meta -eq $meta -and $_.variant -eq $variant -and $_.weapon -eq $weapon -and $_.seed -eq $seed -and $_.build -eq 'Record'};$dw+=($r.wave-$f.wave);$dr+=($f.rate-$r.rate)}
 [ordered]@{meta=$meta;variant=$variant;weapon=$weapon;recordDepthWins=@($dw|Where-Object{$_ -gt 0}).Count;recordDepthTies=@($dw|Where-Object{$_ -eq 0}).Count;farmingRateWins=@($dr|Where-Object{$_ -gt 0}).Count;meanRecordWaveMinusFarming=($dw|Measure-Object -Average).Average;meanFarmingRateMinusRecord=($dr|Measure-Object -Average).Average}
}}})
$paired=@($paired|ForEach-Object{[pscustomobject]$_})
$comparisons=@(foreach($meta in @('0/0/0/0','14/13/14/12')){foreach($weapon in @('Point','Pierce','Bleed','Sweep','Enso','Resonance')){
 $dw=@();$dr=@();$same=0;foreach($seed in $seeds){$b=$rows|Where-Object{$_.meta -eq $meta -and $_.variant -eq 'Baseline' -and $_.weapon -eq $weapon -and $_.seed -eq $seed -and $_.build -eq 'Record'};$c=$rows|Where-Object{$_.meta -eq $meta -and $_.variant -eq $variantName -and $_.weapon -eq $weapon -and $_.seed -eq $seed -and $_.build -eq 'Record'};$dw+=($c.wave-$b.wave);$dr+=($c.rate-$b.rate)
 $f=@();foreach($v in @('Baseline',$variantName)){$f+=@($rows|Where-Object{$_.meta -eq $meta -and $_.variant -eq $v -and $_.weapon -eq $weapon -and $_.seed -eq $seed -and $_.build -eq 'Farming'})};if($f.Count -ne 2){throw 'Expected exactly one Farming row per variant'};if($f[0].status -eq $f[1].status -and $f[0].wave -eq $f[1].wave -and $f[0].ticks -eq $f[1].ticks -and $f[0].crystals -eq $f[1].crystals -and ($f[0].levels -join ',') -eq ($f[1].levels -join ',') -and $f[0].income -eq $f[1].income -and $f[0].ink -eq $f[1].ink){$same++}else{throw 'Farming changed'}
 };[ordered]@{meta=$meta;weapon=$weapon;candidateRecordWaveWins=@($dw|Where-Object{$_ -gt 0}).Count;candidateRecordWaveTies=@($dw|Where-Object{$_ -eq 0}).Count;meanRecordWaveGain=($dw|Measure-Object -Average).Average;meanRecordRateGain=($dr|Measure-Object -Average).Average;farmingMeasuredMetricsIdentical=$same}
}})
$initial=@($paired|Where-Object{$_.meta -eq '0/0/0/0' -and $_.variant -eq $variantName});$point=$initial|Where-Object weapon -eq Point
$pass=($point.recordDepthWins -ge 8 -and $point.farmingRateWins -ge 8 -and ($initial|Measure-Object recordDepthWins -Sum).Sum -ge 48 -and ($initial|Measure-Object farmingRateWins -Sum).Sum -ge 48 -and ($initial|Measure-Object recordDepthWins -Minimum).Minimum -ge 6 -and ($initial|Measure-Object farmingRateWins -Minimum).Minimum -ge 6)
# Fixed decision after three candidates: B is a limited improvement, not a grown-role gate pass.
# Validation11..20 preserves initial baseline but fails generalized G04; grown Bleed18 loses1Wave.
$report=[ordered]@{classification='Standalone in-memory bounded Record purchase-policy experiment against immutable rules3; does not edit production';baselineRef=$BaselineRef;baselineCommit=$commit;sourceHashFormat='UTF8 LF one final LF';sourceHashes=$hashes;compiledVariants=$compiledHashes;candidateNumber=$Candidate;fixedDecision='Candidate2 adopted as limited improvement after3 candidates; grown gate remains unmet; initial preset G04 passes but additional-seed G04 fails; grown Bleedseed18 regresses1Wave, no retuning';candidate=$variantName;candidateRules=@('All: Record cap9, Supply>=2next and Damage>=next maintained','B: afterBranch6 prioritise Supply until next branch condition met; buys at normal30tick boundary, waits for cost','C: cycle replaces Bleed with Supply, all stages; no other modification');seeds=@($seeds);postSelectionValidation=$PostSelectionValidation.IsPresent;metaScenarios=@('Damage/Supply/Growth/Harvest=0/0/0/0','14/13/14/12');rateDefinition='Ordinary crystals/(seconds+3)*60; no first-record milestone';terminalStatuses=@($rows|Group-Object variant,meta,build,status|ForEach-Object{[ordered]@{group=$_.Name;count=$_.Count}});initialG04Pass=$pass;strictDepthNote='TestCompleted atWave60 censors deeper survival; ties never count as depth wins. Fixed grown meta is not a chronological campaign.';pairedBuilds=$paired;candidateAgainstBaselineRecord=$comparisons;runs=$rows;limitations=@('Fixed grown meta is a controlled snapshot, not longitudinal progression simulation','Same seeds shared by weapons; not independent per-weapon populations','No Unity performance or optimum claim; at most3 bounded candidates','Farming equality refers to reported terminal metrics, not full state fingerprint')}
$suffix=if($PostSelectionValidation){'fixed11to20'}else{"candidate$Candidate"}
$report|ConvertTo-Json -Depth 12|Set-Content "$root/docs/evidence/record-v8-$suffix.json" -Encoding utf8
$paired|ForEach-Object{[pscustomobject]$_}|Format-Table
$comparisons|ForEach-Object{[pscustomobject]$_}|Format-Table
"Initial G04 pass: $pass; total runs $($rows.Count)"
