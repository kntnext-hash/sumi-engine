$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$files=Get-ChildItem "$root/UnitySandbox/Assets/BokumyakuPrototype/Simulation/*.cs"|Sort-Object Name
$hashes=@($files|ForEach-Object{[ordered]@{path=$_.Name;sha256=(Get-FileHash $_.FullName -Algorithm SHA256).Hash}})
$source="using System;`nusing System.Collections.Generic;`nusing System.IO;`nusing System.Reflection;`nusing System.Text.Json;`n"+(($files|ForEach-Object{[IO.File]::ReadAllText($_.FullName)-replace '(?m)^using [^;]+;\s*',''})-join "`n")
$source+=@'
namespace Bokumyaku.Simulation {
 public static class LongRunProbe {
  static readonly JsonSerializerOptions Options=new JsonSerializerOptions{IncludeFields=true};
  static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
  static string Json(RunSimulator run){return JsonSerializer.Serialize(run.CaptureState(),Options);}
  static RunSimulator Copy(RunSimulator run){return RunSimulator.Restore(JsonSerializer.Deserialize<RunSnapshot>(Json(run),Options));}
  public sealed class NaturalResult {
   public int seed,weapon,build,ticks,wave,completedWave,crystals,maxEnemies,maxFields,maxBranches;
   public string status;
   public bool finished;
  }
  public static NaturalResult Natural(int weapon,int build){
   var run=new RunSimulator(42,new MetaProgress{Damage=14,Supply=13,Growth=14,Harvest=12});
   run.SetWeapon((WeaponKind)weapon);run.SetBuild((BuildPreset)build);
   var result=new NaturalResult{seed=42,weapon=weapon,build=build};
   while(run.State==RunStatus.Running&&run.TickCount<200000){
    run.Tick();result.maxEnemies=Math.Max(result.maxEnemies,run.Enemies.Count);result.maxFields=Math.Max(result.maxFields,run.Fields.Count);result.maxBranches=Math.Max(result.maxBranches,run.Branches.Count);
   }
   result.ticks=run.TickCount;result.wave=run.Wave;result.completedWave=run.CompletedWave;result.crystals=run.CrystalsReward;result.status=run.State.ToString();result.finished=run.State!=RunStatus.Running;return result;
  }
  public static long[] IntegerBounds(){
   long regular=(long)Math.Ceiling(4.8*9*RunSimulator.LongRunWaveLimit)+1,boss=RunSimulator.LongRunWaveLimit/10;
   long ids=regular*3+boss,points=(regular*2+boss*12)*16,ink=(regular*2+boss*12)*64,crystals=(long)Math.Floor(points*.08),growth=regular+boss;
   Check(RunSimulator.RewardWaveFactor(100000)<10,"reward factor bound");
   Check(ids<RunSimulator.LongRunCounterLimit&&points<RunSimulator.LongRunRewardPointsLimit&&ink<RunSimulator.LongRunIncomeLimit&&crystals<RunSimulator.LongRunCrystalLimit&&growth<RunSimulator.LongRunGrowthChargeLimit,"conservative integer bounds");
   Check(ink<int.MaxValue&&RunSimulator.LongRunMaxTicks<int.MaxValue,"int32 bounds");
   return new[]{regular,boss,ids,points,ink,crystals,growth};
  }
  public static double[] Formulas(){
   double hp=0,spawn=0,reward=0,speed=0;
   for(int w=1;w<=100000;w++){
    double h=RunSimulator.BaseHp(w),s=RunSimulator.SpawnRate(w),r=RunSimulator.RewardWaveFactor(w),v=RunSimulator.EnemySpeedFactor(w);
    Check(!double.IsNaN(h+s+r+v)&&!double.IsInfinity(h+s+r+v),"finite wave "+w);
    Check(h>=hp&&s>=spawn&&r>=reward&&v>=speed,"monotonic wave "+w);
    Check(h*12<=1e12&&35*v*1.8<=157.5,"HP/speed cap wave "+w);
    if(w<=60){Check(h==16*Math.Pow(1.072,w-1)*(1+.12*Math.Floor((w-1)/10.0)),"legacy HP");Check(s==Math.Min(4.8,.50+.027*w+.0007*w*w),"legacy spawn");Check(r==1+.018*w,"legacy reward");Check(v==1+.006*(w-1),"legacy speed");}
    hp=h;spawn=s;reward=r;speed=v;
   }
   return new[]{hp*12,spawn,reward,35*speed*1.8};
  }
  public static string Boundary(int wave,int weapon){
   var original=new RunSimulator(42);original.SetWeapon((WeaponKind)weapon);var snapshot=original.CaptureState();
   snapshot.tickCount=(wave-1)*360;snapshot.wave=Math.Max(1,wave-1);snapshot.completedWave=wave-1;
   var a=RunSimulator.Restore(snapshot);var b=Copy(a);int ticks=0;
   while(ticks<900&&a.State==RunStatus.Running){a.Tick();b.Tick();Check(Json(a)==Json(b),"JSON future mismatch "+wave+"/"+weapon+"/"+ticks);b=Copy(b);ticks++;}
   Check(a.State!=RunStatus.Running,"Expected natural terminal within 900 ticks");
   return wave+"/"+weapon+":"+a.State+":"+ticks;
  }
  public static string FinalBoundary(){
   var snapshot=new RunSimulator(42).CaptureState();snapshot.tickCount=RunSimulator.TickLimitForRules(5)-1;snapshot.wave=100000;snapshot.completedWave=99999;
   var run=RunSimulator.Restore(snapshot);run.Tick();Check(run.State==RunStatus.TestCompleted&&run.CompletedWave==100000,"final boundary");string before=Json(run);run.Tick();Check(Json(run)==before,"terminal tick changed snapshot");return run.State.ToString();
  }
  public static int OldBoundary(string path){
   var snapshot=JsonSerializer.Deserialize<RunSnapshot>(File.ReadAllText(path),Options);snapshot.rulesVersion=5;
   var a=RunSimulator.Restore(snapshot);var b=Copy(a);
   for(int i=0;i<900;i++){a.Tick();b.Tick();Check(Json(a)==Json(b),"old boundary JSON future "+i);b=Copy(b);}
   Check(a.Wave>60,"new rules did not cross wave60");return a.Wave;
  }
 }
}
'@
Add-Type -TypeDefinition $source
if([Bokumyaku.Simulation.RunSimulator]::CurrentRulesVersion -ne 5){throw 'Expected rules5'}
$formula=[Bokumyaku.Simulation.LongRunProbe]::Formulas()
$bounds=[Bokumyaku.Simulation.LongRunProbe]::IntegerBounds()
$boundaries=@(foreach($wave in @(1000,10000,30000,99999,100000)){foreach($weapon in 0..5){[Bokumyaku.Simulation.LongRunProbe]::Boundary($wave,$weapon)}})
$final=[Bokumyaku.Simulation.LongRunProbe]::FinalBoundary()
$cross=[Bokumyaku.Simulation.LongRunProbe]::OldBoundary("$root/UnitySandbox/Assets/BokumyakuPrototype/Tests/Fixtures/v8-enso-tick21599.json")
[ordered]@{classification='Standalone C# injected boundary tests; NOT naturally reached high waves or Unity verification';passed=$true;rulesVersion=5;sourceHashes=$hashes;formulaWaves=100000;legacyExactFormulaWaves=60;maxBossHp=$formula[0];maxSpawnRate=$formula[1];maxRewardFactor=$formula[2];maxFastSpeed=$formula[3];integerBounds=[ordered]@{regularSpawns=$bounds[0];bossSpawns=$bounds[1];enemyIds=$bounds[2];rewardPoints=$bounds[3];ink=$bounds[4];crystals=$bounds[5];growthCharge=$bounds[6];assumptions='All regular spawns may split; regular yield<=2, boss12, Harvest16, metaHarvest20, rewardFactor<10; children give no income/reward/growth'};boundaries=$boundaries;finalBoundary=$final;old60BoundaryAfter900TicksWave=$cross;jsonValidation='Every simulated tick compare future against JSON restored copy; exactDoubleBits retained'}|ConvertTo-Json -Depth 7|Set-Content "$root/docs/evidence/longrun-v9-independent.json" -Encoding utf8
'PASS: full formula domain and injected JSON continuation boundaries'
$natural=@(foreach($weapon in 0..5){foreach($build in 1..2){[Bokumyaku.Simulation.LongRunProbe]::Natural($weapon,$build)}})
$report=Get-Content "$root/docs/evidence/longrun-v9-independent.json" -Raw|ConvertFrom-Json
$report|Add-Member -NotePropertyName naturalProgression -NotePropertyValue ([ordered]@{classification='Small natural progression sample, not balance acceptance or proof of reaching 100000 waves';seed=42;meta='14/13/14/12';tickBudgetPerRun=200000;weaponOrder=@('Point','Pierce','Bleed','Sweep','Enso','Resonance');buildIds=@{Farming=1;Record=2};runs=$natural;finished=@($natural|Where-Object finished).Count;exceededWave60=@($natural|Where-Object{$_.wave -gt 60}).Count})
$report|ConvertTo-Json -Depth 9|Set-Content "$root/docs/evidence/longrun-v9-independent.json" -Encoding utf8
'Natural progression recorded (unfinished cases remain explicitly marked)'
