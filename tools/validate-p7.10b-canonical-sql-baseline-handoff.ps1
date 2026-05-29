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
    'docs\p7\P7.10B-Canonical-SQL-Baseline-Handoff.md',
    'database\sql\p7\022_runtime_canonical_sql_baseline_handoff_inventory.sql',
    'database\sql\p7\023_runtime_canonical_sql_baseline_handoff_validator.sql',
    'config-samples\runtime-sql-baseline-handoff.sample.json',
    'tools\runtime\Invoke-RuntimeCanonicalSqlBaselineHandoffInventory.ps1',
    'tools\runtime\Invoke-RuntimeCanonicalSqlBaselineHandoffValidator.ps1'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = [System.IO.Path]::Combine($repoRoot, $relativePath)
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw ('Required P7.10B file is missing: {0}' -f $relativePath)
    }
}

$scriptsToParse = @(
    'tools\runtime\Invoke-RuntimeCanonicalSqlBaselineHandoffInventory.ps1',
    'tools\runtime\Invoke-RuntimeCanonicalSqlBaselineHandoffValidator.ps1'
)

foreach ($relativeScript in $scriptsToParse) {
    $scriptPath = [System.IO.Path]::Combine($repoRoot, $relativeScript)
    $parseErrors = $null
    [System.Management.Automation.PSParser]::Tokenize((Get-Content -LiteralPath $scriptPath -Raw), [ref]$parseErrors) | Out-Null
    if ($null -ne $parseErrors -and @($parseErrors).Count -gt 0) {
        $message = (@($parseErrors) | ForEach-Object { $_.Message }) -join '; '
        throw ('PowerShell parser errors in {0}: {1}' -f $relativeScript, $message)
    }
}

$sqlInventoryPath = [System.IO.Path]::Combine($repoRoot, 'database\sql\p7\022_runtime_canonical_sql_baseline_handoff_inventory.sql')
$sqlValidatorPath = [System.IO.Path]::Combine($repoRoot, 'database\sql\p7\023_runtime_canonical_sql_baseline_handoff_validator.sql')
$sqlInventory = Get-Content -LiteralPath $sqlInventoryPath -Raw
$sqlValidator = Get-Content -LiteralPath $sqlValidatorPath -Raw

foreach ($term in @('sys.tables', 'sys.schemas', 'sys.columns', 'sys.types', 'CanonicalRuntimeObject', 'LegacyCompatibilityObject')) {
    if ($sqlInventory.IndexOf($term, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw ('Inventory SQL missing required semantic term: {0}' -f $term)
    }
}

foreach ($term in @('migration.Runs', 'migration.WorkItems', 'migration.ManifestRows', 'migration.MigrationRuns', 'sys.foreign_keys')) {
    if ($sqlValidator.IndexOf($term, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw ('Validator SQL missing required semantic term: {0}' -f $term)
    }
}

foreach ($forbiddenTerm in @('DROP TABLE', 'TRUNCATE TABLE', 'DELETE FROM ', 'UPDATE ', 'INSERT INTO migration.')) {
    if ($sqlInventory.IndexOf($forbiddenTerm, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw ('Inventory SQL must be read-only but contains: {0}' -f $forbiddenTerm)
    }
}

$configPath = [System.IO.Path]::Combine($repoRoot, 'config-samples\runtime-sql-baseline-handoff.sample.json')
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
foreach ($propertyName in @('environmentName', 'canonicalSqlInventory', 'canonicalSqlValidator', 'requiredRuntimeTables', 'legacyCompatibilityTables', 'canonicalSmokePath')) {
    if ($null -eq $config.PSObject.Properties[$propertyName]) {
        throw ('Sample handoff configuration missing property: {0}' -f $propertyName)
    }
}

if ($config.canonicalSmokePath -ne 'RuntimeSmoke') {
    throw 'Sample handoff configuration must use RuntimeSmoke as the canonical smoke path.'
}

Write-Host 'P7.10B canonical SQL baseline handoff validation passed.'
