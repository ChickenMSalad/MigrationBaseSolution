[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}
$repoRoot = Split-Path -Parent $scriptRoot

$requiredFiles = @(
    'docs/p7/P7.8C-Runtime-AppSettings-Normalization.md',
    'config-samples/appsettings.SqlServiceBusRuntime.canonical.azure.sample.json',
    'tools/runtime/Get-RuntimeAppSettingsCleanupPlan.ps1',
    'tools/runtime/New-RuntimeAppSettingsSetCommand.ps1',
    'tools/runtime/New-RuntimeAppSettingsDeleteCommand.ps1'
)

$missing = @()
foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        $missing += $relativePath
    }
}

if ($missing.Count -gt 0) {
    throw ('P7.8C validation failed. Missing files: ' + ($missing -join ', '))
}

$jsonPath = Join-Path $repoRoot 'config-samples/appsettings.SqlServiceBusRuntime.canonical.azure.sample.json'
$json = Get-Content -LiteralPath $jsonPath -Raw | ConvertFrom-Json
foreach ($sectionName in @('shared', 'dispatcher', 'executor')) {
    if (-not $json.PSObject.Properties[$sectionName]) {
        throw "P7.8C validation failed. Missing JSON section: $sectionName"
    }
}

$scriptPaths = @(
    'tools/runtime/Get-RuntimeAppSettingsCleanupPlan.ps1',
    'tools/runtime/New-RuntimeAppSettingsSetCommand.ps1',
    'tools/runtime/New-RuntimeAppSettingsDeleteCommand.ps1'
)
foreach ($relativePath in $scriptPaths) {
    $fullPath = Join-Path $repoRoot $relativePath
    $content = Get-Content -LiteralPath $fullPath -Raw
    if ($content -match '\$path:') {
        throw "P7.8C validation failed. Unsafe variable interpolation pattern found in $relativePath"
    }
    if ($content -notmatch 'Set-StrictMode -Version 2\.0') {
        throw "P7.8C validation failed. Script does not enable StrictMode 2.0: $relativePath"
    }
}

Write-Host 'P7.8C runtime appsettings normalization validation passed.'
