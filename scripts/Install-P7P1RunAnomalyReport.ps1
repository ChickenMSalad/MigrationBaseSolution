[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RepoRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $RepoRoot)) {
    throw ('RepoRoot does not exist: ' + $RepoRoot)
}

$sourceRoot = Split-Path -Parent $PSScriptRoot
$repoScripts = Join-Path $RepoRoot 'scripts'
if (-not (Test-Path -LiteralPath $repoScripts)) {
    New-Item -ItemType Directory -Path $repoScripts -Force | Out-Null
}

$files = @(
    'Invoke-P7RunAnomalyReport.ps1',
    'Test-P7P1RunAnomalyReport.ps1'
)

foreach ($fileName in $files) {
    $sourcePath = Join-Path (Join-Path $sourceRoot 'scripts') $fileName
    $targetPath = Join-Path $repoScripts $fileName

    if (-not (Test-Path -LiteralPath $sourcePath)) {
        throw ('Installer payload missing: ' + $sourcePath)
    }

    $sourceFull = [System.IO.Path]::GetFullPath($sourcePath)
    $targetFull = [System.IO.Path]::GetFullPath($targetPath)

    if ([string]::Equals($sourceFull, $targetFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        Write-Host ('Script already installed: ' + $fileName)
        continue
    }

    if (Test-Path -LiteralPath $targetPath) {
        $backupPath = $targetPath + '.bak'
        Copy-Item -LiteralPath $targetPath -Destination $backupPath -Force
        Write-Host ('Backed up ' + $targetPath)
    }

    Copy-Item -LiteralPath $sourcePath -Destination $targetPath -Force
    Write-Host ('Installed ' + $fileName)
}

Write-Host 'P7 run anomaly report scripts installed. No source files changed.'
