# Formula checks only: no combat, spawn RNG, economy simulation, or Unity execution.
# All coefficients are unvalidated design hypotheses from the local GDD v0.2.
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$source = Join-Path $repoRoot 'docs/specs/BOKUMYAKU_GDD_v0.2_Unity.md'
$destination = Join-Path $repoRoot 'docs/evidence/game-balance.json'
$checks = [System.Collections.Generic.List[object]]::new()

function Assert-Formula([string]$Name, [bool]$Condition) {
    $checks.Add([ordered]@{ name = $Name; passed = $Condition })
}

function Test-FiniteNonnegative([double]$Value) {
    return -not [double]::IsNaN($Value) -and -not [double]::IsInfinity($Value) -and $Value -ge 0
}

function Get-Supply([int]$Tips, [double]$Length, [int]$Level) {
    $q0 = 12 * [Math]::Pow(1.09, $Level)
    $qi = ($q0 / $Tips) * [Math]::Exp(-0.0013 * $Length)
    return [ordered]@{
        tips = $Tips; pathLength = $Length; supplyLevel = $Level
        rootSupply = $q0; tipSupply = $qi
        pressure = [Math]::Min(1.65, [Math]::Max(0.45, [Math]::Sqrt($qi / 3.0)))
    }
}

$yieldMultipliers = [ordered]@{ normal = 1.0; fast = 1.1; heavy = 2.0; split = 1.4; boss = 12.0 }
$waves = @(
    foreach ($w in 1..20) {
        $hp = 16 * [Math]::Pow(1.072, $w - 1) * (1 + 0.12 * [Math]::Floor(($w - 1) / 10.0))
        $spawn = [Math]::Min(4.8, 0.50 + 0.027 * $w + 0.0007 * $w * $w)
        $rewards = [ordered]@{}
        foreach ($type in $yieldMultipliers.Keys) {
            # GDD specifies round but not tie-breaking. Make this assumption explicit in output.
            $rewards[$type] = [Math]::Round(4 * $yieldMultipliers[$type] * (1 + 0.018 * $w), 0, [MidpointRounding]::ToEven)
        }
        [ordered]@{
            wave = $w; baseEnemyHp = $hp; spawnPerSecond = $spawn
            expectedRegularSpawns = 9 * $spawn
            baseEnemySpeed = 35 * (1 + 0.006 * ($w - 1))
            killRewards = $rewards
            crystalsWithZeroBossKills = [Math]::Floor(4 + 0.25 * [Math]::Pow($w, 1.40))
        }
    }
)

foreach ($field in @('baseEnemyHp', 'spawnPerSecond', 'expectedRegularSpawns', 'baseEnemySpeed', 'crystalsWithZeroBossKills')) {
    $previous = -1.0
    foreach ($row in $waves) {
        Assert-Formula "wave.$($row.wave).$field.finite_nonnegative_monotone" ((Test-FiniteNonnegative $row[$field]) -and $row[$field] -ge $previous)
        $previous = $row[$field]
    }
}
foreach ($type in $yieldMultipliers.Keys) {
    $previous = -1.0
    foreach ($row in $waves) {
        $reward = $row.killRewards[$type]
        Assert-Formula "wave.$($row.wave).reward.$type.finite_nonnegative_monotone" ((Test-FiniteNonnegative $reward) -and $reward -ge $previous)
        $previous = $reward
    }
}
Assert-Formula 'wave1.hp_anchor_16' ($waves[0].baseEnemyHp -eq 16)
Assert-Formula 'wave1.spawn_anchor_0.5277' ([Math]::Abs($waves[0].spawnPerSecond - 0.5277) -lt 1e-12)
Assert-Formula 'wave11.band_multiplier_begins' ([Math]::Abs($waves[10].baseEnemyHp / $waves[9].baseEnemyHp - 1.072 * 1.12) -lt 1e-12)

$supplyTable = @(
    foreach ($length in @(100, 300)) {
        foreach ($tips in @(1, 3, 6, 12)) {
            $previousPressure = -1.0
            foreach ($level in 0..25) {
                $row = Get-Supply $tips $length $level
                Assert-Formula "supply.n$tips.L$length.lv$level.finite_monotone" ((Test-FiniteNonnegative $row.tipSupply) -and $row.pressure -ge $previousPressure -and $row.pressure -ge 0.45 -and $row.pressure -le 1.65)
                $previousPressure = $row.pressure
                $row
            }
        }
        $previousSupply = [double]::PositiveInfinity
        $previousPressure = [double]::PositiveInfinity
        foreach ($tips in @(1, 3, 6, 12)) {
            $row = Get-Supply $tips $length 0
            Assert-Formula "branch_dilution.n$tips.L$length" ($row.tipSupply -lt $previousSupply -and $row.pressure -le $previousPressure)
            $previousSupply = $row.tipSupply; $previousPressure = $row.pressure
        }
    }
)
$recovery = @(
    foreach ($length in @(100, 300)) {
        $baseline = Get-Supply 3 $length 0
        $diluted = Get-Supply 12 $length 0
        $level = [int][Math]::Ceiling([Math]::Log(12.0 / 3.0) / [Math]::Log(1.09))
        $restored = Get-Supply 12 $length $level
        Assert-Formula "supply_upgrade_restores_3_to_12_tips.L$length" ($level -le 25 -and $diluted.pressure -lt $baseline.pressure -and $restored.pressure -ge $baseline.pressure)
        [ordered]@{ pathLength = $length; baseline = $baseline; diluted = $diluted; restored = $restored; requiredSupplyLevel = $level }
    }
)

$failures = @($checks | Where-Object { -not $_.passed })
$report = [ordered]@{
    classification = 'Standalone PowerShell formula checks; NOT Unity tests or combat simulation'
    designStatus = 'All balance coefficients are initial hypotheses, not playtested or balanced.'
    source = 'docs/specs/BOKUMYAKU_GDD_v0.2_Unity.md'
    sourceSha256 = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant()
    sourceSections = @('4.4 supply', '6.2 HP', '6.3 density', '6.4 speed', '7 supply upgrades and kill rewards', '8.1 crystals')
    evaluatedAtUtc = [DateTime]::UtcNow.ToString('o')
    powerShellVersion = $PSVersionTable.PSVersion.ToString()
    assumptions = @(
        'Kill-reward round uses midpoint-to-even; GDD does not specify tie-breaking. No meta reward multiplier is applied.'
        'Crystal table holds bosses_defeated at zero; boss kill counts require a combat simulation.'
        'Expected regular spawn count is 9 times spawn rate, not an integer schedule; separate bosses are excluded.'
        'Pressure is clamped: tip supply decreases strictly with branch count, but pressure can plateau at either clamp.'
        'Supply recovery compares initial 3 tips to 12 tips at equal path length; affordability is not checked.'
        'GDD section 6.5 is an approximate table. Values here are recomputed from the equations, not copied from that table.'
    )
    formulas = [ordered]@{
        hp = '16 * 1.072^(w-1) * (1 + 0.12 * floor((w-1)/10))'
        spawnPerSecond = 'min(4.8, 0.50 + 0.027*w + 0.0007*w*w)'
        killReward = 'round(4 * enemy_yield_multiplier * (1 + 0.018*w))'
        rootSupply = '12 * 1.09^supply_lv'
        tipSupply = '(Q0/n) * exp(-0.0013*L)'
        pressure = 'clamp(sqrt(Qi/3.0), 0.45, 1.65)'
    }
    passed = ($failures.Count -eq 0); checkCount = $checks.Count; failureCount = $failures.Count
    checks = $checks; waves = $waves; supplyTable = $supplyTable; supplyRecovery = $recovery
    notVerified = @('Combat, enemies, targeting, branch geometry, damage and DPS', 'Spawn RNG and seed replay', 'Upgrade affordability and income over time', 'Survival waves, defeat, retry, save, and permanent progression', 'Unity scenes, visual quality, device performance', 'GDD Gate A: requires full runs to death for 10 seeds; not met by formula checks')
}
[System.IO.Directory]::CreateDirectory((Split-Path -Parent $destination)) | Out-Null
$report | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $destination -Encoding utf8
Write-Output "Formula checks: $($checks.Count - $failures.Count) passed / $($failures.Count) failed. Initial hypotheses only."
Write-Output "Evidence: $destination"
if ($failures.Count -gt 0) { throw 'Game formula checks failed; inspect evidence.' }
