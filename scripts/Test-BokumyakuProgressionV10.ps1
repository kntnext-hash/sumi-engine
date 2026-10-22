$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$files=Get-ChildItem "$root/UnitySandbox/Assets/BokumyakuPrototype/Simulation/*.cs"|Sort-Object Name
$hashes=@($files|ForEach-Object{[ordered]@{path=$_.Name;sha256=(Get-FileHash $_.FullName -Algorithm SHA256).Hash}})
$source="using System;`nusing System.Collections.Generic;`nusing System.IO;`nusing System.Reflection;`nusing System.Text.Json;`n"+(($files|ForEach-Object{[IO.File]::ReadAllText($_.FullName)-replace '(?m)^using [^;]+;\s*',''})-join "`n")
$source+=@'
namespace Bokumyaku.Simulation {
 public static class ProgressionProbe {
  static readonly JsonSerializerOptions Options=new JsonSerializerOptions{IncludeFields=true};
  static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
  static string Json(RunSimulator run){return JsonSerializer.Serialize(run.CaptureState(),Options);}
  static RunSimulator Copy(RunSimulator run){return RunSimulator.Restore(JsonSerializer.Deserialize<RunSnapshot>(Json(run),Options));}
  static void Reject(RunSnapshot snapshot,string label){bool rejected=false;try{RunSimulator.Restore(snapshot);}catch(ArgumentException){rejected=true;}Check(rejected,label);}
  public sealed class FormulaRow { public int level,metaCost,runCost;public double metaMultiplier,observedRunMultiplier; }
  static double FirstHit(int level){
   var state=new RunSimulator(42).CaptureState();state.levels[0]=level;var run=RunSimulator.Restore(state);
   for(int tick=0;tick<900;tick++){run.Tick();if(run.Attacks.Count>0)return run.Attacks[0].Damage;}
   throw new Exception("No first attack");
  }
  public static FormulaRow[] Formulas(){
   var rows=new List<FormulaRow>();double baseHit=FirstHit(0),lastMeta=0,lastRun=0;int lastMetaCost=0,lastRunCost=0;
   Check(MetaProgress.MaxLevel(MetaUpgrade.Damage)==100,"metaDamage cap");
   foreach(var kind in new[]{MetaUpgrade.Supply,MetaUpgrade.Growth,MetaUpgrade.Harvest}){
    Check(MetaProgress.MaxLevel(kind)==20,"other meta cap");
    for(int level=0;level<=20;level++){
     var m=new MetaProgress();if(kind==MetaUpgrade.Supply)m.Supply=level;else if(kind==MetaUpgrade.Growth)m.Growth=level;else m.Harvest=level;
     int baseCost=kind==MetaUpgrade.Supply?10:kind==MetaUpgrade.Harvest?12:8;
     Check(m.Cost(kind)==(int)Math.Ceiling(baseCost*Math.Pow(1.48,level)),"other meta legacy cost");
    }
   }
   for(int level=0;level<=100;level++){
    var meta=new MetaProgress{Damage=level};var state=new RunSimulator(42).CaptureState();state.levels[0]=level;var run=RunSimulator.Restore(state);
    Check(run.UpgradeMaxLevel(UpgradeKind.Damage)==100,"runDamage cap");
    double multiplier=FirstHit(level)/baseHit;int mc=meta.Cost(MetaUpgrade.Damage),rc=run.UpgradeCost(UpgradeKind.Damage);
    Check(mc>0&&rc>0&&mc>=lastMetaCost&&rc>=lastRunCost,"cost finite monotonic");
    Check(!double.IsInfinity(meta.DamageMultiplier+multiplier)&&!double.IsNaN(meta.DamageMultiplier+multiplier)&&meta.DamageMultiplier>=lastMeta&&multiplier>=lastRun,"multiplier finite monotonic");
    if(level<=20){Check(mc==(int)Math.Ceiling(8*Math.Pow(1.48,level)),"legacy meta cost");Check(meta.DamageMultiplier==1+.025*level,"legacy meta multiplier");}
    if(level<=30){Check(rc==(int)Math.Ceiling(10*Math.Pow(1.16,level)*(1+.008*level*level)),"legacy run cost");Check(Math.Abs(multiplier-Math.Pow(1.1,level))<1e-10,"legacy observed run multiplier");}
    rows.Add(new FormulaRow{level=level,metaCost=mc,runCost=rc,metaMultiplier=meta.DamageMultiplier,observedRunMultiplier=multiplier});lastMetaCost=mc;lastRunCost=rc;lastMeta=meta.DamageMultiplier;lastRun=multiplier;
   }
   return rows.ToArray();
  }
  public static long[] ResourceBounds(){
   long regular=(long)Math.Ceiling(4.8*9*RunSimulator.LongRunWaveLimit)+1,boss=RunSimulator.LongRunWaveLimit/10;
   long ids=regular*3+boss,points=(regular*2+boss*12)*16,ink=(regular*2+boss*12)*64,crystals=(long)Math.Floor(points*.08),growth=regular+boss;
   Check(RunSimulator.RewardWaveFactor(100000)<10,"reward factor bound");
   Check(ids<RunSimulator.LongRunCounterLimit&&points<RunSimulator.LongRunRewardPointsLimit&&ink<RunSimulator.LongRunIncomeLimit&&crystals<RunSimulator.LongRunCrystalLimit&&growth<RunSimulator.LongRunGrowthChargeLimit,"resource bounds");
   Check(ink<int.MaxValue&&RunSimulator.LongRunMaxTicks<int.MaxValue,"int32 resource bounds");
   return new[]{ids,points,ink,crystals,growth};
  }
  public static double[] DamageBound(){
   double baseDamage=RunSimulator.BaseAttackDamage*Math.Pow(1.10,30)*Math.Pow(1.12,70)*new MetaProgress{Damage=100}.DamageMultiplier;
   double hit=baseDamage*1.65,field=hit*.85*(1+.12*10)/RunSimulator.TickRate;
   double bound=Math.Ceiling(RunSimulator.LongRunMaxTicks/7.0)*12*96*hit+RunSimulator.LongRunMaxTicks*96.0*2*field;
   Check(bound<RunSimulator.LongRunEffectiveDamageLimit,"effective damage conservative bound");return new[]{hit,field,bound};
  }
  public static string[] Boundary(){
   var done=new List<string>();
   var old=new RunSimulator(42).CaptureState();old.rulesVersion=5;old.levels[0]=31;Reject(old,"rules5 runDamage31");done.Add("rules5 runDamage31 rejected");
   old=new RunSimulator(42).CaptureState();old.rulesVersion=5;old.meta.Damage=21;Reject(old,"rules5 metaDamage21");done.Add("rules5 metaDamage21 rejected");
   foreach(int meta in new[]{20,21,100})foreach(int damage in new[]{30,31,100}){
    var state=new RunSimulator(42,new MetaProgress{Damage=meta,Supply=20,Growth=20,Harvest=20}).CaptureState();state.levels[0]=damage;
    var a=RunSimulator.Restore(state);var b=Copy(a);
    for(int tick=0;tick<900;tick++){a.Tick();b.Tick();Check(Json(a)==Json(b),"JSON future "+meta+"/"+damage+"/"+tick);b=Copy(b);}
    done.Add("meta"+meta+"/run"+damage+":900 exact JSON future ticks");
   }
   return done.ToArray();
  }
  public sealed class Ledger {
   public int seed,build,weapon,startingDamage,endingDamage,startingCrystals,earned,spent,endingCrystals,ticks,wave,completedWave,nextCost,purchases;
   public string status;
   public bool observationLimit,settled;
   public double seconds,normalCrystalsPerHour;
  }
  public static Ledger[] PaidSeries(int build){
   var rows=new List<Ledger>();var meta=new MetaProgress{Damage=20,Supply=20,Growth=20,Harvest=20};
   for(int seed=1;seed<=10;seed++){
    var row=new Ledger{seed=seed,build=build,weapon=(int)WeaponKind.Enso,startingDamage=meta.Damage,startingCrystals=meta.Crystals};
    var run=new RunSimulator(seed,meta);run.SetBuild((BuildPreset)build);run.SetWeapon(WeaponKind.Enso);
    while(run.State==RunStatus.Running&&run.TickCount<2000*360)run.Tick();
    row.observationLimit=run.State==RunStatus.Running;row.status=run.State.ToString();row.ticks=run.TickCount;row.wave=run.Wave;row.completedWave=run.CompletedWave;row.seconds=run.ElapsedSeconds;
    if(!row.observationLimit){row.settled=run.ClaimReward(meta);row.earned=meta.Crystals-row.startingCrystals;Check(row.settled||run.State==RunStatus.CapacityExceeded,"terminal reward claim");}
    while(!row.observationLimit&&meta.Damage<MetaProgress.MaxLevel(MetaUpgrade.Damage)&&meta.Crystals>=meta.Cost(MetaUpgrade.Damage)){
     int cost=meta.Cost(MetaUpgrade.Damage);Check(meta.TryBuy(MetaUpgrade.Damage),"paid research");row.spent+=cost;row.purchases++;
    }
    row.endingDamage=meta.Damage;row.endingCrystals=meta.Crystals;row.nextCost=meta.Cost(MetaUpgrade.Damage);row.normalCrystalsPerHour=row.seconds>0?row.earned*3600.0/row.seconds:0;
    Check(row.startingCrystals+row.earned-row.spent==row.endingCrystals,"ledger conservation");
    rows.Add(row);if(row.observationLimit)break;
   }
   return rows.ToArray();
  }
 }
}
'@
# Execute only after the production Core freeze has been confirmed.
Add-Type -TypeDefinition $source
if([Bokumyaku.Simulation.RunSimulator]::CurrentRulesVersion -ne 6){throw 'Expected production rules6'}
$boundary=[Bokumyaku.Simulation.ProgressionProbe]::Boundary()
$formulas=[Bokumyaku.Simulation.ProgressionProbe]::Formulas()
$resourceBounds=[Bokumyaku.Simulation.ProgressionProbe]::ResourceBounds()
$damageBound=[Bokumyaku.Simulation.ProgressionProbe]::DamageBound()
$series=@([Bokumyaku.Simulation.ProgressionProbe]::PaidSeries(1))+@([Bokumyaku.Simulation.ProgressionProbe]::PaidSeries(2))
[ordered]@{classification='Standalone C# paid progression and synthetic boundary verification; not Unity or product balance acceptance';passed=$true;rulesVersion=6;sourceHashes=$hashes;boundaries=$boundary;resourceBounds=[ordered]@{enemyIds=$resourceBounds[0];rewardPoints=$resourceBounds[1];ink=$resourceBounds[2];crystals=$resourceBounds[3];growthCharge=$resourceBounds[4];maxHit=$damageBound[0];maxFieldPerTick=$damageBound[1];effectiveDamage=$damageBound[2];effectiveDamageAssumption='ceil(36m/7)*12tips*96targets*maxHit +36m*96enemies*2fields*maxFieldPerTick'};formulaDomain=$formulas;formulaNotes='Run multiplier measured through first Point AttackEvent relative to level0 with same seed/pressure; legacy numerical ratio tolerance1e-10. Meta0..20 and run0..30 cost formulas exact.';paidProgression=[ordered]@{initialMeta='20/20/20/20';initialCrystals=0;weapon='Enso';separateBuildSeries=@('Farming','Record');maximumRunsEach=10;observationTickLimit=720000;policy='Only naturally terminal runs settle reward; running at 2000Wave observation limit receives none. Buy Damage only using actual balance; no milestone bonuses.';rate='Earned normal crystals per battle hour, excludes restart wait';ledger=$series}}|ConvertTo-Json -Depth 9|Set-Content "$root/docs/evidence/progression-v10-independent.json" -Encoding utf8
'Independent progression report written'
