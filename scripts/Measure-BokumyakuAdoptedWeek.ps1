param()
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$files=@(Get-ChildItem (Join-Path $root 'UnitySandbox/Assets/BokumyakuPrototype/Simulation') -Filter '*.cs'|Sort-Object Name)
$hashes=@($files|ForEach-Object{[ordered]@{file=$_.Name;sha256=(Get-FileHash $_.FullName -Algorithm SHA256).Hash}})
$header="using System;`nusing System.Collections.Generic;`nusing System.IO;`nusing System.Reflection;`nusing Bokumyaku.Simulation;`n"
$source=$header+(($files|ForEach-Object{[IO.File]::ReadAllText($_.FullName)-replace '(?m)^using [^;]+;\s*',''})-join "`n")
$runner=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'RankWeekProbe.cs.txt')).Replace(',milestone=0','')
$runner=$runner.Replace("run.SetBuild(BuildPreset.Farming);run.SetWeapon(WeaponKind.Point);","run.SetBuild(round%4==0?BuildPreset.Record:BuildPreset.Farming);run.SetWeapon(round%4==0?WeaponKind.Enso:WeaponKind.Point);").Replace("record.SetWeapon(WeaponKind.Point);","record.SetWeapon(WeaponKind.Enso);")
$reports=@()
foreach($factor in @('1.48')) {
 $ns='BokumyakuPriceProbe'+$factor.Replace('.','')
 $code=$source
 $candidateRunner=$runner.Replace("level<=20?Math.Max(500+25*level,meta.Cost(chosen)):meta.Cost(chosen)","meta.Cost(chosen)")
 $code=($code+"`nnamespace Bokumyaku.Simulation {`n"+$candidateRunner+"`n}").Replace('Bokumyaku.Simulation',"$ns.Simulation")
 Add-Type -TypeDefinition $code
 $type=("$ns.Simulation.RankWeekProbe" -as [type]);$result=$type::Run()
 $reports += [ordered]@{factor=$factor;result=$result}
}
$report=[ordered]@{sourceCommit=(& git -C $root rev-parse HEAD);sourceHashes=$hashes;policy='Isolated compiled candidates; no live changes. Every fourth run Enso/Record, otherwise Point/Farming; diagnostic Record uses Enso. Lowest-level research D/S/G/H, Production source rules9/price3 unchanged; no coefficient replacement. 1h/day,7h/week simulation budget. Highest unlocked tier chosen next run. Record probes are extra-budget diagnostics; incomplete rewards excluded.';reports=$reports}
$report|ConvertTo-Json -Depth 14|Set-Content (Join-Path $root 'docs/evidence/rank-adopted-week.json') -Encoding utf8
$reports|ForEach-Object{$_.factor;$_.result.milestones|ConvertTo-Json -Depth 5}
