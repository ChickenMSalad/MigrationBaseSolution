[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$toolRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($toolRoot)) {
    if (-not [string]::IsNullOrWhiteSpace($PSCommandPath)) {
        $toolRoot = Split-Path -Parent $PSCommandPath
    }
}

if ([string]::IsNullOrWhiteSpace($toolRoot)) {
    throw 'Unable to resolve validator root.'
}

$repoRoot = Split-Path -Parent $toolRoot

$requiredFiles = @(
    'docs\p10\P10.2B-Admin-Api-Dashboard-Endpoint-Verification.md',
    'config-samples\p10-admin-api-dashboard-endpoint-verification.sample.json',
    'tools\runtime\Test-P102AdminApiDashboardEndpointContract.ps1',
    'tools\runtime\New-P102AdminApiDashboardCloudVerificationCommands.ps1'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw ('Required P10.2B file is missing: {0}' -f $relativePath)
    }
}

$scriptsToParse = @(
    'tools\runtime\Test-P102AdminApiDashboardEndpointContract.ps1',
    'tools\runtime\New-P102AdminApiDashboardCloudVerificationCommands.ps1'
)

foreach ($relativeScript in $scriptsToParse) {
    $scriptPath = Join-Path $repoRoot $relativeScript
    $parseErrors = $null
    [System.Management.Automation.PSParser]::Tokenize((Get-Content -LiteralPath $scriptPath -Raw), [ref]$parseErrors) | Out-Null
    if ($null -ne $parseErrors -and @($parseErrors).Count -gt 0) {
        $message = (@($parseErrors) | ForEach-Object { $_.Message }) -join '; '
        throw ('PowerShell parser errors in {0}: {1}' -f $relativeScript, $message)
    }
}

$contractScript = Join-Path $repoRoot 'tools\runtime\Test-P102AdminApiDashboardEndpointContract.ps1'
& $contractScript -RepoRoot $repoRoot

$configPath = Join-Path $repoRoot 'config-samples\p10-admin-api-dashboard-endpoint-verification.sample.json'
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
foreach ($propertyName in @('resourceGroup', 'adminApiAppName', 'baseUrl', 'requiredSettings', 'dashboardEndpoints')) {
    if ($null -eq $config.PSObject.Properties[$propertyName]) {
        throw ('Sample config is missing property: {0}' -f $propertyName)
    }
}

$endpointCount = @($config.dashboardEndpoints).Count
if ($endpointCount -lt 2) {
    throw 'Sample config must include both runtime dashboard endpoints.'
}

Write-Host 'P10.2B Admin API dashboard endpoint verification validation passed.'
