[CmdletBinding()]
param([string]$BuildFolder='Builds/Bokumyaku-profile-v27')
$ErrorActionPreference='Stop'
$repo=Split-Path -Parent $PSScriptRoot
$folder=Join-Path $repo $BuildFolder
$out=Join-Path $repo ('logs/profiler-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $out | Out-Null
$raw=Join-Path $out 'startup.raw'
$argsList=@('--bokumyaku-smoke-output',('"'+$out+'"'),'--bokumyaku-settled-start','-screen-width','540','-screen-height','960','-screen-fullscreen','0','-logFile',('"'+(Join-Path $out 'player.log')+'"'),'-profiler-log-file',('"'+$raw+'"'),'-profiler-capture-frame-count','700','-profiler-maxusedmemory','134217728')
# Visible Player launch explicitly authorized by the user. Isolated QA save only.
$p=Start-Process -FilePath (Join-Path $folder 'Bokumyaku.exe') -WorkingDirectory $folder -ArgumentList $argsList -WindowStyle Normal -PassThru
if(!$p.WaitForExit(90000)){Stop-Process -Id $p.Id; throw 'Profiler probe exceeded 90 seconds'}
if($p.ExitCode -ne 0){throw "Profiler probe exit $($p.ExitCode)"}
if(!(Test-Path $raw)){throw 'No profiler capture produced'}
$report=Get-Content (Join-Path $out 'player-report.json') -Raw | ConvertFrom-Json
if(!$report.passed){throw 'Probe did not pass'}
Copy-Item (Join-Path $out 'player-report.json') (Join-Path $repo 'docs/evidence/profile-player-v27.json')
[pscustomobject]@{raw=$raw;bytes=(Get-Item $raw).Length;sha256=(Get-FileHash $raw).Hash;frameLimit=700;development=$true;deepProfiling=$false;log=(Join-Path $out 'player.log');note='Development instrumentation changes timings. Raw stays local; inspect thread samples before conclusions.'} | ConvertTo-Json | Set-Content (Join-Path $repo 'docs/evidence/profile-capture-v27.json')
Write-Output $raw

