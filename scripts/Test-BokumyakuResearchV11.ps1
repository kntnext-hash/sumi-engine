param([string]$BaselineRef='94606b8')
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$commit=(& git -C $root rev-parse --verify "$BaselineRef^{commit}").Trim()
if($LASTEXITCODE -ne 0){throw 'Baseline commit unavailable'}
function HashText([string]$text){$sha=[Security.Cryptography.SHA256]::Create();try{([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($text)))).Replace('-','')}finally{$sha.Dispose()}}
$oldPaths=@(& git -C $root ls-tree -r --name-only $commit -- UnitySandbox/Assets/BokumyakuPrototype/Simulation)|Where-Object{$_ -like '*.cs'}|Sort-Object
if($LASTEXITCODE -ne 0){throw 'Baseline file list failed'}
$oldParts=@();$oldHashes=@()
foreach($path in $oldPaths){$lines=@(& git -C $root show "${commit}:$path");if($LASTEXITCODE -ne 0){throw "Baseline read failed: $path"};$text=($lines -join "`n")+"`n";$oldHashes += [ordered]@{path=$path;sha256=HashText $text};$oldParts+=($text -replace '(?m)^using [^;]+;\s*','')}
$files=Get-ChildItem "$root/UnitySandbox/Assets/BokumyakuPrototype/Simulation/*.cs"|Sort-Object Name
$hashes=@($files|ForEach-Object{[ordered]@{path=$_.Name;sha256=(Get-FileHash $_.FullName -Algorithm SHA256).Hash}})
$source="using System;`nusing System.Collections.Generic;`nusing System.IO;`nusing System.Reflection;`nusing System.Text.Json;`n"+((($oldParts -join "`n").Replace('Bokumyaku.Simulation','Bokumyaku.Old')))+"`n"+(($files|ForEach-Object{([IO.File]::ReadAllText($_.FullName)-replace '(?m)^using [^;]+;\s*','').Replace('Bokumyaku.Simulation','Bokumyaku.New')})-join "`n")
$source+=@'
namespace ResearchIndependent {
 public static class Probe {
  static readonly JsonSerializerOptions Options=new JsonSerializerOptions{IncludeFields=true};
  static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
  static Bokumyaku.New.MetaProgress Meta(int kind,int level,int crystals){var m=new Bokumyaku.New.MetaProgress{Crystals=crystals};if(kind==0)m.Damage=level;else if(kind==1)m.Supply=level;else if(kind==2)m.Growth=level;else m.Harvest=level;return m;}
  public sealed class PriceRow{public int kind,level,cost;public double damageMultiplier;}
  public static PriceRow[] Prices(){
   var rows=new List<PriceRow>();
   for(int kind=0;kind<4;kind++){
    var upgrade=(Bokumyaku.New.MetaUpgrade)kind;int cap=kind==0?100:20;Check(Bokumyaku.New.MetaProgress.MaxLevel(upgrade)==cap,"cap");int previous=0;
    int baseCost=kind==1?10:kind==3?12:8;int legacy10=(int)Math.Ceiling(baseCost*Math.Pow(1.48,10));int new20=(int)Math.Ceiling(legacy10*Math.Pow(1.22,10));
    for(int level=0;level<=cap;level++){
     var meta=Meta(kind,level,0);int cost=meta.Cost(upgrade);
     int expected=level<=10?(int)Math.Ceiling(baseCost*Math.Pow(1.48,level)):level<=20?(int)Math.Ceiling(legacy10*Math.Pow(1.22,level-10)):(int)Math.Ceiling(new20*Math.Pow(1+.05*(level-20),2));
     Check(cost==expected&&cost>0&&cost>=previous,"cost formula "+kind+"/"+level);previous=cost;
     if(kind==0){var old=new Bokumyaku.Old.MetaProgress{Damage=level};Check(meta.DamageMultiplier==old.DamageMultiplier,"damage multiplier unchanged");}
     var shortBalance=Meta(kind,level,cost-1);Check(!shortBalance.TryBuy(upgrade)&&shortBalance.Crystals==cost-1&&shortBalance.Level(upgrade)==level,"one short atomic");
     var exact=Meta(kind,level,cost);bool bought=exact.TryBuy(upgrade);Check(bought==(level<cap),"exact balance/cap");Check(exact.Crystals==(level<cap?0:cost)&&exact.Level(upgrade)==(level<cap?level+1:level),"exact purchase accounting");
     rows.Add(new PriceRow{kind=kind,level=level,cost=cost,damageMultiplier=meta.DamageMultiplier});
    }
   }
   foreach(int invalid in new[]{-1,4,int.MaxValue}){
    var meta=Meta(0,0,9999);var kind=(Bokumyaku.New.MetaUpgrade)invalid;Check(!meta.TryBuy(kind)&&meta.Crystals==9999&&meta.Damage==0,"invalid purchase atomic");
    bool rejected=false;try{meta.Cost(kind);}catch(ArgumentOutOfRangeException){rejected=true;}Check(rejected,"invalid cost enum");
    rejected=false;try{Bokumyaku.New.MetaProgress.MaxLevel(kind);}catch(ArgumentOutOfRangeException){rejected=true;}Check(rejected,"invalid cap enum");
   }
   return rows.ToArray();
  }
  public sealed class CombatRow{public int seed,damage,otherMeta,weapon,build,ticks,wave,crystals;public string status;public bool observationLimit;public int exactFutureTicks;}
  public static CombatRow Compare(int damage,int weapon,int build){
   int others=damage==0?0:20;
   var old=new Bokumyaku.Old.RunSimulator(42,new Bokumyaku.Old.MetaProgress{Damage=damage,Supply=others,Growth=others,Harvest=others});
   var current=new Bokumyaku.New.RunSimulator(42,new Bokumyaku.New.MetaProgress{Damage=damage,Supply=others,Growth=others,Harvest=others});
   old.SetWeapon((Bokumyaku.Old.WeaponKind)weapon);current.SetWeapon((Bokumyaku.New.WeaponKind)weapon);old.SetBuild((Bokumyaku.Old.BuildPreset)build);current.SetBuild((Bokumyaku.New.BuildPreset)build);
   Check(JsonSerializer.Serialize(old.CaptureState(),Options)==JsonSerializer.Serialize(current.CaptureState(),Options),"initial snapshot");
   for(int i=0;i<900;i++){old.Tick();current.Tick();Check(JsonSerializer.Serialize(old.CaptureState(),Options)==JsonSerializer.Serialize(current.CaptureState(),Options),"900tick snapshot "+damage+"/"+weapon+"/"+build+"/"+i);}
   while(old.State==Bokumyaku.Old.RunStatus.Running&&old.TickCount<720000){old.Tick();current.Tick();}
   Check(JsonSerializer.Serialize(old.CaptureState(),Options)==JsonSerializer.Serialize(current.CaptureState(),Options),"terminal or observation snapshot "+damage+"/"+weapon+"/"+build);
   return new CombatRow{seed=42,damage=damage,otherMeta=others,weapon=weapon,build=build,ticks=current.TickCount,wave=current.Wave,crystals=current.CrystalsReward,status=current.State.ToString(),observationLimit=current.State==Bokumyaku.New.RunStatus.Running,exactFutureTicks=900};
  }
 }
}
'@
# Run only after the Core implementation freeze is confirmed.
Add-Type -TypeDefinition $source
$prices=[ResearchIndependent.Probe]::Prices()
$combat=@(foreach($damage in @(0,20,100)){foreach($weapon in @(0,4)){foreach($build in @(1,2)){[ResearchIndependent.Probe]::Compare($damage,$weapon,$build)}}})
[ordered]@{classification='Standalone C# independent price-domain and unchanged-combat verification; not Unity validation';passed=$true;baselineCommit=$commit;baselineHashesFormat='UTF8 LF one final LF';baselineHashes=$oldHashes;productionHashesFormat='Exact file bytes SHA256';productionHashes=$hashes;prices=$prices;priceChecks='All164 levels, 1-short refusal, exact funds purchase, cap refusal, invalid enums, unchanged DamageMultiplier';combat=$combat;combatChecks='Same seed42/meta/build/weapon full snapshot exact at initial state, each of first900ticks, and natural terminal or720000tick observation limit';limitations=@('Research price can change paid progression even though fixed-meta combat is unchanged','High-meta conditions are injected configuration, not evidence of paid leveling to100','Running at observation limit is not a natural terminal')}|ConvertTo-Json -Depth 8|Set-Content "$root/docs/evidence/research-v11-independent.json" -Encoding utf8
'PASS: independent research price domain and fixed-meta combat comparison'
