[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

$toolRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $toolRoot

$requiredFiles = @(
    'docs\p7\P7.8H-Legacy-Runtime-Quarantine.md',
    'database\sql\p7\011_legacy_runtime_object_inventory.sql',
    'config-samples\runtime-legacy-reference-allowlist.sample.json',
    'tools\runtime\New-LegacyRuntimeReferenceInventory.ps1',
    'tools\runtime\Test-LegacyRuntimeReferenceBoundary.ps1'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw ("Required P7.8H file is missing: {0}" -f $relativePath)
    }
}

$allowlistPath = Join-Path $repoRoot 'config-samples\runtime-legacy-reference-allowlist.sample.json'
$allowlist = Get-Content -LiteralPath $allowlistPath -Raw | ConvertFrom-Json
if ($null -eq $allowlist.legacyTerms) {
    throw "Legacy reference allowlist is missing legacyTerms."
}
if ($null -eq $allowlist.allowedPathFragments) {
    throw "Legacy reference allowlist is missing allowedPathFragments."
}

$legacyTermCount = @($allowlist.legacyTerms).Count
if ($legacyTermCount -lt 5) {
    throw "Legacy reference allowlist must include the known GUID-era runtime table names."
}

$sqlPath = Join-Path $repoRoot 'database\sql\p7\011_legacy_runtime_object_inventory.sql'
$sqlText = Get-Content -LiteralPath $sqlPath -Raw

$requiredMetadataTerms = @('sys.tables', 'sys.schemas', 'sys.columns', 'sys.types')
foreach ($requiredTerm in $requiredMetadataTerms) {
    if ($sqlText.IndexOf($requiredTerm, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw ("SQL inventory is missing required metadata source: {0}" -f $requiredTerm)
    }
}

$requiredOutputTerms = @('ObjectCategory', 'IsLegacyRuntimeObject', 'CanonicalRuntimeObject', 'ObjectExists')
foreach ($requiredTerm in $requiredOutputTerms) {
    if ($sqlText.IndexOf($requiredTerm, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw ("SQL inventory is missing required output/classification term: {0}" -f $requiredTerm)
    }
}

$requiredCurrentObjectNames = @('WorkItems', 'ManifestRows')
foreach ($requiredTerm in $requiredCurrentObjectNames) {
    if ($sqlText.IndexOf($requiredTerm, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw ("SQL inventory is missing current runtime object name: {0}" -f $requiredTerm)
    }
}

$requiredLegacyObjectNames = @('MigrationWorkItems', 'MigrationManifestRows', 'MigrationManifestRecords')
foreach ($requiredTerm in $requiredLegacyObjectNames) {
    if ($sqlText.IndexOf($requiredTerm, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw ("SQL inventory is missing legacy runtime object name: {0}" -f $requiredTerm)
    }
}

$forbiddenSqlTerms = @('DROP TABLE', 'ALTER TABLE', 'INSERT INTO', 'UPDATE ', 'DELETE FROM', 'TRUNCATE TABLE')
foreach ($forbiddenTerm in $forbiddenSqlTerms) {
    if ($sqlText.IndexOf($forbiddenTerm, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw ("SQL inventory must be read-only but contains: {0}" -f $forbiddenTerm)
    }
}

$runtimeScripts = @(
    'tools\runtime\New-LegacyRuntimeReferenceInventory.ps1',
    'tools\runtime\Test-LegacyRuntimeReferenceBoundary.ps1'
)

foreach ($relativeScript in $runtimeScripts) {
    $scriptPath = Join-Path $repoRoot $relativeScript
    $scriptText = Get-Content -LiteralPath $scriptPath -Raw
    if ($scriptText.IndexOf('$MyInvocation.ScriptName', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw ("Runtime script uses fragile MyInvocation.ScriptName pattern: {0}" -f $relativeScript)
    }
    if ($scriptText.IndexOf('PackageReference Version=', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw ("Runtime script contains inline PackageReference Version text: {0}" -f $relativeScript)
    }
}

Write-Host "P7.8H legacy runtime quarantine drop-in validation passed."
