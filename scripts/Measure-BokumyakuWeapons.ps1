[CmdletBinding()]
param([string]$OutputPath = 'docs/evidence/bokumyaku-weapons-v3.json')
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$source = Join-Path $root 'UnitySandbox/Assets/BokumyakuPrototype/Simulation/RunSimulator.cs'
$runner = @'
namespace Bokumyaku.Measurement {
    public sealed class Row {
        public int seed, ticks, wave, completedWave, kills, leaks, tips, nodes, income, crystals, hitEvents, peakFields;
        public string weapon, build, status;
        public double seconds, inkPerMinute, crystalsPerMinute, effectiveDamage;
        public int[] levels;
    }
    public static class Weapons {
        public static Row[] Run() {
            var rows = new System.Collections.Generic.List<Row>();
            for(int seed=1; seed<=10; seed++)
            foreach(Simulation.BuildPreset build in System.Enum.GetValues(typeof(Simulation.BuildPreset)))
            foreach(Simulation.WeaponKind weapon in System.Enum.GetValues(typeof(Simulation.WeaponKind))) {
                var run=new Simulation.RunSimulator(seed); run.SetBuild(build); run.SetWeapon(weapon);
                int peak=0, events=0;
                while(run.State==Simulation.RunStatus.Running) {
                    run.Tick(); events+=run.Attacks.Count; peak=System.Math.Max(peak,run.Fields.Count);
                    if(run.TickCount>Simulation.RunSimulator.MaxTicks || run.RunInk<0 || run.TipCount>12 || run.Branches.Count>64 || run.Fields.Count>Simulation.RunSimulator.FieldLimit || double.IsNaN(run.EffectiveDamage))
                        throw new System.Exception("Simulation invariant failed");
                }
                rows.Add(new Row {seed=seed,weapon=weapon.ToString(),build=build.ToString(),status=run.State.ToString(),ticks=run.TickCount,seconds=run.ElapsedSeconds,wave=run.Wave,completedWave=run.CompletedWave,kills=run.Kills,leaks=run.Leaks,tips=run.TipCount,nodes=run.Branches.Count,income=run.IncomeEarned,inkPerMinute=run.IncomePerMinute,crystals=run.CrystalsReward,crystalsPerMinute=run.CrystalsReward*60/run.ElapsedSeconds,levels=run.UpgradeLevels,hitEvents=events,peakFields=peak,effectiveDamage=run.EffectiveDamage});
            }
            return rows.ToArray();
        }
    }
}
'@
Add-Type -TypeDefinition ([IO.File]::ReadAllText($source) + [Environment]::NewLine + $runner)
$rows = [Bokumyaku.Measurement.Weapons]::Run()
function Get-Summary($values) {
    $sorted = @($values | Sort-Object)
    [ordered]@{min=$sorted[0];median=($sorted[4]+$sorted[5])/2;max=$sorted[9]}
}
$summaries = @(foreach($build in @('Manual','Farming','Record')) {
    foreach($weapon in @('Point','Pierce','Bleed')) {
        $r=@($rows | Where-Object {$_.build -eq $build -and $_.weapon -eq $weapon})
        [ordered]@{build=$build;weapon=$weapon;conditions=$r.Count;seconds=(Get-Summary $r.seconds);wave=(Get-Summary $r.wave);inkPerMinute=(Get-Summary $r.inkPerMinute);crystalsPerMinute=(Get-Summary $r.crystalsPerMinute);tips=(Get-Summary $r.tips);peakFields=($r.peakFields | Measure-Object -Maximum).Maximum;defeated=@($r | Where-Object status -eq 'Defeated').Count;withinFiveToEightMinutes=@($r | Where-Object {$_.seconds -ge 300 -and $_.seconds -le 480}).Count}
    }
})
$report=[ordered]@{
    classification='Standalone C# fixed-tick simulation, not Unity/device/physical input evidence'
    sourceSha256=(Get-FileHash $source).Hash
    generatedAtUtc=[DateTime]::UtcNow.ToString('o')
    conditions=90; independentSeeds=10; metaLevels=@(0,0,0,0); policy='Balanced'
    method='Each weapon stays fixed for the entire run. Manual is a zero-purchase control, not skilled manual play. Enemy/spawn/reward/supply formulas are shared; no weapon-specific reward multiplier.'
    limits=[ordered]@{maxWave=60;maxTicks=21600;maxFields=96;maxFieldStacks=2}
    summaries=$summaries; runs=$rows
}
$path=if([IO.Path]::IsPathRooted($OutputPath)){$OutputPath}else{Join-Path $root $OutputPath}
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($path)) | Out-Null
$report | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $path -Encoding utf8
$summaries | ForEach-Object { [pscustomobject]@{build=$_.build;weapon=$_.weapon;medianSeconds=[Math]::Round($_.seconds.median,2);medianWave=$_.wave.median;medianCrystalsPerMinute=[Math]::Round($_.crystalsPerMinute.median,2);defeated=$_.defeated;peakFields=$_.peakFields} } | Format-Table -AutoSize
Write-Output "Evidence: $path"
