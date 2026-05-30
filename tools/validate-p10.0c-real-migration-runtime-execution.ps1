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
    'docs\p10\P10.0C-Real-Migration-Runtime-Execution.md',
    'config-samples\p10-real-migration-runtime-execution.sample.json',
    'database\sql\p10\002_p10_real_migration_runtime_state_validator.sql',
    'tools\runtime\Invoke-P100RealMigrationRuntimeExecution.ps1',
    'tools\runtime\Test-P100RealMigrationRuntimeState.ps1'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw ('Required P10.0C file is missing: {0}' -f $relativePath)
    }
}

$scriptsToParse = @(
    'tools\runtime\Invoke-P100RealMigrationRuntimeExecution.ps1',
    'tools\runtime\Test-P100RealMigrationRuntimeState.ps1'
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

$sqlText = Get-Content -LiteralPath (Join-Path $repoRoot 'database\sql\p10\002_p10_real_migration_runtime_state_validator.sql') -Raw
foreach ($term in @('migration.Runs', 'migration.WorkItems', 'RunId', 'WorkItemId', 'LastErrorMessage')) {
    if ($sqlText.IndexOf($term, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw ('P10.0C SQL validator is missing required term: {0}' -f $term)
    }
}

$configPath = Join-Path $repoRoot 'config-samples\p10-real-migration-runtime-execution.sample.json'
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
foreach ($propertyName in @('sqlServer', 'database', 'jobDefinitionPath', 'expectedWorkType')) {
    if ($null -eq $config.PSObject.Properties[$propertyName]) {
        throw ('P10.0C sample config is missing property: {0}' -f $propertyName)
    }
}

$invokeText = Get-Content -LiteralPath (Join-Path $repoRoot 'tools\runtime\Invoke-P100RealMigrationRuntimeExecution.ps1') -Raw
foreach ($term in @('p10-localstorage-real-migration.job.json', 'MigrationJobDefinition', 'migration.WorkItems', 'migration.Runs')) {
    if ($invokeText.IndexOf($term, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw ('P10.0C enqueue script is missing required term: {0}' -f $term)
    }
}

Write-Host 'P10.0C real migration runtime execution validation passed.'
