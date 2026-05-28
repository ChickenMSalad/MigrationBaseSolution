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
    'docs\p7\P7.9H-Runtime-SQL-Baseline-Reconciliation.md',
    'database\sql\p7\019_runtime_sql_baseline_reconciliation_diagnostics.sql',
    'database\sql\p7\020_runtime_sql_baseline_reconciliation_plan.sql',
    'database\sql\p7\021_runtime_sql_baseline_reconciliation_validator.sql',
    'tools\runtime\Invoke-RuntimeSqlBaselineReconciliationDiagnostics.ps1',
    'tools\runtime\Invoke-RuntimeSqlBaselineReconciliationValidator.ps1'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw ('Required P7.9H file is missing: {0}' -f $relativePath)
    }
}

$runtimeScripts = @(
    'tools\runtime\Invoke-RuntimeSqlBaselineReconciliationDiagnostics.ps1',
    'tools\runtime\Invoke-RuntimeSqlBaselineReconciliationValidator.ps1'
)

$fragileInvocationPattern = '$' + 'MyInvocation' + '.ScriptName'
$colonPattern = '\$[A-Za-z_][A-Za-z0-9_]*:'
$allowedScopedColonPattern = '\$(script|global|local|private|using|env):'

foreach ($relativeScript in $runtimeScripts) {
    $scriptPath = Join-Path $repoRoot $relativeScript
    $parseErrors = $null
    [System.Management.Automation.PSParser]::Tokenize((Get-Content -LiteralPath $scriptPath -Raw), [ref]$parseErrors) | Out-Null
    if ($null -ne $parseErrors -and @($parseErrors).Count -gt 0) {
        $message = (@($parseErrors) | ForEach-Object { $_.Message }) -join '; '
        throw ('PowerShell parser errors in {0}: {1}' -f $relativeScript, $message)
    }

    $scriptText = Get-Content -LiteralPath $scriptPath -Raw
    if ($scriptText.IndexOf($fragileInvocationPattern, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw ('Avoid fragile invocation-root usage in {0}' -f $relativeScript)
    }
    if ($scriptText -match $colonPattern -and $scriptText -notmatch $allowedScopedColonPattern) {
        throw ('Potential fragile colon interpolation in {0}' -f $relativeScript)
    }
    if ($scriptText.IndexOf('PackageReference Version=', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw ('Script contains inline PackageReference Version text: {0}' -f $relativeScript)
    }
}

$sqlValidatorPath = Join-Path $repoRoot 'database\sql\p7\021_runtime_sql_baseline_reconciliation_validator.sql'
$sqlValidatorText = Get-Content -LiteralPath $sqlValidatorPath -Raw
foreach ($term in @('migration.Runs', 'migration.WorkItems', 'migration.ManifestRows', 'migration.MigrationRuns', 'sys.foreign_keys', 'THROW 51079')) {
    if ($sqlValidatorText.IndexOf($term, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw ('SQL validator is missing expected semantic term: {0}' -f $term)
    }
}

$sqlDiagnosticsPath = Join-Path $repoRoot 'database\sql\p7\019_runtime_sql_baseline_reconciliation_diagnostics.sql'
$sqlDiagnosticsText = Get-Content -LiteralPath $sqlDiagnosticsPath -Raw
foreach ($term in @('CanonicalRuntime', 'LegacyRuntime', 'WorkItemsReferencesCanonicalRuns', 'WorkItemsReferencesLegacyMigrationRuns')) {
    if ($sqlDiagnosticsText.IndexOf($term, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw ('SQL diagnostics are missing expected semantic term: {0}' -f $term)
    }
}

Write-Host 'P7.9H runtime SQL baseline reconciliation validation passed.'
