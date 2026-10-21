param(
  [string]$Destination = "$HOME\source\repos\sumi-engine",
  [switch]$Push
)
$ErrorActionPreference = 'Stop'
$Source = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$Repo = 'https://github.com/kntnext-hash/sumi-engine.git'
if (!(Get-Command git -ErrorAction SilentlyContinue)) { throw 'Git for Windowsが必要です。' }
if (!(Test-Path (Join-Path $Destination '.git'))) {
  New-Item -ItemType Directory -Path (Split-Path $Destination -Parent) -Force | Out-Null
  git clone $Repo $Destination
  if ($LASTEXITCODE -ne 0) { throw 'git clone に失敗。GitHub認証を確認してください。' }
}
$Files = Get-ChildItem -LiteralPath $Source -File -Recurse -Force | Where-Object { $_.FullName -notmatch '[\\/]\.git[\\/]' }
foreach ($file in $Files) {
  $Relative = $file.FullName.Substring($Source.Length).TrimStart([char]'\', [char]'/')
  $Target = Join-Path $Destination $Relative
  $Parent = Split-Path -Path $Target -Parent
  if (!(Test-Path $Parent)) { New-Item -ItemType Directory -Path $Parent -Force | Out-Null }
  Copy-Item -LiteralPath $file.FullName -Destination $Target -Force
}
Push-Location $Destination
try {
  git status -sb
  if ($Push) {
    git add .
    git commit -m 'Bootstrap SUMI ENGINE v0.1 and Codex handoff'
    if ($LASTEXITCODE -ne 0) { Write-Host 'コミットが不要か、Git設定が必要です。' }
    git push -u origin main
    if ($LASTEXITCODE -ne 0) { throw 'pushに失敗。git status とログを確認してください。' }
  } else {
    Write-Host 'コミットは未実行。反映するなら -Push を付けて再実行してください。'
  }
} finally { Pop-Location }
Write-Host "Ready: $Destination"
