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
    'docs\p10\P10.1A-Site-Cloud-Readiness-Inventory.md',
    'config-samples\p10-site-cloud-readiness.sample.json',
    'tools\runtime\Test-P101SiteCloudReadinessInventory.ps1'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw ('Required P10.1A file is missing: {0}' -f $relativePath)
    }
}

$scriptsToParse = @(
    'tools\runtime\Test-P101SiteCloudReadinessInventory.ps1'
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

$configPath = Join-Path $repoRoot 'config-samples\p10-site-cloud-readiness.sample.json'
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
foreach ($propertyName in @('frontendPath', 'adminApiPath', 'requiredCloudSettings', 'expectedRuntimeState')) {
    if ($null -eq $config.PSObject.Properties[$propertyName]) {
        throw ('Config sample is missing property: {0}' -f $propertyName)
    }
}

$docPath = Join-Path $repoRoot 'docs\p10\P10.1A-Site-Cloud-Readiness-Inventory.md'
$docText = Get-Content -LiteralPath $docPath -Raw
foreach ($term in @('apps/migration-admin-ui', 'src/Core/Migration.Admin.Api', 'migration.WorkItems')) {
    if ($docText.IndexOf($term, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw ('P10.1A doc is missing expected term: {0}' -f $term)
    }
}

Write-Host 'P10.1A site cloud readiness inventory validation passed.'
