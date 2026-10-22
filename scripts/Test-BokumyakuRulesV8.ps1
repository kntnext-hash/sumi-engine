$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$files=Get-ChildItem "$root/UnitySandbox/Assets/BokumyakuPrototype/Simulation/*.cs"|Sort-Object Name
$source="using System;`nusing System.Collections.Generic;`nusing System.IO;`nusing System.Reflection;`n"+(($files|ForEach-Object{[IO.File]::ReadAllText($_.FullName)-replace '(?m)^using [^;]+;\s*',''})-join "`n")
$source+=@'
namespace Bokumyaku.Simulation {
 public static class ProductionProbe {
  public static RunSimulator Run(int seed, string weapon, string build, int[] levels) {
   var meta=new MetaProgress{Damage=levels[0],Supply=levels[1],Growth=levels[2],Harvest=levels[3]};
   var run=new RunSimulator(seed,meta);run.SetWeapon((WeaponKind)Enum.Parse(typeof(WeaponKind),weapon));run.SetBuild((BuildPreset)Enum.Parse(typeof(BuildPreset),build));
   while(run.State==RunStatus.Running)run.Tick();return run;
  }
  public static double MeanPressure(RunSimulator run){double sum=0;foreach(var branch in run.Branches)if(branch.IsTip)sum+=run.Pressure(branch);return sum/run.TipCount;}
 }
}
'@
$hashes=@($files|ForEach-Object{[ordered]@{path=$_.FullName.Substring($root.Length+1).Replace('\','/');sha256=(Get-FileHash $_.FullName -Algorithm SHA256).Hash}})
Add-Type -TypeDefinition $source
if([Bokumyaku.Simulation.RunSimulator]::CurrentRulesVersion -ne 4){throw 'Expected production rules 4'}
function Levels($value){if($value -is [string]){return (($value -split '\s+'|Where-Object{$_ -ne ''}) -join ',')};return ($value -join ',')}
$checks=[Collections.Generic.List[object]]::new()
$failures=[Collections.Generic.List[string]]::new()
$references=@('record-v8-candidate2.json','record-v8-fixed11to20.json','bokumyaku-pacing-v7-candidate-2.json','bokumyaku-pacing-v7-candidate-2-fixed31to50.json')
foreach($reference in $references){
 $data=Get-Content "$root/docs/evidence/$reference" -Raw|ConvertFrom-Json
 $isV8=$reference.StartsWith('record-v8')
 $rows=if($isV8){@($data.runs|Where-Object variant -eq PrioritySupply)}else{@($data.runs|Where-Object{$_.weapon -eq 'Point' -and $_.build -eq 'Farming'})}
 $expectedCount=if($isV8){240}elseif($reference.Contains('fixed')){20}else{30}
 if($rows.Count -ne $expectedCount){throw "Unexpected reference row count $reference $($rows.Count)"}
 $matched=0
 foreach($row in $rows){
  $meta=if($isV8){[int[]]($row.meta -split '/')}else{[int[]](0,0,0,0)}
  $run=[Bokumyaku.Simulation.ProductionProbe]::Run($row.seed,$row.weapon,$row.build,$meta)
  $actual=@{ticks=$run.TickCount;wave=$run.CompletedWave;crystals=$run.CrystalsReward;levels=(Levels $run.UpgradeLevels);status=$run.State.ToString();repairs=$run.RepairCount}
  if($isV8){$actual.nodes=$run.Branches.Count;$actual.tips=$run.TipCount;$actual.income=$run.IncomeEarned;$actual.ink=$run.RunInk;$actual.seconds=$run.ElapsedSeconds;$actual.rate=$run.CrystalsReward*60/($run.ElapsedSeconds+3);$actual.meanPressure=[Bokumyaku.Simulation.ProductionProbe]::MeanPressure($run)}
  $ok=$true
  foreach($key in $actual.Keys){
   $wanted=if($key -eq 'levels'){Levels $row.levels}else{$row.$key}
   $same=if($key -in @('seconds','rate','meanPressure')){[Math]::Abs([double]$actual[$key]-[double]$wanted) -le 1e-10}else{[string]$actual[$key] -ceq [string]$wanted}
   if(!$same){$ok=$false;$failures.Add("$reference seed=$($row.seed) $($row.meta) $($row.weapon) $($row.build) $key expected=$wanted actual=$($actual[$key])")}
  }
  if($ok){$matched++}
 }
 $checks.Add([ordered]@{reference=$reference;referenceSha256=(Get-FileHash "$root/docs/evidence/$reference" -Algorithm SHA256).Hash;cases=$rows.Count;matched=$matched;seeds=@($rows.seed|Sort-Object -Unique);scope=if($isV8){'2 meta x 6 weapons x 2 builds x 10 seeds; terminal status,ticks,wave,crystals,levels,repairs,nodes,tips,income,ink,seconds,rate,meanPressure'}else{'meta0 Farming Point; terminal ticks,wave,crystals,levels,status,repairs'}})
}
[ordered]@{classification='Standalone C# production replay; not Unity validation';rulesVersion=4;sourceHashFormat='Exact file bytes SHA256';sourceHashes=$hashes;checks=$checks;passed=($failures.Count -eq 0);failures=$failures;rateDefinition='normal crystals*60/(battle seconds+3); candidate reference metric';limitations=@('Terminal metrics equality, not full state equality','Source code compiled directly; no Unity runtime or device performance validation')}|ConvertTo-Json -Depth 9|Set-Content "$root/docs/evidence/record-v8-production.json" -Encoding utf8
if($failures.Count){throw "Production replay failed: $($failures.Count) metric mismatches"}
'PASS: 480 production candidate replays and 50 unchanged initial Farming Point runs'
