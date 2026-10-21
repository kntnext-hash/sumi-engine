[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repo 'UnitySandbox'
if (!(Test-Path -LiteralPath (Join-Path $project 'ProjectSettings/ProjectVersion.txt'))) {
    throw 'Run Setup-UnitySandbox.ps1 with the installed Unity 6.3 LTS Editor first. No project was created.'
}
if (Test-Path -LiteralPath (Join-Path $project 'Temp/UnityLockfile')) {
    throw 'Close UnitySandbox before installing the probe.'
}
$destination = Join-Path $project 'Assets/CodexAutomation'
if (Test-Path -LiteralPath $destination) { throw 'Probe destination already exists; inspect rather than overwrite.' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'UnityAutomation~') -Destination $destination -Recurse
Write-Output 'Probe sources installed. Compilation, scene generation, PlayMode and live Editor QA are still required.'
