# Run after building menu v5. Two real processes share only an isolated QA save.
[CmdletBinding()]
param([string]$BuildFolder='Builds/Bokumyaku-menu-v5')
$ErrorActionPreference='Stop'
$repo=Split-Path -Parent $PSScriptRoot
$folder=[IO.Path]::GetFullPath((Join-Path $repo $BuildFolder))
$exe=Join-Path $folder 'Bokumyaku.exe'
if(!(Test-Path -LiteralPath $exe)){throw "Build missing: $exe"}
$out=Join-Path $repo ('logs/bokumyaku-player-menu-v5-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $out | Out-Null
$launches=@()
foreach($resume in @($false,$true)){
    $name=if($resume){'player-resume'}else{'player'}
    $arguments=@('--bokumyaku-smoke-output',('"'+$out+'"'),'-logFile',('"'+(Join-Path $out ($name+'.log'))+'"'),'-screen-width','1080','-screen-height','2400','-screen-fullscreen','0')
    if($resume){$arguments+='--bokumyaku-resume-check'}
    $timer=[Diagnostics.Stopwatch]::StartNew()
    $process=Start-Process -FilePath $exe -WorkingDirectory $folder -ArgumentList $arguments -PassThru -WindowStyle Hidden
    $timeout=if($resume){40}else{165}
    while(!$process.WaitForExit(1000)){
        if($timer.Elapsed.TotalSeconds -gt $timeout){Stop-Process -Id $process.Id;throw "Probe timeout: $name"}
    }
    $timer.Stop()
    $report=Join-Path $out ($name+'-report.json')
    if(!(Test-Path -LiteralPath $report)){throw "Report missing: $report / exit $($process.ExitCode)"}
    $result=Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
    Copy-Item -LiteralPath $report -Destination (Join-Path $repo ('docs/evidence/bokumyaku-'+$name+'-menu-v5.json'))
    $launches += [pscustomobject]@{name=$name;exitCode=$process.ExitCode;elapsedSeconds=[Math]::Round($timer.Elapsed.TotalSeconds,3);passed=$result.passed}
    $launches | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $repo 'docs/evidence/bokumyaku-player-launch-menu-v5.json') -Encoding UTF8
    if($process.ExitCode -ne 0 -or !$result.passed){throw "Probe failed: $name. Evidence copied; logs=$out"}
}
$assemblies=Get-ChildItem -LiteralPath (Join-Path $folder 'Bokumyaku_Data/Managed') -Filter 'Bokumyaku.*.dll' | ForEach-Object {[pscustomobject]@{name=$_.Name;bytes=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName).Hash}}
[pscustomobject]@{path=$BuildFolder+'/Bokumyaku.exe';bytes=(Get-Item -LiteralPath $exe).Length;sha256=(Get-FileHash -LiteralPath $exe).Hash;unity='6000.3.26f1';scene='Assets/BokumyakuPrototype/Scenes/Bokumyaku.unity';note='Keep accompanying Data, UnityPlayer and MonoBleedingEdge files. Windows Mono evidence; not mobile.';gameAssemblies=@($assemblies)} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $repo 'docs/evidence/bokumyaku-artifact-menu-v5.json') -Encoding UTF8
$launches | Format-Table
Write-Output "Logs: $out"

