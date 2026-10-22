param([ValidateRange(1,3)][int]$Candidate=1,[switch]$VerifyProduction,[switch]$PostSelectionValidation)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$commit=(& git -C $root rev-parse --verify '94606b8^{commit}').Trim()
if($LASTEXITCODE -ne 0){throw 'Missing baseline'}
$paths=@(& git -C $root ls-tree -r --name-only $commit -- UnitySandbox/Assets/BokumyakuPrototype/Simulation)|Where-Object{$_ -like '*.cs'}|Sort-Object
$parts=@();$hashes=@()
function Hash([string]$text){$h=[Security.Cryptography.SHA256]::Create();try{([BitConverter]::ToString($h.ComputeHash([Text.Encoding]::UTF8.GetBytes($text)))).Replace('-','')}finally{$h.Dispose()}}
foreach($path in $paths){$text=if($VerifyProduction -or $PostSelectionValidation){([IO.File]::ReadAllText((Join-Path $root $path))).Replace("`r`n","`n")}else{(@(& git -C $root show "${commit}:$path") -join "`n")+"`n"};if($LASTEXITCODE -ne 0){throw 'Blob failure'};$hashes += [ordered]@{path=$path;sha256=Hash $text};$parts += ($text -replace '(?m)^using [^;]+;\s*','')}
$source="using System;`nusing System.Collections.Generic;`nusing System.IO;`nusing System.Reflection;`n"+($parts -join "`n")
$growth=@('1.18','1.22','1.26')[$Candidate-1]
$runner=@'
namespace NAMESPACE {
 public class Purchase {public string kind;public int fromLevel,cost;}
 public class Row {public string scenario,weapon,build,status;public int index,seed,ticks,wave,normalReward,balanceBefore,balanceAfter,purchaseCount,spent;public double seconds,rate;public bool censored;public int[] beforeLevels,afterLevels;public List<Purchase> purchases=new List<Purchase>();}
 public class Series {public string scenario,weapon,build;public int firstPurchaseRun=-1,totalRewards,totalSpent,finalBalance;public int[] finalLevels;public List<Row> rows=new List<Row>();}
 public static class Trials {
  static int[] Levels(MetaProgress m){return new[]{m.Damage,m.Supply,m.Growth,m.Harvest};}
  public static Series Run(string scenario,int weapon,int build,int count,bool grown,int seedStart=1){var meta=grown?new MetaProgress{Damage=20,Supply=20,Growth=20,Harvest=20}:new MetaProgress();var result=new Series{scenario=scenario,weapon=((WeaponKind)weapon).ToString(),build=((BuildPreset)build).ToString()};
   for(int i=1;i<=count;i++){var row=new Row{scenario=scenario,index=i,seed=seedStart+i-1,weapon=result.weapon,build=result.build,beforeLevels=Levels(meta),balanceBefore=meta.Crystals};var r=new RunSimulator(seedStart+i-1,meta);r.SetWeapon((WeaponKind)weapon);r.SetBuild((BuildPreset)build);while(r.State==RunStatus.Running&&r.TickCount<180000)r.Tick();if(r.State==RunStatus.CapacityExceeded)throw new Exception("Capacity failure");row.ticks=r.TickCount;row.seconds=r.ElapsedSeconds;row.wave=r.CompletedWave;row.status=r.State.ToString();row.censored=r.State==RunStatus.Running;
    if(!row.censored){row.normalReward=r.CrystalsReward;if(!r.ClaimReward(meta))throw new Exception("Claim failure");result.totalRewards+=row.normalReward;row.rate=row.normalReward*60/(row.seconds+3);
     for(int guard=0;guard<160;guard++){MetaUpgrade chosen=MetaUpgrade.Damage;int lowest=int.MaxValue;bool found=false;foreach(MetaUpgrade kind in Enum.GetValues(typeof(MetaUpgrade))){int lv=meta.Level(kind);if(lv<MetaProgress.MaxLevel(kind)&&lv<lowest){lowest=lv;chosen=kind;found=true;}}if(!found)break;int cost=meta.Cost(chosen);if(!meta.TryBuy(chosen))break;row.purchases.Add(new Purchase{kind=chosen.ToString(),fromLevel=lowest,cost=cost});row.spent+=cost;row.purchaseCount++;if(result.firstPurchaseRun<0)result.firstPurchaseRun=i;}
    }
    result.totalSpent+=row.spent;row.afterLevels=Levels(meta);row.balanceAfter=meta.Crystals;result.rows.Add(row);if(meta.Crystals!=result.totalRewards-result.totalSpent)throw new Exception("Unbalanced cash ledger");if(row.censored)break;
   }result.finalLevels=Levels(meta);result.finalBalance=meta.Crystals;return result;
  }
 }
}
'@
$variants=if($PostSelectionValidation){@('FixedAdditional11to13')}elseif($VerifyProduction){@('Production')}elseif($Candidate -eq 1){@('Baseline',"Growth$Candidate")}else{@("Growth$Candidate")}
foreach($variant in $variants){
 $ns="Bokumyaku.ResearchV11$variant";$src=$source
 if($variant -ne 'Baseline' -and !$VerifyProduction -and !$PostSelectionValidation){
  $pattern='public int Cost\(MetaUpgrade kind\) \{[^\r\n]*\}'
  if(([regex]::Matches($src,$pattern)).Count -ne 1){throw 'Cost method drift'}
  $cost='public int Cost(MetaUpgrade kind) { int level=Level(kind);if(level<0||level>MaxLevel(kind))throw new ArgumentOutOfRangeException(nameof(kind));double basis=kind==MetaUpgrade.Supply?10:kind==MetaUpgrade.Harvest?12:8;if(level<=10)return (int)Math.Ceiling(basis*Math.Pow(1.48,level));double anchor=Math.Ceiling(basis*Math.Pow(1.48,10));if(kind==MetaUpgrade.Damage&&level>20)return (int)Math.Ceiling(Math.Ceiling(anchor*Math.Pow(GROWTH,10))*Math.Pow(1+.05*(level-20),2));return (int)Math.Ceiling(anchor*Math.Pow(GROWTH,level-10)); }'
  $src=[regex]::Replace($src,$pattern,$cost.Replace('GROWTH',$growth))
 }
 $src=$src.Replace('namespace Bokumyaku.Simulation',"namespace $ns")
 Add-Type -TypeDefinition ($src+$runner.Replace('NAMESPACE',$ns))
 $type="$ns.Trials" -as [type]
 $series=if($PostSelectionValidation){@()}else{@($type::Run('grownEnso10',4,1,10,$true),$type::Run('grownEnso10',4,2,10,$true),$type::Run('initialBalanced20',4,1,20,$false))}
 foreach($w in 0..5){foreach($b in @(1,2)){[array]$series+=$type::Run('grownAllWeapons3',$w,$b,3,$true,$(if($PostSelectionValidation){11}else{1}))}}
 $metaType="$ns.MetaProgress" -as [type];$meta=$metaType::new();$prices=@(foreach($kind in 0..3){foreach($level in @(0,10,11,20)){switch($kind){0{$meta.Damage=$level}1{$meta.Supply=$level}2{$meta.Growth=$level}3{$meta.Harvest=$level}};[ordered]@{kind=$kind;level=$level;cost=$meta.Cost($kind)}}})
 $report=[ordered]@{classification='Paid sequential simulation with only research prices varied, no battle changes';baselineCommit=$commit;sourceHashes=$hashes;compilerInputSha256=Hash ($src+$runner.Replace('NAMESPACE',$ns));variant=$variant;candidate=$Candidate;growth= $(if($variant -eq 'Baseline'){1.48}else{[double]$growth});purchasePolicy='After each settled run repeatedly choose uncapped lowest research level; ties Damage,Supply,Growth,Harvest; wait if selected item is unaffordable. No borrowing or grants. Grown20/20/20/20 has Supply/Growth/Harvest capped, so only Damage can be purchased; initial0 series balances all four.';initialFunds=0;rewards='Ordinary crystals only; PB milestone excluded';seedPolicy=$(if($PostSelectionValidation){'Fixed selected1.22, new seeds11..13 per weapon/build, no retuning'}else{'Each independent series seeds1..N'});tickBudgetPerRun=180000;prices=$prices;series=$series;limitations=@('Same seeds and scenarios reused; not independent populations','Grown studies start at externally supplied20/20/20/20; from-zero series is separate','Running atbudget is censored and receives no reward or purchase','No change to battle rules6 or research multipliers/caps')}
 $report|ConvertTo-Json -Depth 15|Set-Content "$root/docs/evidence/research-v11-$variant.json" -Encoding utf8
 if($VerifyProduction){$expected=Get-Content "$root/docs/evidence/research-v11-Growth2.json" -Raw|ConvertFrom-Json;if(($series|ConvertTo-Json -Depth 15 -Compress) -cne ($expected.series|ConvertTo-Json -Depth 15 -Compress)){throw 'Production/candidate ledger mismatch'};$report['productionCandidateLedgerExactMatch']=$true;$report|ConvertTo-Json -Depth 15|Set-Content "$root/docs/evidence/research-v11-$variant.json" -Encoding utf8}
 $series|Select-Object scenario,weapon,build,firstPurchaseRun,totalRewards,totalSpent,finalBalance,@{n='levels';e={$_.finalLevels -join ','}}|Format-Table
}

