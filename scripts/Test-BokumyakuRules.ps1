$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$files=Get-ChildItem "$root/UnitySandbox/Assets/BokumyakuPrototype/Simulation/*.cs"|Sort-Object Name
$source="using System;`nusing System.Collections.Generic;`nusing System.IO;`nusing System.Reflection;`n"+(($files|ForEach-Object{[IO.File]::ReadAllText($_.FullName)-replace '(?m)^using [^;]+;\s*',''})-join "`n")
Add-Type -TypeDefinition $source
$opt=[System.Text.Json.JsonSerializerOptions]::new();$opt.IncludeFields=$true
function Read-Snapshot($path){[System.Text.Json.JsonSerializer]::Deserialize([IO.File]::ReadAllText($path),[Bokumyaku.Simulation.RunSnapshot],$opt)}
function State($r){[System.Text.Json.JsonSerializer]::Serialize($r.CaptureState(),$opt)}
$f="$root/UnitySandbox/Assets/BokumyakuPrototype/Tests/Fixtures"
$r=[Bokumyaku.Simulation.RunSimulator]::Restore((Read-Snapshot "$f/v5-run-tick1800.json"))
for($i=0;$i -lt 900;$i++){$r.Tick()}
$expected=[Bokumyaku.Simulation.RunSimulator]::Restore((Read-Snapshot "$f/v5-run-tick2700.json"))
if((State $r) -cne (State $expected)){throw 'Legacy 900-tick exact continuation mismatch'}
while($r.State -eq 'Running'){$r.Tick()}
$expected=[Bokumyaku.Simulation.RunSimulator]::Restore((Read-Snapshot "$f/v5-run-terminal.json"))
if((State $r) -cne (State $expected)){throw 'Legacy terminal exact continuation mismatch'}
$checks=0
foreach($b in @(1,2)){foreach($w in 0..5){
 $r=[Bokumyaku.Simulation.RunSimulator]::new(42);$r.SetBuild($b);$r.SetWeapon($w)
 for($i=0;$i -lt 6000;$i++){$r.Tick()}
 $s=[System.Text.Json.JsonSerializer]::Deserialize((State $r),[Bokumyaku.Simulation.RunSnapshot],$opt)
 $copy=[Bokumyaku.Simulation.RunSimulator]::Restore($s)
 for($i=0;$i -lt 900;$i++){$r.Tick();$copy.Tick();if((State $r) -cne (State $copy)){throw "New rules exact continuation mismatch $b/$w/$i"}}
 $checks++
}}
[ordered]@{classification='Standalone C# System.Text.Json verification; Unity JsonUtility tested separately';legacyFixture900TicksExact=$true;legacyFixtureTerminalExact=$true;newRules900TickEveryTickChecks=$checks;rulesVersion=2}|ConvertTo-Json|Set-Content "$root/docs/evidence/bokumyaku-balance-v6-rules-checks.json" -Encoding utf8
'PASS: actual v5 fixture exact continuation and 12 new-rules JSON continuation cases'
