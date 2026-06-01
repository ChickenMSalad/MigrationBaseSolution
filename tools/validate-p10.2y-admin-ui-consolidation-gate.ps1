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

function Join-RepoPath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $RelativePath
    )

    $parts = @($RelativePath -split '[\\/]') | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    $current = $repoRoot
    foreach ($part in $parts) {
        $current = [System.IO.Path]::Combine($current, $part)
    }
    return $current
}

$requiredFiles = @(
    'docs/p10/P10.2Y-Admin-UI-Consolidation-Gate.md',
    'docs/operations/admin-ui-consolidation-gate.md',
    'config-samples/p10-admin-ui-consolidation-gate.sample.json',
    'tools/runtime/New-P102AdminUiConsolidationReport.ps1'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-RepoPath -RelativePath $relativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw ('Required P10.2Y file is missing: {0}' -f $relativePath)
    }
}

$scriptsToParse = @(
    'tools/runtime/New-P102AdminUiConsolidationReport.ps1'
)

foreach ($relativeScript in $scriptsToParse) {
    $scriptPath = Join-RepoPath -RelativePath $relativeScript
    if (-not (Test-Path -LiteralPath $scriptPath -PathType Leaf)) {
        throw ('Script file is missing: {0}' -f $relativeScript)
    }

    $parseErrors = $null
    $scriptText = Get-Content -LiteralPath $scriptPath -Raw
    [System.Management.Automation.PSParser]::Tokenize($scriptText, [ref]$parseErrors) | Out-Null
    if ($null -ne $parseErrors -and @($parseErrors).Count -gt 0) {
        $message = (@($parseErrors) | ForEach-Object { $_.Message }) -join '; '
        throw ('PowerShell parser errors in {0}: {1}' -f $relativeScript, $message)
    }
}

$configPath = Join-RepoPath -RelativePath 'config-samples/p10-admin-ui-consolidation-gate.sample.json'
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
foreach ($propertyName in @('canonicalAdminUiPath', 'featureSourcePath', 'reportOutputPath', 'canonicalFeatureFolderRecommendation', 'migratedFeatureSurfaces')) {
    if ($null -eq $config.PSObject.Properties[$propertyName]) {
        throw ('P10.2Y config is missing property: {0}' -f $propertyName)
    }
}

if ([string]$config.canonicalAdminUiPath -ne 'src/Admin/Migration.Admin.Web') {
    throw 'canonicalAdminUiPath must remain src/Admin/Migration.Admin.Web.'
}
if ([string]$config.featureSourcePath -ne 'apps/migration-admin-ui') {
    throw 'featureSourcePath must remain apps/migration-admin-ui.'
}

$checks = @(
    [pscustomobject]@{ Path = 'docs/p10/P10.2Y-Admin-UI-Consolidation-Gate.md'; Terms = @('src/Admin/Migration.Admin.Web', 'apps/migration-admin-ui', 'src/features') },
    [pscustomobject]@{ Path = 'docs/operations/admin-ui-consolidation-gate.md'; Terms = @('feature-source', 'src/Admin/Migration.Admin.Web', 'src/features') },
    [pscustomobject]@{ Path = 'tools/runtime/New-P102AdminUiConsolidationReport.ps1'; Terms = @('Get-RelativeFileInventory', 'migratedFeatureSurfaces', 'canonicalFeatureFolderRecommendation') }
)

foreach ($check in $checks) {
    $pathProperty = $check.PSObject.Properties['Path']
    $termsProperty = $check.PSObject.Properties['Terms']
    if ($null -eq $pathProperty -or $null -eq $termsProperty) {
        throw 'Validator check entry is malformed.'
    }

    $relativePath = [string]$pathProperty.Value
    $fullPath = Join-RepoPath -RelativePath $relativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw ('Required checked file is missing: {0}' -f $relativePath)
    }

    $text = Get-Content -LiteralPath $fullPath -Raw
    foreach ($term in @($termsProperty.Value)) {
        $termText = [string]$term
        if ([string]::IsNullOrWhiteSpace($termText)) {
            throw ('Expected term is empty for file: {0}' -f $relativePath)
        }
        if ($text.IndexOf($termText, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
            throw ('File {0} is missing expected term: {1}' -f $relativePath, $termText)
        }
    }
}

Write-Host 'P10.2Y Admin UI consolidation gate validation passed.'
