[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RepoRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repo = [System.IO.Path]::GetFullPath($RepoRoot)
if (-not (Test-Path -LiteralPath $repo)) {
    throw ('RepoRoot does not exist: ' + $repo)
}

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$source = Join-Path $scriptRoot 'Watch-P7RunProgress.ps1'
if (-not (Test-Path -LiteralPath $source)) {
    throw ('Watch script source not found: ' + $source)
}

$targetDir = Join-Path $repo 'scripts'
if (-not (Test-Path -LiteralPath $targetDir)) {
    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
}

$target = Join-Path $targetDir 'Watch-P7RunProgress.ps1'
if (Test-Path -LiteralPath $target) {
    Copy-Item -LiteralPath $target -Destination ($target + '.p7-progress-watch.bak') -Force
}

Copy-Item -LiteralPath $source -Destination $target -Force
Write-Host 'Installed scripts\Watch-P7RunProgress.ps1'
Write-Host 'P7 P1 run progress watch installed. No source files changed.'
