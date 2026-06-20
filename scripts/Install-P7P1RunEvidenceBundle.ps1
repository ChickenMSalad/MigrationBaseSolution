param(
    [Parameter(Mandatory = $true)]
    [string]$RepoRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $RepoRoot)) {
    throw 'RepoRoot does not exist.'
}

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$targetScripts = Join-Path $RepoRoot 'scripts'
if (-not (Test-Path -LiteralPath $targetScripts)) {
    New-Item -ItemType Directory -Path $targetScripts -Force | Out-Null
}

$files = @(
    'Invoke-P7RunEvidenceBundle.ps1',
    'Test-P7P1RunEvidenceBundle.ps1'
)

foreach ($name in $files) {
    $source = Join-Path $scriptRoot $name
    if (-not (Test-Path -LiteralPath $source)) {
        throw ('Installer payload missing: ' + $name)
    }

    $target = Join-Path $targetScripts $name
    Copy-Item -LiteralPath $source -Destination $target -Force
    Write-Host ('Installed ' + $target)
}

Write-Host 'P7 P1 run evidence bundle scripts installed. No source files changed.'
