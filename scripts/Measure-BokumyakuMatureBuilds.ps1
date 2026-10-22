[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$files=@(Get-ChildItem (Join-Path $root 'UnitySandbox/Assets/BokumyakuPrototype/Simulation') -Filter '*.cs'|Sort-Object Name)
Add-Type -Path $files.FullName
if([Bokumyaku.Simulation.RunSimulator]::CurrentRulesVersion -ne 3){throw 'This measurement requires rules3.'}
$rows=@()
foreach($seed in 1..10){
 foreach($build in @('Farming','Record')){
  $meta=[Bokumyaku.Simulation.MetaProgress]::new();$meta.Damage=14;$meta.Supply=13;$meta.Growth=14;$meta.Harvest=12
  $run=[Bokumyaku.Simulation.RunSimulator]::new($seed,$meta);$run.SetBuild([Bokumyaku.Simulation.BuildPreset]$build)
  $run.SetWeapon([Bokumyaku.Simulation.WeaponKind]::Point);$run.SetPolicy([Bokumyaku.Simulation.GrowthPolicy]::Balanced)
  while($run.State -eq 'Running'){$run.Tick();if($run.TickCount -gt [Bokumyaku.Simulation.RunSimulator]::MaxTicks){throw 'Finite bound failed'}}
  if($run.State -eq 'CapacityExceeded'){throw 'Capacity stop invalidates economy measurement'}
  $rows += [ordered]@{seed=$seed;build=$build;rulesVersion=$run.RulesVersion;status=$run.State.ToString();ticks=$run.TickCount;seconds=$run.ElapsedSeconds;completedWave=$run.CompletedWave;normalCrystals=$run.CrystalsReward;normalCrystalsPerMinute=60*$run.CrystalsReward/($run.ElapsedSeconds+3);levels=$run.UpgradeLevels}
 }
}
$pairs=@(foreach($seed in 1..10){$f=$rows|Where-Object {$_.seed -eq $seed -and $_.build -eq 'Farming'};$r=$rows|Where-Object {$_.seed -eq $seed -and $_.build -eq 'Record'};[ordered]@{seed=$seed;farmingCompletedWave=$f.completedWave;recordCompletedWave=$r.completedWave;recordCompletedWaveDelta=$r.completedWave-$f.completedWave;farmingCrystalsPerMinute=$f.normalCrystalsPerMinute;recordCrystalsPerMinute=$r.normalCrystalsPerMinute}})
$summary=[ordered]@{recordDepthWins=@($pairs|Where-Object {$_.recordCompletedWaveDelta -gt 0}).Count;depthTies=@($pairs|Where-Object {$_.recordCompletedWaveDelta -eq 0}).Count;farmingEfficiencyWins=@($pairs|Where-Object {$_.farmingCrystalsPerMinute -gt $_.recordCrystalsPerMinute}).Count;meanRecordCompletedWaveDelta=($pairs.recordCompletedWaveDelta|Measure-Object -Average).Average;testCompletedRuns=@($rows|Where-Object {$_.status -eq 'TestCompleted'}).Count}
$result=[ordered]@{classification='Standalone C# simulation; not Unity/device/persistence/product economy acceptance';generatedAtUtc=[DateTime]::UtcNow.ToString('o');conditions=20;rulesVersion=3;weapon='Point';growthPolicy='Balanced';meta=[ordered]@{Damage=14;Supply=13;Growth=14;Harvest=12;Crystals=0};metaProvenance='Fixed evaluation point derived from v0.6 rules2 nineteen-round Farming cheapest-meta-purchase diagnostic; NOT a v0.7 twenty-round progression result';policy='Clone fixed meta independently for every seed/build. No post-run purchases or first-time milestone rewards. Normal crystals/min includes three-second retry delay.';limitations=@('One fixed mature meta and Point only','Seeds1..10, finite comparison without tuning','No old evidence replaced');sourceHashes=@($files|ForEach-Object {[ordered]@{file=$_.Name;sha256=(Get-FileHash $_.FullName -Algorithm SHA256).Hash}});summary=$summary;pairs=$pairs;runs=$rows}
$result|ConvertTo-Json -Depth 10|Set-Content (Join-Path $root 'docs/evidence/bokumyaku-mature-builds-v7.json') -Encoding utf8
$summary|ConvertTo-Json
