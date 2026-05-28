[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$toolRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($toolRoot)) {
    $toolRoot = Split-Path -Parent $PSCommandPath
}

if ([string]::IsNullOrWhiteSpace($toolRoot)) {
    throw 'Unable to resolve validator script root.'
}

$repoRoot = Split-Path -Parent $toolRoot

$requiredFiles = @(
    'docs\p7\P7.9A-Runtime-Run-Parent-FK-Canonicalization.md',
    'database\sql\p7\012_runtime_run_parent_fk_diagnostics.sql',
    'database\sql\p7\013_runtime_run_parent_fk_canonicalization.sql',
    'database\sql\p7\014_runtime_run_parent_fk_validator.sql',
    'tools\runtime\Invoke-RuntimeRunParentFkDiagnostics.ps1',
    'tools\runtime\Invoke-RuntimeRunParentFkCanonicalization.ps1',
    'tools\runtime\Invoke-RuntimeRunParentFkValidator.ps1'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw ('Required P7.9A file is missing: {0}' -f $relativePath)
    }
}

$sqlCanonicalizationPath = Join-Path $repoRoot 'database\sql\p7\013_runtime_run_parent_fk_canonicalization.sql'
$sqlValidatorPath = Join-Path $repoRoot 'database\sql\p7\014_runtime_run_parent_fk_validator.sql'
$sqlCanonicalizationText = Get-Content -LiteralPath $sqlCanonicalizationPath -Raw
$sqlValidatorText = Get-Content -LiteralPath $sqlValidatorPath -Raw

$canonicalizationTerms = @(
    'FK_WorkItems_MigrationRuns',
    'FK_WorkItems_Runs',
    'migration.WorkItems',
    'migration.Runs',
    'migration.MigrationRuns',
    'BEGIN TRANSACTION',
    'COMMIT TRANSACTION'
)

foreach ($term in $canonicalizationTerms) {
    if ($sqlCanonicalizationText.IndexOf($term, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw ('Canonicalization SQL is missing required term: {0}' -f $term)
    }
}

$validatorTerms = @(
    'FK_WorkItems_Runs',
    'FK_WorkItems_MigrationRuns',
    'migration.WorkItems',
    'migration.Runs',
    'THROW 51096'
)

foreach ($term in $validatorTerms) {
    if ($sqlValidatorText.IndexOf($term, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw ('Validator SQL is missing required term: {0}' -f $term)
    }
}

$parser = [System.Management.Automation.Language.Parser]
$parseErrors = @()
$tokens = $null

$scriptFiles = @(
    'tools\runtime\Invoke-RuntimeRunParentFkDiagnostics.ps1',
    'tools\runtime\Invoke-RuntimeRunParentFkCanonicalization.ps1',
    'tools\runtime\Invoke-RuntimeRunParentFkValidator.ps1',
    'tools\validate-p7.9a-runtime-run-parent-fk.ps1'
)

foreach ($relativeScript in $scriptFiles) {
    $scriptPath = Join-Path $repoRoot $relativeScript
    $null = $parser::ParseFile($scriptPath, [ref] $tokens, [ref] $parseErrors)
    if (@($parseErrors).Count -gt 0) {
        $message = ($parseErrors | ForEach-Object { $_.Message }) -join '; '
        throw ('PowerShell parser errors in {0}: {1}' -f $relativeScript, $message)
    }
}

Write-Host 'P7.9A runtime run parent FK drop-in validation passed.'
