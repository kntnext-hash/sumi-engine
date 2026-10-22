[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$inputPath=Join-Path $root 'docs/evidence/bokumyaku-weapons-v3.json'
$sourcePath=Join-Path $root 'UnitySandbox/Assets/BokumyakuPrototype/Simulation/RunSimulator.cs'
$data=Get-Content $inputPath -Raw|ConvertFrom-Json
function Hash-Text([string]$text){return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($text)))}
$rawHash=(Get-FileHash $sourcePath).Hash
$source=[IO.File]::ReadAllText($sourcePath)
$lf=$source.Replace("`r`n","`n").Replace("`r","`n")
$lfHash=Hash-Text $lf
$crlfHash=Hash-Text ($lf.Replace("`n","`r`n"))
$match=if($data.sourceSha256 -eq $rawHash){'exact raw bytes'}elseif($data.sourceSha256 -eq $lfHash){'UTF-8 without BOM, LF line endings'}elseif($data.sourceSha256 -eq $crlfHash){'UTF-8 without BOM, CRLF line endings'}else{throw 'Input simulation source hash does not match current content or newline-only variants.'}
if($data.runs.Count -ne 90){throw 'Expected 90 existing conditions'}
foreach($r in $data.runs){$expected=if($r.completedWave -eq 0){0}else{[Math]::Floor(4+.25*[Math]::Pow($r.completedWave,1.4))};if($r.crystals -ne $expected){throw "Reward mismatch seed=$($r.seed)"};if($r.seconds -le 0 -or $r.status -ne 'Defeated'){throw 'Invalid completed-run input'}}
function Median($values){$s=@($values|Sort-Object);return ($s[4]+$s[5])/2}
function Correlation($xs,$ys){$mx=($xs|Measure-Object -Average).Average;$my=($ys|Measure-Object -Average).Average;$cov=0.0;$vx=0.0;$vy=0.0;for($i=0;$i -lt $xs.Count;$i++){$dx=$xs[$i]-$mx;$dy=$ys[$i]-$my;$cov+=$dx*$dy;$vx+=$dx*$dx;$vy+=$dy*$dy};if($vx -eq 0 -or $vy -eq 0){return $null};return $cov/[Math]::Sqrt($vx*$vy)}
$candidates=@()
foreach($exponent in @(1.4,1.2,1.0)){
$rows=@(foreach($r in $data.runs){$reward=if($r.completedWave -eq 0){0}else{[Math]::Floor(4+.25*[Math]::Pow($r.completedWave,$exponent))};[pscustomobject]@{seed=$r.seed;build=$r.build;weapon=$r.weapon;completedWave=$r.completedWave;seconds=$r.seconds;cycleSeconds=$r.seconds+3;crystals=$reward;crystalsPerMinute=$reward*60/($r.seconds+3)}})
$summaries=@(foreach($g in ($rows|Group-Object build,weapon)){[ordered]@{build=$g.Group[0].build;weapon=$g.Group[0].weapon;conditions=$g.Count;medianCrystalsPerMinute=Median $g.Group.crystalsPerMinute;medianCompletedWave=Median $g.Group.completedWave;medianCycleSeconds=Median $g.Group.cycleSeconds}})
$winners=@(foreach($seed in 1..10){$rank=@($rows|Where-Object seed -eq $seed|Sort-Object crystalsPerMinute -Descending);$best=$rank[0].crystalsPerMinute;$ties=@($rank|Where-Object{[Math]::Abs($_.crystalsPerMinute-$best) -lt 1e-12});[ordered]@{seed=$seed;winners=@($ties|ForEach-Object{"$($_.build)/$($_.weapon)"});manualIsOptimal=@($ties|Where-Object build -eq 'Manual').Count -gt 0;bestPerMinute=$best;runnerUpPerMinute=$rank[1].crystalsPerMinute;advantagePercent=100*($best/$rank[1].crystalsPerMinute-1)}})
$candidates += [ordered]@{exponent=$exponent;coefficient=.25;constant=4;zeroWaveReward=0;waitSeconds=3;manualOptimalSeedCount=@($winners|Where-Object manualIsOptimal).Count;pearsonCompletedWaveVersusEfficiency=Correlation @($rows.completedWave) @($rows.crystalsPerMinute);summaries=$summaries;seedWinners=$winners;runs=$rows}
}
$report=[ordered]@{classification='Post-processing of existing v3 combat measurements; no new combat or Unity execution';input='docs/evidence/bokumyaku-weapons-v3.json';inputJsonSha256=(Get-FileHash $inputPath).Hash;inputSourceSha256=$data.sourceSha256;currentRawSourceSha256=$rawHash;currentLfSourceSha256=$lfHash;sourceMatch=$match;currentFormulaMatches=90;conditions=90;independentSeeds=10;generatedAtUtc=[DateTime]::UtcNow.ToString('o');method='Three reward formulas only. w=completedWave, floor(4+0.25*w^exponent), zero if w=0. Rate=reward*60/(measured seconds+3). Seeds/builds/weapons/meta and battle outcomes unchanged.';limitations=@('Reuses identical seed across nine configurations; 90 rows are not independent samples.','Pearson correlation is descriptive across this pooled sample, not causal or inferential.','No save overhead, player interaction time, meta progression over multiple runs, tiers, or hypothetical tier multipliers.','Manual means no purchases, not player-controlled optimized play.','Floor rounding and fixed reward offset materially affect short runs.');candidates=$candidates}
$report|ConvertTo-Json -Depth 12|Set-Content (Join-Path $root 'docs/evidence/bokumyaku-reward-candidates-v4.json') -Encoding utf8
"Source match: $match; original formula 90/90 matched"
foreach($c in $candidates){"Exponent $($c.exponent): manual optimal $($c.manualOptimalSeedCount)/10; Pearson=$($c.pearsonCompletedWaveVersusEfficiency)";$c.summaries|ForEach-Object{[pscustomobject]$_}|Format-Table build,weapon,medianCrystalsPerMinute;$c.seedWinners|ForEach-Object{[pscustomobject]$_}|Format-Table seed,winners,advantagePercent}
