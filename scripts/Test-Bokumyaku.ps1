# Standalone simulation checks only. Unity Editor/NUnit/Persistence/visual tests are separate.
# Invoke in a fresh PowerShell 7 process. No NUnit shim or Unity stub is used.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$script:passed=0; $script:failed=0
function Assert($condition, [string]$message) { if (!$condition) { throw $message } }
function Check([string]$name, [scriptblock]$body) {
    try { & $body; $script:passed++; Write-Output "PASS $name" }
    catch { $script:failed++; Write-Output "FAIL ${name}: $_" }
}
function Finish($run) {
    while ($run.State -eq 'Running' -and $run.TickCount -le [Bokumyaku.Simulation.RunSimulator]::MaxTicks) { $run.Tick() }
    Assert ($run.State -ne 'Running') 'Run did not terminate'
}
try {
    if ($PSVersionTable.PSVersion.Major -lt 7) { throw 'PowerShell 7 required' }
    $repo=Split-Path -Parent $PSScriptRoot
    Add-Type -Path @(Get-ChildItem -LiteralPath (Join-Path $repo 'UnitySandbox/Assets/BokumyakuPrototype/Simulation') -Filter '*.cs' | Select-Object -ExpandProperty FullName)
    Write-Output "Standalone Bokumyaku / PowerShell $($PSVersionTable.PSVersion) / .NET $([Environment]::Version)"
    Write-Output 'Not Unity, NUnit, persistence, visual or device evidence.'
    Check 'fixed seeds replay state and enemies at every tick' {
        foreach ($seed in @(0,42,-17)) {
            $a=[Bokumyaku.Simulation.RunSimulator]::new($seed); $b=[Bokumyaku.Simulation.RunSimulator]::new($seed)
            while ($a.State -eq 'Running' -and $a.TickCount -le [Bokumyaku.Simulation.RunSimulator]::MaxTicks) {
                $a.Tick(); $b.Tick()
                Assert ($a.CoreHp -eq $b.CoreHp -and $a.Kills -eq $b.Kills -and $a.RunInk -eq $b.RunInk -and $a.Enemies.Count -eq $b.Enemies.Count) 'Score mismatch'
                for ($i=0;$i -lt $a.Enemies.Count;$i++) { Assert ($a.Enemies[$i].X -eq $b.Enemies[$i].X -and $a.Enemies[$i].Y -eq $b.Enemies[$i].Y -and $a.Enemies[$i].Hp -eq $b.Enemies[$i].Hp) 'Enemy mismatch' }
            }
            Assert ($a.State -eq 'Defeated' -and $b.State -eq 'Defeated') 'Expected natural defeat'
        }
    }
    Check 'insufficient purchases preserve currency and levels' {
        $r=[Bokumyaku.Simulation.RunSimulator]::new(42)
        foreach ($kind in [Enum]::GetValues([Bokumyaku.Simulation.UpgradeKind])) { Assert (!$r.TryBuy($kind)) 'Unaffordable purchase accepted' }
        Assert ($r.RunInk -eq 0 -and ($r.UpgradeLevels | Measure-Object -Sum).Sum -eq 0) 'Purchase mutated state'
    }
    Check 'earned purchase charges exact cost' {
        $m=[Bokumyaku.Simulation.MetaProgress]::new(); $m.Damage=20; $m.Harvest=20
        $r=[Bokumyaku.Simulation.RunSimulator]::new(42,$m)
        while ($r.State -eq 'Running' -and $r.RunInk -lt 10) { $r.Tick() }
        $before=$r.RunInk; $cost=$r.UpgradeCost('Damage')
        Assert ($r.TryBuy('Damage')) 'Could not buy after earning'; Assert ($r.RunInk -eq $before-$cost -and $r.UpgradeLevels[0] -eq 1) 'Wrong charge'
    }
    Check 'actual split dilutes pressure at fixed path length' {
        $m=[Bokumyaku.Simulation.MetaProgress]::new(); $m.Damage=20; $m.Harvest=20; $m.Growth=20
        $r=[Bokumyaku.Simulation.RunSimulator]::new(42,$m); $reference=$r.Branches[1]; $before=$r.Pressure($reference)
        while ($r.State -eq 'Running' -and $r.RunInk -lt 22) { $r.Tick() }
        Assert ($r.TryBuy('Branch')) 'Could not fund split'
        while ($r.State -eq 'Running' -and $r.TipCount -eq 3) { $r.Tick() }
        Assert ($r.TipCount -eq 4 -and $r.Pressure($reference) -lt $before) 'No dilution'
    }
    Check 'natural defeat stops ticks and rejects duplicate rewards' {
        $r=[Bokumyaku.Simulation.RunSimulator]::new(42); $m=[Bokumyaku.Simulation.MetaProgress]::new()
        Assert (!$r.ClaimReward($m)) 'Running reward'; Finish $r; $tick=$r.TickCount
        $r.Tick(); Assert ($r.State -eq 'Defeated' -and $r.CoreHp -eq 0 -and $r.TickCount -eq $tick) 'Did not stop'
        Assert ($r.ClaimReward($m)) 'First reward rejected'; $balance=$m.Crystals
        Assert (!$r.ClaimReward($m) -and $m.Crystals -eq $balance) 'Duplicate reward'
    }
    Check 'meta costs and caps hold for every type' {
        foreach ($kind in [Enum]::GetValues([Bokumyaku.Simulation.MetaUpgrade])) {
            $m=[Bokumyaku.Simulation.MetaProgress]::new(); Assert (!$m.TryBuy($kind)) 'Unfunded meta purchase'
            $m.Crystals=[int]::MaxValue
            for ($level=0;$level -lt [Bokumyaku.Simulation.MetaProgress]::MaxLevel($kind);$level++) { $before=$m.Crystals; $cost=$m.Cost($kind); Assert ($m.TryBuy($kind)) 'Meta purchase failed'; Assert ($m.Crystals -eq $before-$cost -and $m.Level($kind) -eq $level+1) 'Meta accounting' }
            Assert (!$m.TryBuy($kind)) 'Meta cap ignored'
        }
    }
    Check 'three natural runs retain permanent progress and reset run ink' {
        $m=[Bokumyaku.Simulation.MetaProgress]::new(); $earned=0; $spent=0
        foreach ($seed in @(42,43,44)) {
            $r=[Bokumyaku.Simulation.RunSimulator]::new($seed,$m); Assert ($r.RunInk -eq 0) 'Run ink carried over'
            Finish $r; Assert ($r.ClaimReward($m)) 'Reward rejected'; $earned+=$r.CrystalsReward
            $cost=$m.Cost('Damage'); if ($m.TryBuy('Damage')) { $spent+=$cost }
            Assert ($m.Crystals -eq $earned-$spent) 'Meta accounting mismatch'
        }
        Assert ($m.Damage -gt 0) 'Three runs did not fund permanent upgrade'
    }
    Check 'auto builds replay purchases, retain branch budget, and terminate' {
        foreach ($build in @('Farming','Record')) {
            $a=[Bokumyaku.Simulation.RunSimulator]::new(42); $b=[Bokumyaku.Simulation.RunSimulator]::new(42)
            $a.SetBuild($build); $b.SetBuild($build); $tips=3
            while ($a.State -eq 'Running' -and $a.TickCount -le [Bokumyaku.Simulation.RunSimulator]::MaxTicks) {
                $a.Tick(); $b.Tick(); $tips=[Math]::Max($tips,$a.TipCount)
                Assert ($a.CoreHp -eq $b.CoreHp -and $a.RunInk -eq $b.RunInk -and $a.Kills -eq $b.Kills) 'Automatic replay mismatch'
                Assert (($a.UpgradeLevels -join ',') -eq ($b.UpgradeLevels -join ',')) 'Automatic purchases differ'
                Assert ($a.RunInk -ge 0 -and $a.TipCount -le 12 -and $a.Branches.Count -le 64 -and $a.Enemies.Count -le 96) 'State bound violated'
                foreach ($branch in $a.Branches) { if ($branch.Id -ne 0) { Assert ($branch.ParentId -ge 0 -and $branch.ParentId -lt $branch.Id) 'Graph cycle or invalid parent' } }
            }
            Assert ($a.State -ne 'Running' -and $a.State -ne 'CapacityExceeded') 'No valid finite terminal state'
            Assert ($a.UpgradeLevels[3] -gt 0 -and $tips -gt 3) 'Branch starved by cheaper upgrades'
            Write-Output "  $build tick=$($a.TickCount) state=$($a.State) wave=$($a.CompletedWave) tips=$tips levels=$($a.UpgradeLevels -join ',')"
        }
    }
    Check 'switching to Manual halts automatic spending' {
        $r=[Bokumyaku.Simulation.RunSimulator]::new(42); $r.SetBuild('Farming')
        for ($i=0;$i -lt 600;$i++) { $r.Tick() }
        Assert ($r.State -eq 'Running') 'Fixture ended before switching'
        $r.SetBuild('Record'); for ($i=0;$i -lt 600;$i++) { $r.Tick() }
        Assert ($r.State -eq 'Running') 'Fixture ended before Manual'
        $r.SetBuild('Manual'); $levels=$r.UpgradeLevels -join ','; $ink=$r.RunInk
        Finish $r; Assert (($r.UpgradeLevels -join ',') -eq $levels -and $r.RunInk -ge $ink) 'Manual build spent automatically'
    }
    Check 'weapon switches replay attack and field state and retain finite termination' {
        $a=[Bokumyaku.Simulation.RunSimulator]::new(42); $b=[Bokumyaku.Simulation.RunSimulator]::new(42)
        $a.SetBuild('Record'); $b.SetBuild('Record')
        while ($a.State -eq 'Running' -and $a.TickCount -le [Bokumyaku.Simulation.RunSimulator]::MaxTicks) {
            if ($a.TickCount -eq 300) { $a.SetWeapon('Pierce'); $b.SetWeapon('Pierce') }
            if ($a.TickCount -eq 900) { $a.SetWeapon('Bleed'); $b.SetWeapon('Bleed') }
            $a.Tick(); $b.Tick()
            Assert ($a.Weapon -eq $b.Weapon -and $a.CoreHp -eq $b.CoreHp -and $a.Kills -eq $b.Kills -and $a.RunInk -eq $b.RunInk) 'Weapon replay mismatch'
            Assert ($a.Attacks.Count -eq $b.Attacks.Count -and $a.Fields.Count -eq $b.Fields.Count -and $a.Fields.Count -le 96) 'Attack/field count mismatch'
            for ($i=0;$i -lt $a.Fields.Count;$i++) {
                Assert ($a.Fields[$i].X -eq $b.Fields[$i].X -and $a.Fields[$i].DamagePerTick -eq $b.Fields[$i].DamagePerTick -and $a.Fields[$i].UntilTick -eq $b.Fields[$i].UntilTick) 'Field replay mismatch'
            }
        }
        Assert ($a.State -ne 'Running' -and $a.State -ne 'CapacityExceeded') 'Weapon run did not end normally'
    }
    Check 'serialized checkpoints replay all six weapons and isolate mutable snapshots' {
        $options=[System.Text.Json.JsonSerializerOptions]::new(); $options.IncludeFields=$true
        foreach ($weapon in [Enum]::GetValues([Bokumyaku.Simulation.WeaponKind])) {
            $a=[Bokumyaku.Simulation.RunSimulator]::new(42)
            $a.SetBuild('Farming'); $a.SetPolicy('Intercept'); $a.SetWeapon($weapon)
            for($i=0;$i -lt 1800 -and $a.State -eq 'Running';$i++){$a.Tick()}
            $snapshot=$a.CaptureState()
            $json=[System.Text.Json.JsonSerializer]::Serialize($snapshot,$options)
            $dto=[System.Text.Json.JsonSerializer]::Deserialize($json,[Bokumyaku.Simulation.RunSnapshot],$options)
            $b=[Bokumyaku.Simulation.RunSimulator]::Restore($dto)
            $dto.levels[0]=30
            Assert ($a.UpgradeLevels[0] -eq $b.UpgradeLevels[0]) 'Restored run aliases the input DTO'
            while($a.State -eq 'Running'){$a.Tick();$b.Tick()}
            $aj=[System.Text.Json.JsonSerializer]::Serialize($a.CaptureState(),$options)
            $bj=[System.Text.Json.JsonSerializer]::Serialize($b.CaptureState(),$options)
            Assert ($aj -ceq $bj) ('Checkpoint final state differs: '+$weapon)
        }
    }
    Check 'auto repeat requires saved defeat, pauses, and fires once' {
        $c=[Bokumyaku.Simulation.AutoRepeatController]::new()
        Assert (!$c.Enabled -and !$c.Schedule('Defeated',$true)) 'Auto repeat enabled by default'
        $c.SetEnabled($true); Assert (!$c.IsPending) 'Enabling scheduled a run'
        Assert (!$c.Schedule('Defeated',$false)) 'Unsaved defeat scheduled'
        Assert (!$c.Schedule('TestCompleted',$true)) 'Test completion scheduled'
        Assert ($c.Schedule('Defeated',$true)) 'Saved defeat not scheduled'
        Assert (!$c.Advance(2,$false)) 'Early repeat'
        Assert (!$c.Advance(100,$true) -and $c.SecondsRemaining -eq 1) 'Pause consumed countdown'
        Assert ($c.Advance(1,$false)) 'Countdown never fired'
        Assert (!$c.Advance(100,$false)) 'Repeat fired twice'
        $null=$c.Schedule('Defeated',$true); $c.SetEnabled($false)
        Assert (!$c.IsPending -and !$c.Advance(3,$false)) 'Disabled repeat fired'
    }
    Write-Output "TOTAL $script:passed passed / $script:failed failed"
    if ($script:failed -gt 0) { exit 1 }; exit 0
} catch { Write-Error -ErrorAction Continue $_; exit 1 }
