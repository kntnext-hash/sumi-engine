param(
  [string]$Destination = "$HOME\source\repos\sumi-engine",
  [switch]$Push
)
$ErrorActionPreference = 'Stop'
$Source = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$Repo = 'https://github.com/kntnext-hash/sumi-engine.git'
if (!(Get-Command git -ErrorAction SilentlyContinue)) { throw 'Git for Windows is required.' }
$Destination = [IO.Path]::GetFullPath($Destination).TrimEnd([char]'\', [char]'/')
if ($Destination -eq $Source -or $Destination.StartsWith($Source + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
  throw 'Destination must not equal or be inside the source.'
}
# Reject reparse points so lexical containment checks cannot be bypassed.
$ancestor = $Destination
while ($ancestor) {
  if ((Test-Path -LiteralPath $ancestor) -and ((Get-Item -LiteralPath $ancestor).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw "Destination contains a symlink/junction: $ancestor" }
  $ancestor = Split-Path -Path $ancestor -Parent
}
function Invoke-Git([string[]]$GitArgs) {
  $output = & git @GitArgs
  if ($LASTEXITCODE -ne 0) { throw "git failed ($LASTEXITCODE): $($GitArgs -join ' ')" }
  return $output
}
$sourceRoot = Invoke-Git @('-C', $Source, 'rev-parse', '--show-toplevel')
if ([IO.Path]::GetFullPath($sourceRoot) -ne $Source) { throw 'Source must be the Git repository root.' }
# Git's ignore rules exclude generated caches; quoted unusual filenames are rejected.
$files = @(Invoke-Git @('-C', $Source, '-c', 'core.quotepath=false', 'ls-files', '--cached', '--others', '--exclude-standard')) | Select-Object -Unique
if (!(Test-Path -LiteralPath (Join-Path $Destination '.git'))) {
  if (Test-Path -LiteralPath $Destination) { throw 'Existing destination has no .git; refusing to clone over it.' }
  New-Item -ItemType Directory -Path (Split-Path -Path $Destination -Parent) -Force | Out-Null
  Invoke-Git @('clone', $Repo, $Destination) | Out-Host
}
$targetRoot = Invoke-Git @('-C', $Destination, 'rev-parse', '--show-toplevel')
if ([IO.Path]::GetFullPath($targetRoot) -ne $Destination) { throw 'Destination must be a Git repository root.' }
$origin = Invoke-Git @('-C', $Destination, 'remote', 'get-url', 'origin')
if ($origin -notin @($Repo, 'https://github.com/kntnext-hash/sumi-engine', 'git@github.com:kntnext-hash/sumi-engine.git', 'ssh://git@github.com/kntnext-hash/sumi-engine.git')) { throw 'Destination origin does not match kntnext-hash/sumi-engine.' }
$dirty = @(Invoke-Git @('-C', $Destination, 'status', '--porcelain', '--untracked-files=all'))
if ($dirty.Count -ne 0) { throw 'Destination has uncommitted files. Preserve/commit them before importing.' }
$copyPlan = @()
foreach ($relative in $files) {
  if ($relative.StartsWith('"')) { throw "Unsupported quoted filename: $relative" }
  $sourceFile = Join-Path $Source $relative
  if (!(Test-Path -LiteralPath $sourceFile -PathType Leaf)) { throw "Source file is missing or not a regular file: $relative" }
  if ((Get-Item -LiteralPath $sourceFile).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Source symlink is unsupported: $relative" }
  $target = Join-Path $Destination $relative
  $parent = Split-Path -Path $target -Parent
  while ($parent -and $parent -ne $Destination) {
    if (Test-Path -LiteralPath $parent) {
      $item = Get-Item -LiteralPath $parent
      if (!$item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw "Destination parent conflicts: $relative" }
    }
    $parent = Split-Path -Path $parent -Parent
  }
  if (Test-Path -LiteralPath $target) {
    $item = Get-Item -LiteralPath $target
    if ($item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw "Destination path conflicts: $relative" }
    if ((Get-FileHash -LiteralPath $sourceFile -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash) { throw "Destination content differs; no files copied: $relative" }
  } else {
    $copyPlan += [pscustomobject]@{ Source = $sourceFile; Target = $target }
  }
}
# All conflicts are checked before the first copy. Copy never overwrites a file.
foreach ($item in $copyPlan) {
  New-Item -ItemType Directory -Path (Split-Path -Path $item.Target -Parent) -Force | Out-Null
  [IO.File]::Copy($item.Source, $item.Target, $false)
}
Invoke-Git @('-C', $Destination, 'status', '-sb') | Out-Host
if ($Push) {
  $branch = Invoke-Git @('-C', $Destination, 'symbolic-ref', '--short', 'HEAD')
  if ($branch -ne 'main') { throw 'Import copied safely, but push requires the main branch. Inspect before switching branches.' }
  if ($copyPlan.Count -gt 0) {
    Invoke-Git @('-C', $Destination, 'add', '--all') | Out-Host
    Invoke-Git @('-C', $Destination, 'commit', '-m', 'Bootstrap SUMI ENGINE v0.1 and Codex handoff') | Out-Host
  }
  Invoke-Git @('-C', $Destination, 'push', '-u', 'origin', 'main') | Out-Host
} else {
  Write-Host 'Copied only missing files. Review changes, then commit and push manually; rerunning against a dirty destination is refused.'
}
Write-Host "Ready: $Destination"
