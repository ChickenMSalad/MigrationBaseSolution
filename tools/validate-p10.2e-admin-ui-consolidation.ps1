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
    @('docs','p10','P10.2E-Admin-UI-Consolidation-Plan.md'),
    @('docs','operations','admin-ui-consolidation.md'),
    @('apps','migration-admin-ui','README.CANONICALIZATION.md'),
    @('config-samples','p10-admin-ui-consolidation.sample.json'),
    @('tools','runtime','New-P102AdminUiConsolidationInventory.ps1')
)

foreach ($relativeParts in $requiredFiles) {
    $fullPath = $repoRoot
    foreach ($part in $relativeParts) {
        $fullPath = [System.IO.Path]::Combine($fullPath, $part)
    }
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw ('Required P10.2E file is missing: {0}' -f ($relativeParts -join '/'))
    }
}

$scriptsToParse = @(
    'tools\runtime\New-P102AdminUiConsolidationInventory.ps1'
)

foreach ($relativeScript in $scriptsToParse) {
    $scriptPath = [System.IO.Path]::Combine($repoRoot, $relativeScript)

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

$configPath = [System.IO.Path]::Combine($repoRoot, 'config-samples', 'p10-admin-ui-consolidation.sample.json')
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
foreach ($propertyName in @('canonicalAdminUiPath', 'featureSourcePath', 'inventoryOutputPath', 'featureFamilies')) {
    if ($null -eq $config.PSObject.Properties[$propertyName]) {
        throw ('P10.2E config is missing property: {0}' -f $propertyName)
    }
}
if ($config.canonicalAdminUiPath -ne 'src/Admin/Migration.Admin.Web') {
    throw 'P10.2E canonicalAdminUiPath must remain src/Admin/Migration.Admin.Web.'
}
if ($config.featureSourcePath -ne 'apps/migration-admin-ui') {
    throw 'P10.2E featureSourcePath must remain apps/migration-admin-ui.'
}

$docs = @(
    [System.IO.Path]::Combine($repoRoot, 'docs', 'p10', 'P10.2E-Admin-UI-Consolidation-Plan.md'),
    [System.IO.Path]::Combine($repoRoot, 'docs', 'operations', 'admin-ui-consolidation.md'),
    [System.IO.Path]::Combine($repoRoot, 'apps', 'migration-admin-ui', 'README.CANONICALIZATION.md')
)
foreach ($docPath in $docs) {
    $text = Get-Content -LiteralPath $docPath -Raw
    foreach ($term in @('src/Admin/Migration.Admin.Web', 'apps/migration-admin-ui')) {
        if ($text.IndexOf($term, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
            throw ('Documentation file is missing consolidation term {0}: {1}' -f $term, $docPath)
        }
    }
}

Write-Host 'P10.2E Admin UI consolidation validation passed.'
