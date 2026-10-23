$ErrorActionPreference='Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
if (Test-Path 'UnitySandbox/Assets/BokumyakuPrototype/Tests/Fixtures/rules8-tier2-research-tick1800.json') { throw 'Golden fixtures already exist; generate only in a separate clean checkout without these fixtures.' }
$parts=@(git ls-tree -r --name-only f72af05 -- UnitySandbox/Assets/BokumyakuPrototype/Simulation | Where-Object {$_ -like '*.cs'} | ForEach-Object {((git show "f72af05:$_") -join "`n") -replace '(?m)^using [^;]+;\s*',''})
$source="using System;`nusing System.IO;`nusing System.Reflection;`nusing System.Collections.Generic;`n"+($parts -join "`n")
Add-Type -TypeDefinition $source
$m=[Bokumyaku.Simulation.MetaProgress]::new();$m.Damage=20;$m.Supply=10;$m.Growth=10;$m.Harvest=10
$r=[Bokumyaku.Simulation.RunSimulator]::new(42,$m,2);$r.SetBuild([Bokumyaku.Simulation.BuildPreset]::Farming)
for($i=0;$i -lt 1800;$i++){$r.Tick()}
$r.CaptureState()|ConvertTo-Json -Depth 100 -Compress|Set-Content UnitySandbox/Assets/BokumyakuPrototype/Tests/Fixtures/rules8-tier2-research-tick1800.json -Encoding utf8
for($i=0;$i -lt 900;$i++){$r.Tick()}
$r.CaptureState()|ConvertTo-Json -Depth 100 -Compress|Set-Content UnitySandbox/Assets/BokumyakuPrototype/Tests/Fixtures/rules8-tier2-research-tick2700.json -Encoding utf8
$r.CaptureState()|Select-Object rulesVersion,tickCount,wave,status

