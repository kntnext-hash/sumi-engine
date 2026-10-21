# Standalone .NET execution, NOT NUnit or Unity Editor validation.
# Run in a fresh PowerShell 7 process.
[CmdletBinding()]
param([string]$SourceRoot = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference = 'Stop'
$script:passed = 0
$script:failed = 0
function Assert($Condition, [string]$Message) { if (!$Condition) { throw $Message } }
function Check([string]$Name, [scriptblock]$Body) {
    try { & $Body; $script:passed++; Write-Output "PASS $Name" }
    catch { $script:failed++; Write-Output "FAIL ${Name}: $_" }
}
function Reject([scriptblock]$Body) {
    $rejected = $false
    try { & $Body } catch {
        $e = $_.Exception
        while ($e.InnerException) { $e = $e.InnerException }
        if ($e -is [ArgumentException]) { $rejected = $true } else { throw }
    }
    Assert $rejected 'Expected argument rejection'
}
function Paper { [ConnectPiece.SumiEngine.Core.InkPaper]::new(64,64,5) }
function Engine { [ConnectPiece.SumiEngine.Core.SumiEngineCore]::new(64,64,5) }
function Pixels($Paper) {
    $b = [byte[]]::new($Paper.Width * $Paper.Height * 4)
    $Paper.RenderRgba($b)
    [Convert]::ToBase64String($b)
}
function Draw($Engine, [int]$Seed) {
    $Engine.BeginStroke([ConnectPiece.SumiEngine.Core.InkPoint]::new(30,8,0,.6), [ConnectPiece.SumiEngine.Core.InkBrushSettings]::Slash, $Seed)
    $Engine.AppendPoint([ConnectPiece.SumiEngine.Core.InkPoint]::new(30,52,.3,.9))
    $Engine.EndStroke()
}
try {
    if ($PSVersionTable.PSVersion.Major -lt 7) { throw 'PowerShell 7 or newer required.' }
    Write-Output "PowerShell $($PSVersionTable.PSVersion) / .NET $([Environment]::Version)"
    Write-Output 'Classification: S + standalone .NET execution. Unity Editor/NUnit/visual/device NOT RUN.'
    $files = @(Get-ChildItem -LiteralPath (Join-Path $SourceRoot 'Runtime/Core') -Filter '*.cs' | Sort-Object Name | Select-Object -ExpandProperty FullName)
    Add-Type -Path $files
    foreach ($seed in @(0,32,-17,[int]::MaxValue)) {
        Check "self Replay exact pixels seed=$seed" {
            $e = Engine; Draw $e $seed; $before = Pixels $e.Paper
            $e.Replay($e.Session)
            Assert ($e.Session.strokes.Count -eq 1) 'Stroke count changed'
            Assert ($before -ceq (Pixels $e.Paper)) 'Pixels differ'
        }
        Check "Clear fresh-state exact pixels seed=$seed" {
            $e = Engine; Draw $e $seed; $e.Clear(); $fresh = Engine
            Draw $e $seed; Draw $fresh $seed
            Assert ((Pixels $e.Paper) -ceq (Pixels $fresh.Paper)) 'Pixels differ'
        }
    }
    Check 'invalid stroke inputs reject without changing history or pixels' {
        $e = Engine; Draw $e 3; $before = Pixels $e.Paper
        foreach ($bad in @([float]::NaN,[float]::PositiveInfinity,[float]::MaxValue)) {
            Reject { $e.BeginStroke([ConnectPiece.SumiEngine.Core.InkPoint]::new($bad,0,0,1),[ConnectPiece.SumiEngine.Core.InkBrushSettings]::Slash,3) }
        }
        $settings = [ConnectPiece.SumiEngine.Core.InkBrushSettings]::Slash; $settings.radius=257
        Reject { $e.BeginStroke([ConnectPiece.SumiEngine.Core.InkPoint]::new(10,10,0,1),$settings,3) }
        Assert ($e.Session.strokes.Count -eq 1) 'Invalid begin changed history'
        Assert ($before -ceq (Pixels $e.Paper)) 'Invalid begin changed pixels'
    }
    Check 'extreme movement rejects before recording or rendering' {
        $e = Engine
        $e.BeginStroke([ConnectPiece.SumiEngine.Core.InkPoint]::new(10,10,0,1),[ConnectPiece.SumiEngine.Core.InkBrushSettings]::Slash,3)
        $before = Pixels $e.Paper
        Reject { $e.AppendPoint([ConnectPiece.SumiEngine.Core.InkPoint]::new(5000,10,.1,1)) }
        Assert ($before -ceq (Pixels $e.Paper)) 'Invalid move changed pixels'
        $e.EndStroke(); Assert ($e.Session.strokes[0].points.Count -eq 1) 'Invalid move changed history'
    }
    Check 'negative water clamps' {
        $p = Paper; $p.Deposit(16,16,8,.8,-10,.2,3)
        for ($y=0;$y -lt 64;$y++) { for ($x=0;$x -lt 64;$x++) { Assert ($p.MoistureAt($x,$y) -eq 0) 'Negative moisture' } }
    }
    Check 'negative simulation rates reject before mutation' {
        foreach ($field in @('Diffusion','WaterDiffusion','Evaporation','Binding')) {
            $p = Paper; $p.Deposit(16,16,8,.8,.8,.2,3); $before = Pixels $p
            $p.$field = -1; Reject { $p.Step(.1) }
            Assert ($before -ceq (Pixels $p)) 'Mutated invalid step'
        }
    }
    Check 'nonfinite deposit fields reject' {
        foreach ($bad in @([float]::NaN,[float]::PositiveInfinity,[float]::NegativeInfinity)) {
            for ($field=0;$field -lt 6;$field++) {
                $p = Paper; $v = [float[]]@(16,16,8,.8,.8,.2); $v[$field]=$bad
                Reject { $p.Deposit($v[0],$v[1],$v[2],$v[3],$v[4],$v[5],3) }
                Assert ($p.TotalPigment() -eq 0) 'Invalid deposit mutated paper'
            }
        }
    }
    Check 'nonfinite and excessive step reject' {
        foreach ($bad in @([float]::NaN,[float]::PositiveInfinity,[float]::MaxValue)) {
            $p = Paper; Reject { $p.Step($bad) }; Assert ($p.TotalPigment() -eq 0) 'Invalid step mutated paper'
        }
    }
    Check 'negative time safe no-op' {
        $p = Paper; $p.Deposit(16,16,8,.8,.8,.2,3); $before=Pixels $p
        $p.Step(-1); Assert ($before -ceq (Pixels $p)) 'Negative time mutated paper'
    }
    Check 'drying conserves pigment and remains finite/nonnegative' {
        $p = Paper; $p.Deposit(18,12,9,.55,.95,.1,12); $before=$p.TotalPigment()
        for ($i=0;$i -lt 100;$i++) { $p.Step(.1) }
        Assert ([Math]::Abs($p.TotalPigment()-$before) -le ($before*.002+.01)) 'Pigment mass changed'
        for ($y=0;$y -lt 64;$y++) { for ($x=0;$x -lt 64;$x++) {
            foreach ($v in @($p.PigmentAt($x,$y),$p.MoistureAt($x,$y))) {
                Assert (($v -ge 0) -and ![float]::IsNaN($v) -and ![float]::IsInfinity($v)) 'Invalid cell'
            }
        } }
    }
    Check 'outside and extreme finite deposits bounded' {
        $p = Paper; $p.Deposit(-100,-100,20,.8,.8,.2,4)
        $p.Deposit([float]::MaxValue,[float]::MaxValue,1,.8,.8,.2,4)
        Assert ($p.TotalPigment() -eq 0) 'Outside deposit changed paper'
        Assert (($p.PigmentAt(-1,0) -eq 0) -and ($p.MoistureAt(64,64) -eq 0)) 'Outside reads'
        $p.Deposit(16,16,[float]::MaxValue,.8,.8,.2,3)
        Assert (($p.TotalPigment() -gt 0) -and ![float]::IsNaN($p.TotalPigment())) 'Radius overflow'
    }
    Check 'invalid dimensions and render buffers reject' {
        Reject { [ConnectPiece.SumiEngine.Core.InkPaper]::new(15,32) }
        Reject { [ConnectPiece.SumiEngine.Core.InkPaper]::new(32,2049) }
        $p = Paper; Reject { $p.RenderRgba($null) }; Reject { $p.RenderRgba([byte[]]::new(4)) }
    }
    Write-Output "TOTAL $script:passed passed / $script:failed failed"
    if ($script:failed -gt 0) { exit 1 }
    exit 0
} catch { Write-Error -ErrorAction Continue $_; exit 1 }
