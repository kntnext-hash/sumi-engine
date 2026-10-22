param([int]$Candidate=-1,[switch]$FixedValidation,[switch]$VerifyCurrentRules,[switch]$VerifyAdopted)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$files=Get-ChildItem "$root/UnitySandbox/Assets/BokumyakuPrototype/Simulation/*.cs"|Sort-Object Name
$source="using System;`nusing System.Collections.Generic;`nusing System.IO;`nusing System.Reflection;`n"+(($files|ForEach-Object{[IO.File]::ReadAllText($_.FullName)-replace '(?m)^using [^;]+;\s*',''})-join "`n")
if($VerifyAdopted){
 $runner=@'
namespace Bokumyaku.AdoptionCheck {public static class Run {public static Simulation.RunSimulator Measure(int seed,string weapon,string build){var r=new Simulation.RunSimulator(seed);r.SetWeapon((Simulation.WeaponKind)Enum.Parse(typeof(Simulation.WeaponKind),weapon));r.SetBuild((Simulation.BuildPreset)Enum.Parse(typeof(Simulation.BuildPreset),build));while(r.State==Simulation.RunStatus.Running)r.Tick();return r;}}}
'@
 Add-Type -TypeDefinition ($source+$runner)
 $paths=@("$root/docs/evidence/bokumyaku-pacing-v7-candidate-2.json","$root/docs/evidence/bokumyaku-pacing-v7-candidate-2-fixed31to50.json")
 $count=0
 foreach($path in $paths){$expected=Get-Content $path -Raw|ConvertFrom-Json;foreach($e in $expected.runs){$r=[Bokumyaku.AdoptionCheck.Run]::Measure($e.seed,$e.weapon,$e.build);if($r.RulesVersion -ne 3 -or $r.TickCount -ne $e.ticks -or $r.CompletedWave -ne $e.wave -or $r.CrystalsReward -ne $e.crystals -or $r.RepairCount -ne $e.repairs -or $r.State.ToString() -ne $e.status -or (($r.UpgradeLevels -join ',') -ne ($e.levels -join ','))){throw "Adoption mismatch seed$($e.seed) $($e.weapon)/$($e.build)"};$count++}}
 [ordered]@{classification='Actual production rules3 against fixed candidateC terminal records';pass=$true;matchedRuns=$count;coverage='All50 Point Farming plus ten seeds/six weapons/both builds (Point overlaps)';comparedFields=@('ticks','completedWave','crystals','repairCount','status','all7upgradeLevels');sourceHashes=@($files|ForEach-Object{[ordered]@{name=$_.Name;sha256=(Get-FileHash $_.FullName).Hash}});evidenceHashes=@($paths|ForEach-Object{[ordered]@{name=[IO.Path]::GetFileName($_);sha256=(Get-FileHash $_).Hash}})}|ConvertTo-Json -Depth 8|Set-Content "$root/docs/evidence/bokumyaku-pacing-v7-adoption-check.json" -Encoding utf8
 "PASS production rules3 matches $count candidateC runs";return
}
if($VerifyCurrentRules){
 Add-Type -TypeDefinition $source
 $options=[System.Text.Json.JsonSerializerOptions]::new();$options.IncludeFields=$true
 function Serialize($r){[System.Text.Json.JsonSerializer]::Serialize($r.CaptureState(),$options)}
 function LoadFixture($name){$text=[IO.File]::ReadAllText("$root/UnitySandbox/Assets/BokumyakuPrototype/Tests/Fixtures/$name.json");[Bokumyaku.Simulation.RunSimulator]::Restore([System.Text.Json.JsonSerializer]::Deserialize($text,[Bokumyaku.Simulation.RunSnapshot],$options))}
 foreach($prefix in @('v6-run','v6-record')){
  $r=LoadFixture "$prefix-tick6000";for($i=0;$i -lt 900;$i++){$r.Tick()};$e=LoadFixture "$prefix-tick6900";if((Serialize $r) -cne (Serialize $e)){throw "$prefix tick6900 mismatch"};while($r.State -eq 'Running'){$r.Tick()};$e=LoadFixture "$prefix-terminal";if((Serialize $r) -cne (Serialize $e)){throw "$prefix terminal mismatch"}
 }
 foreach($build in @(1,2)){foreach($weapon in 0..5){$a=[Bokumyaku.Simulation.RunSimulator]::new(42);$a.SetBuild($build);$a.SetWeapon($weapon);for($i=0;$i -lt 6000;$i++){$a.Tick()};$b=[Bokumyaku.Simulation.RunSimulator]::Restore([System.Text.Json.JsonSerializer]::Deserialize((Serialize $a),[Bokumyaku.Simulation.RunSnapshot],$options));for($i=0;$i -lt 900;$i++){$a.Tick();$b.Tick();if((Serialize $a) -cne (Serialize $b)){throw 'Rules3 JSON continuation mismatch'}}}}
 [ordered]@{classification='Standalone C# System.Text.Json, not Unity JsonUtility';rulesVersion=3;legacyV6TickAndTerminalCases=2;newRules900TickEveryTickCases=12;pass=$true}|ConvertTo-Json|Set-Content "$root/docs/evidence/bokumyaku-pacing-v7-rules-checks.json" -Encoding utf8
 'PASS legacy v6 exact future/terminal and12 rules3 JSON continuation cases';return
}
# Candidate experiments remain anchored to pre-intervention rules2 after production adopts rules3.
$baselineCommit=(& git -C $root rev-parse --verify '397b666^{commit}').Trim()
if($LASTEXITCODE -ne 0){throw 'Missing immutable v6 baseline'}
$baselineParts=@();$baselineHashes=@()
foreach($file in $files){$path="UnitySandbox/Assets/BokumyakuPrototype/Simulation/$($file.Name)";$lines=@(& git -C $root show "${baselineCommit}:$path");if($LASTEXITCODE -ne 0){throw "Missing baseline blob $path"};$text=($lines -join "`n")+"`n";$sha=[Security.Cryptography.SHA256]::Create();try{$hash=([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($text)))).Replace('-','')}finally{$sha.Dispose()};$baselineHashes += [ordered]@{name=$file.Name;sha256=$hash};$baselineParts += ($text -replace '(?m)^using [^;]+;\s*','')}
$source="using System;`nusing System.Collections.Generic;`nusing System.IO;`nusing System.Reflection;`n"+($baselineParts -join "`n")
if($Candidate -ge 0){
 if($Candidate -gt 2){throw 'Only three candidates are permitted'}
 $names=@('A_PaidRepair2','B_EarlyBossRamp','C_SplitAngle65')
 if($Candidate -eq 0){$source=$source.Replace('int harvestTarget =','if (CoreHp <= 6 && repairCount < 2) { TryRepair(); return; } int harvestTarget =')}
 if($Candidate -eq 1){$source=$source.Replace('kind == EnemyKind.Boss ? 12 : kind == EnemyKind.Splitter','kind == EnemyKind.Boss ? (Wave == 10 ? 8 : Wave == 20 ? 10 : 12) : kind == EnemyKind.Splitter')}
 if($Candidate -eq 2){$source=$source.Replace('split ? -.35 :','split ? -.65 :').Replace('angle + .35, length','angle + .65, length')}
 $measurement=@'
namespace Bokumyaku.PacingCandidates {
 public class Row {public int seed,wave,crystals,repairs,ticks;public double seconds,rate;public string weapon,build,status;public int[] levels;}
 public static class Measure {public static Row Run(int seed,int weapon,int build){var r=new Simulation.RunSimulator(seed);r.SetWeapon((Simulation.WeaponKind)weapon);r.SetBuild((Simulation.BuildPreset)build);while(r.State==Simulation.RunStatus.Running)r.Tick();return new Row{seed=seed,wave=r.CompletedWave,crystals=r.CrystalsReward,repairs=r.RepairCount,ticks=r.TickCount,seconds=r.ElapsedSeconds,rate=r.CrystalsReward*60/(r.ElapsedSeconds+3),weapon=r.Weapon.ToString(),build=r.Build.ToString(),status=r.State.ToString(),levels=r.UpgradeLevels};}}
}
'@
 Add-Type -TypeDefinition ($source+$measurement)
 $seedRange=if($FixedValidation){31..50}else{1..30}; $rows=@(foreach($seed in $seedRange){foreach($weapon in 0..5){if($seed -gt 10 -and $weapon -ne 0){continue};foreach($build in @(1,2)){if($seed -gt 10 -and $build -ne 1){continue};[Bokumyaku.PacingCandidates.Measure]::Run($seed,$weapon,$build)}}})
 $pairs=@(foreach($weapon in @('Point','Pierce','Bleed','Sweep','Enso','Resonance')){$dw=@();$dr=@();foreach($seed in 1..10){$f=$rows|Where-Object{$_.seed -eq $seed -and $_.weapon -eq $weapon -and $_.build -eq 'Farming'};$r=$rows|Where-Object{$_.seed -eq $seed -and $_.weapon -eq $weapon -and $_.build -eq 'Record'};$dw+=($r.wave-$f.wave);$dr+=($f.rate-$r.rate)};[ordered]@{weapon=$weapon;recordDepthWins=@($dw|Where-Object{$_ -gt 0}).Count;farmingRateWins=@($dr|Where-Object{$_ -gt 0}).Count;meanWaveDifference=($dw|Measure-Object -Average).Average;meanRateDifference=($dr|Measure-Object -Average).Average}})
 if($FixedValidation){$pairs=@()}; $point=@($rows|Where-Object{$_.weapon -eq 'Point' -and $_.build -eq 'Farming'})
 $report=[ordered]@{candidate=$names[$Candidate];classification='Standalone in-memory source intervention, production files unchanged';baselineCommit=$baselineCommit;sourceHashFormat='UTF8 LF one final LF';sourceHashes=$baselineHashes;rules=@('A: Farming only, HP<=6 paid repair priority at normal30tick purchase boundaries, cap2, waits for cost','B: All builds Boss HP8x atWave10,10x atWave20,12x later','C: All builds split angular offsets +/-0.65 instead of0.35; no other growth change');pointWithin300to480=@($point|Where-Object{$_.seconds -ge 300 -and $_.seconds -le 480}).Count;pointMinSeconds=($point|Measure-Object seconds -Minimum).Minimum;pointMaxSeconds=($point|Measure-Object seconds -Maximum).Maximum;pairs=$pairs;runs=$rows}
 $report['postSelectionValidation']=$FixedValidation.IsPresent; $suffix=if($FixedValidation){"-fixed31to50"}else{""}; $report|ConvertTo-Json -Depth 12|Set-Content "$root/docs/evidence/bokumyaku-pacing-v7-candidate-$Candidate$suffix.json" -Encoding utf8
 $pairs|ForEach-Object{[pscustomobject]$_}|Format-Table
 "$($names[$Candidate]): G05 $($report.pointWithin300to480)/$($point.Count), range $($report.pointMinSeconds)..$($report.pointMaxSeconds)"
 return
}
$runner=@'
namespace Bokumyaku.PacingV7 {
 public class Tip {public int id;public double x,y,angle,pressure,path;}
 public class State {public int tick,wave,hp,enemies,ink,kills,tips;public int[] levels;public double supply,maxTipAngleGap;public Tip[] branches;}
 public class Lifetime {public int id,firstSeen,firstAttack=-1,attacks,inRangeTicks,coveredTipTicks,maxCoveringTips;public double spawnAngle;}
 public class Contact {public int tick,wave,id,spawnWave,damage,hpBefore,hpAfter;public string kind;public bool child;public double angle,x,y,hpRemaining,maxHp,nearestTipDistance,nearestTipAngle;public Lifetime lifetime;public State state;}
 public class Boss {public int id,endTick;public string outcome;public Lifetime lifetime;public State firstState;}
 public class Run {public int seed,ticks,firstHit,completedWave,crystals;public double seconds;public string status;public int[] leaked;public List<Boss> bosses=new List<Boss>();public List<State> waves=new List<State>();public List<Contact> contacts=new List<Contact>();}
 public static class Diagnose {
  static double Angle(double x,double y){double a=Math.Atan2(y,x)*180/Math.PI;return a<0?a+360:a;}
  static State Sample(Simulation.RunSimulator r){var tips=new List<Tip>();var angles=new List<double>();foreach(var b in r.Branches)if(b.IsTip){double a=Angle(b.X,b.Y);angles.Add(a);tips.Add(new Tip{id=b.Id,x=b.X,y=b.Y,angle=a,pressure=r.Pressure(b),path=b.PathLength});}angles.Sort();double gap=angles[0]+360-angles[angles.Count-1];for(int i=1;i<angles.Count;i++)gap=Math.Max(gap,angles[i]-angles[i-1]);return new State{tick=r.TickCount,wave=r.Wave,hp=r.CoreHp,enemies=r.Enemies.Count,ink=r.RunInk,kills=r.Kills,tips=r.TipCount,levels=r.UpgradeLevels,supply=r.RootSupply,maxTipAngleGap=gap,branches=tips.ToArray()};}
  public static Run Measure(int seed){var r=new Simulation.RunSimulator(seed);r.SetBuild(Simulation.BuildPreset.Farming);var result=new Run{seed=seed,firstHit=-1};var life=new Dictionary<int,Lifetime>();
   while(r.State==Simulation.RunStatus.Running){
    var pending=new List<Contact>();var before=Sample(r);double range=Simulation.RunSimulator.BaseAttackRange+4*r.UpgradeLevels[4];
    foreach(var e in r.Enemies){Lifetime l;if(!life.TryGetValue(e.Id,out l)){l=new Lifetime{id=e.Id,firstSeen=r.TickCount,spawnAngle=Angle(e.X,e.Y)};life.Add(e.Id,l);if(e.Kind==Simulation.EnemyKind.Boss)result.bosses.Add(new Boss{id=e.Id,lifetime=l,firstState=before});}int covering=0;double nearest=double.MaxValue,angular=180;foreach(var b in r.Branches)if(b.IsTip){double dx=b.X-e.X,dy=b.Y-e.Y;nearest=Math.Min(nearest,Math.Sqrt(dx*dx+dy*dy));double ad=Math.Abs(Angle(e.X,e.Y)-Angle(b.X,b.Y));angular=Math.Min(angular,Math.Min(ad,360-ad));if(dx*dx+dy*dy<=range*range)covering++;}if(covering>0)l.inRangeTicks++;l.coveredTipTicks+=covering;l.maxCoveringTips=Math.Max(l.maxCoveringTips,covering);
     if(Math.Sqrt(e.X*e.X+e.Y*e.Y)<=12+e.Speed/30)pending.Add(new Contact{tick=r.TickCount+1,id=e.Id,spawnWave=e.SpawnWave,kind=e.Kind.ToString(),child=e.IsSplitChild,damage=e.Kind==Simulation.EnemyKind.Boss?4:e.Kind==Simulation.EnemyKind.Heavy?2:1,nearestTipDistance=nearest,nearestTipAngle=angular,angle=Angle(e.X,e.Y),x=e.X,y=e.Y,hpRemaining=e.Hp,maxHp=e.MaxHp,lifetime=l,state=before,hpBefore=r.CoreHp});
    }
    int oldLeaks=r.Leaks;r.Tick();if(r.Leaks-oldLeaks!=pending.Count)throw new Exception("Contact observation mismatch");
    foreach(var boss in result.bosses)if(boss.endTick==0){bool alive=false,leaked=false;foreach(var e in r.Enemies)if(e.Id==boss.id)alive=true;foreach(var c in pending)if(c.id==boss.id)leaked=true;if(!alive){boss.endTick=r.TickCount;boss.outcome=leaked?"leaked":"killed";}}
    foreach(var c in pending){c.wave=r.Wave;c.hpAfter=r.CoreHp;result.contacts.Add(c);if(result.firstHit<0)result.firstHit=r.TickCount;}
    foreach(var a in r.Attacks){Lifetime l;if(life.TryGetValue(a.EnemyId,out l)){l.attacks++;if(l.firstAttack<0)l.firstAttack=r.TickCount;}}
    if((r.TickCount-1)%360==0 || r.State!=Simulation.RunStatus.Running)result.waves.Add(Sample(r));
   }
   result.ticks=r.TickCount;result.seconds=r.ElapsedSeconds;result.completedWave=r.CompletedWave;result.crystals=r.CrystalsReward;result.status=r.State.ToString();result.leaked=r.LeakedByType;return result;
  }
 }
}
'@
Add-Type -TypeDefinition ($source+$runner)
$runs=@(foreach($seed in 1..30){[Bokumyaku.PacingV7.Diagnose]::Measure($seed)})
$report=[ordered]@{classification='Standalone C# observational trace; no combat or purchase changes';seedRange='1..30';build='Farming';weapon='Point';policy='Balanced';metaLevels=@(0,0,0,0);baselineCommit=$baselineCommit;sourceHashFormat='UTF8 LF one final LF';sourceHashes=$baselineHashes;observationNotes=@('Contact prediction uses existing enemy position <=12+speed/30 before Tick and is asserted against actual leak-count delta every tick. Spawned enemies start at360 and cannot contact in the spawn tick.','Contact state is before Tick and its scheduled purchase; hpAfter is actual end of movement phase shared by simultaneous contacts.','Lifetime firstSeen is one sampled tick after spawn; inRangeTicks measures geometric coverage, not weapon readiness or target selection.','Attack count tracks direct AttackEvent only, excludes field damage. hpRemaining captures all actual damage.','No candidate tuning or causal intervention performed.');runs=$runs}
$report|ConvertTo-Json -Depth 16|Set-Content "$root/docs/evidence/bokumyaku-pacing-v7-traces.json" -Encoding utf8
$runs|Select-Object seed,seconds,completedWave,firstHit,@{n='causes';e={($_.contacts|Group-Object kind|ForEach-Object{"$($_.Name):$($_.Count)"}) -join ','}}|Format-Table

$bossRows=@($runs|ForEach-Object{$b=$_.bosses[0];[pscustomobject]@{seed=$_.seed;outcome=$b.outcome;coveredTipSeconds=$b.lifetime.coveredTipTicks/30;attacks=$b.lifetime.attacks;meanPressure=($b.firstState.branches.pressure|Measure-Object -Average).Average;maxAngleGap=$b.firstState.maxTipAngleGap}})
$analysis=[ordered]@{classification='Observational diagnosis, hypotheses not causal proof';shortSeeds=@(4,20,25,28);firstBossGroups=@($bossRows|Group-Object outcome|ForEach-Object{[ordered]@{outcome=$_.Name;count=$_.Count;meanCoveredTipSeconds=($_.Group.coveredTipSeconds|Measure-Object -Average).Average;meanAttacks=($_.Group.attacks|Measure-Object -Average).Average;meanPressure=($_.Group.meanPressure|Measure-Object -Average).Average;meanMaxAngleGap=($_.Group.maxAngleGap|Measure-Object -Average).Average}});findings=@('All four short runs lose4HP to first Boss at tick3755 (125.1667sec). Seed25 then loses6HP to three Heavy by166.5sec, before Harvest unlock.','Seeds4/20/28 die around244-245sec after second Boss and Heavy contacts. Fixed arrival times explain clusters; there is no arbitrary death timer.','Bosses that leak have less integrated multi-tip coverage and fewer hits, while starting mean pressure is nearly equal. Largest tip gap alone does not distinguish outcomes.','Farming branches early: by first Boss contact SupplyLv2, DamageLv3, SpeedLv2,6-7tips. Pressure dilution is shared across seeds, not enough alone to explain directional variation.','All leaking first Bosses had geometric exposure, so a fully blind no-range gap is not supported. Directional overlap, cooldown allocation and earlier-ETA competing targets jointly limit damage.','Wave15 Harvest cannot explain seed25 failure and earlier boss/Heavy damage in other short runs.');boundedCandidateProposals=@([ordered]@{id='A';change='Split angular offsets ±.35 to ±.65, shared growth geometry';hypothesis='Spread siblings and increase overlapping directional coverage away from initial three lobes';risks='Can reduce coverage density on old lanes or extend strong seeds beyond480sec; no success claim'},[ordered]@{id='B';change='When adding branches at radius limit, choose currently least-covered angular sector with deterministic tie break';hypothesis='Address spatial concentration directly rather than using largest gap as an outcome predictor';risks='Requires explicit geometry/path validation and rules version isolation; may alter weapon compatibility'},[ordered]@{id='C';change='Delay next Farming branch until Supply>=2*nextBranchLevel, keeping all prices and manual actions common';hypothesis='Test pressure dilution versus geographic coverage with fewer stronger tips';risks='May weaken multi-lane defense; not combine with A/B in first causal comparison'});constraints=@('Do not alter production source until parent approval','At most3 candidate schedules/geometries; no special seed/timer rules','Keep old rules saves on old behavior, new behavior versioned if adopted','Evaluate all30 seeds plus role split; fixing short cases alone is not acceptance');traceFile='docs/evidence/bokumyaku-pacing-v7-traces.json';traceSha256=(Get-FileHash "$root/docs/evidence/bokumyaku-pacing-v7-traces.json").Hash;firstBossRows=$bossRows}
$analysis|ConvertTo-Json -Depth 10|Set-Content "$root/docs/evidence/bokumyaku-pacing-v7-diagnosis.json" -Encoding utf8
