[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$source = Join-Path $root 'UnitySandbox/Assets/BokumyakuPrototype/Simulation/RunSimulator.cs'
Add-Type -Path $source
$rows = @()
foreach ($seed in 1..10) {
    foreach ($build in [Enum]::GetValues([Bokumyaku.Simulation.BuildPreset])) {
        $run = [Bokumyaku.Simulation.RunSimulator]::new($seed)
        $run.SetBuild($build)
        $maxTips = $run.TipCount
        while ($run.State -eq [Bokumyaku.Simulation.RunStatus]::Running) {
            $run.Tick()
            $maxTips = [Math]::Max($maxTips, $run.TipCount)
            if ($run.TickCount -gt [Bokumyaku.Simulation.RunSimulator]::MaxTicks -or $run.TipCount -gt 12 -or $run.Branches.Count -gt 64 -or $run.RunInk -lt 0) { throw 'Run invariant violated.' }
        }
        $rows += [ordered]@{
            seed=$seed; build=$build.ToString(); policy=$run.Policy.ToString(); metaLevels=@(0,0,0,0)
            status=$run.State.ToString(); ticks=$run.TickCount; durationSeconds=$run.ElapsedSeconds
            wave=$run.Wave; completedWave=$run.CompletedWave; kills=$run.Kills; leaks=$run.Leaks
            maxTips=$maxTips; finalTips=$run.TipCount; nodes=$run.Branches.Count
            incomeEarned=$run.IncomeEarned; inkRemaining=$run.RunInk; incomePerMinute=$run.IncomePerMinute
            crystals=$run.CrystalsReward; crystalsPerMinute=$run.CrystalsReward*60/$run.ElapsedSeconds
            upgradeLevels=$run.UpgradeLevels
        }
    }
}
function Get-Summary($values) {
    $sorted = @($values | Sort-Object)
    return [ordered]@{ min=$sorted[0]; median=($sorted[4]+$sorted[5])/2; max=$sorted[9] }
}
$summaries = @(
    foreach ($build in @('Manual','Farming','Record')) {
        $selected = @($rows | Where-Object { $_.build -eq $build })
        [ordered]@{
            build=$build; conditions=$selected.Count
            durationSeconds=Get-Summary @($selected | ForEach-Object { $_.durationSeconds })
            tips=Get-Summary @($selected | ForEach-Object { $_.finalTips })
            incomePerMinute=Get-Summary @($selected | ForEach-Object { $_.incomePerMinute })
            wave=Get-Summary @($selected | ForEach-Object { $_.wave })
            inFiveToEightMinutes=@($selected | Where-Object { $_.durationSeconds -ge 300 -and $_.durationSeconds -le 480 }).Count
            defeated=@($selected | Where-Object { $_.status -eq 'Defeated' }).Count
        }
    }
)
$recordWins = 0; $farmingWins = 0
foreach ($seed in 1..10) {
    $f = $rows | Where-Object { $_.seed -eq $seed -and $_.build -eq 'Farming' }
    $r = $rows | Where-Object { $_.seed -eq $seed -and $_.build -eq 'Record' }
    if ($r.ticks -gt $f.ticks) { $recordWins++ }
    if ($f.ticks -gt $r.ticks) { $farmingWins++ }
}
$report = [ordered]@{
    classification='Standalone C# simulation, PowerShell Add-Type; not Unity or player testing'
    sourceSha256=(Get-FileHash $source -Algorithm SHA256).Hash
    generatedAtUtc=[DateTime]::UtcNow.ToString('o')
    config='v2 experimental hypotheses; 3 enemy types, no bosses or splitting enemies'
    limits=[ordered]@{ maxWave=60; maxTicks=21600; tickRate=30; maxEnemies=96; maxTips=12; maxNodes=64 }
    sharedTuning=[ordered]@{ baseAttackDamage=8; baseAttackRange=150; growthRadius=150; growthThreshold='4+growthEventCount, before meta discount' }
    unchanged=@('Wave HP and spawn equations','Enemy HP/speed/type multipliers','Kill rewards and crystal formula','Upgrade cost equations','Supply and pressure equations')
    buildRules=[ordered]@{
        Manual='No automatic purchases'
        Farming='Every 30 ticks; reserve branch cost toward branchLv6 when damage+speed+supply >= branchLv*2. Otherwise cycle Supply,Speed,Width,Damage,Bleed using total purchased levels as index; wait for chosen item.'
        Record='Every 30 ticks; reserve branch cost toward branchLv9 when damage+speed+supply >= branchLv*1. Otherwise cycle Damage,Supply,Speed,Damage,Width using total purchased levels as index; wait for chosen item.'
    }
    finiteCandidateExperiment=[ordered]@{
        description='Three Record purchase candidates, seeds1..10, other coefficients fixed. Tested before the independent blocked-growth charge bugfix; final runs below include the bugfix.'
        candidates=@(
            [ordered]@{name='A';branchTargetLevel=6;milestone=2;cycle='Damage,Supply,Speed,Damage,Width';waves=@(42,42,42,40,40,42,39,42,43,41)}
            [ordered]@{name='B';branchTargetLevel=9;milestone=1;cycle='Damage,Supply,Speed,Damage,Width';waves=@(43,44,43,42,41,43,40,42,45,43)}
            [ordered]@{name='C';branchTargetLevel=9;milestone=1;cycle='Supply,Speed,Damage,Speed,Width,Bleed';waves=@(40,41,42,38,38,41,38,37,40,39)}
        )
        selected='B: highest Wave across the three candidates at each paired seed; no changes to reward multipliers.'
        bugfix='Blocked growth retains charge and does not increase growth-event cost; only successful node creation consumes charge.'
        interpretation='Record is a candidate push build, not a proven optimum. Farming is not independently established as an income-specialized role; inspect paired income and Wave outcomes.'
    }
    rewardDifferences='None; build only selects purchases. No build-specific attack, supply, reward, or enemy multipliers.'
    conditionCount=$rows.Count; independentSeeds=10; summaries=$summaries
    pairedLongerRunCounts=[ordered]@{ Farming=$farmingWins; Record=$recordWins; ties=10-$farmingWins-$recordWins }
    limitations=@('Presets are candidate purchasing strategies, not proven farming/record optima','Income/min means gross kill rewards divided by entire run duration, not retained currency','Five-to-eight-minute target remains a hypothesis; see per-build counts','Manual is a control with no purchases, not an automatically balanced baseline','10 shared seeds are a small paired sample; no statistical generalization','No Unity visual/device performance or five-enemy game acceptance claimed')
    runs=$rows
}
$destination=Join-Path $root 'docs/evidence/bokumyaku-build-comparison-v2.json'
$report | ConvertTo-Json -Depth 10 | Set-Content $destination -Encoding utf8
$summaries | ConvertTo-Json -Depth 4
Write-Output "Paired longer runs: Farming=$farmingWins Record=$recordWins; evidence=$destination"
