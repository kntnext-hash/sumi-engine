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
    while ($run.State -eq 'Running' -and $run.TickCount -le 7200) { $run.Tick() }
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
            while ($a.State -eq 'Running' -and $a.TickCount -le 7200) {
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
            $m.Crystals=10000000
            for ($level=0;$level -lt 20;$level++) { $before=$m.Crystals; $cost=$m.Cost($kind); Assert ($m.TryBuy($kind)) 'Meta purchase failed'; Assert ($m.Crystals -eq $before-$cost -and $m.Level($kind) -eq $level+1) 'Meta accounting' }
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
    Write-Output "TOTAL $script:passed passed / $script:failed failed"
    if ($script:failed -gt 0) { exit 1 }; exit 0
} catch { Write-Error -ErrorAction Continue $_; exit 1 }
