#Requires -Version 5.1
[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..')

$requiredFiles = @(
    'docs\p7\P7.8A-Runtime-Contract-Cleanup.md',
    'database\sql\p7\008_runtime_contract_validator.sql',
    'config-samples\appsettings.SqlServiceBusRuntime.canonical.sample.json',
    'config-samples\appsettings.SqlServiceBusDispatcher.sample.json',
    'config-samples\appsettings.SqlServiceBusExecutor.sample.json',
    'tools\runtime\Export-RuntimeAppSettings.ps1',
    'tools\runtime\Test-RuntimeAppSettings.ps1',
    'tools\runtime\Test-RuntimeSqlContract.ps1'
)

$failures = New-Object System.Collections.Generic.List[string]
foreach ($relativePath in $requiredFiles) {
    $path = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $path)) {
        $failures.Add("Missing required file: $relativePath")
    }
}

function Test-TextFileContains {
    param(
        [Parameter(Mandatory = $true)] [string] $RelativePath,
        [Parameter(Mandatory = $true)] [string] $ExpectedText
    )

    $path = Join-Path $repoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        return
    }

    $content = Get-Content -LiteralPath $path -Raw
    if ($content -notlike "*$ExpectedText*") {
        $failures.Add("File '$RelativePath' does not contain expected text '$ExpectedText'.")
    }
}

Test-TextFileContains -RelativePath 'config-samples\appsettings.SqlServiceBusExecutor.sample.json' -ExpectedText '"WorkItemsTableName"'
Test-TextFileContains -RelativePath 'config-samples\appsettings.SqlServiceBusExecutor.sample.json' -ExpectedText '"WorkItems"'
Test-TextFileContains -RelativePath 'database\sql\p7\008_runtime_contract_validator.sql' -ExpectedText 'migration'
Test-TextFileContains -RelativePath 'database\sql\p7\008_runtime_contract_validator.sql' -ExpectedText 'WorkItems'
Test-TextFileContains -RelativePath 'database\sql\p7\008_runtime_contract_validator.sql' -ExpectedText 'bigint'

# Guard against the exact bad sample property that caused confusion.
$executorSample = Join-Path $repoRoot 'config-samples\appsettings.SqlServiceBusExecutor.sample.json'
if (Test-Path -LiteralPath $executorSample) {
    $sampleContent = Get-Content -LiteralPath $executorSample -Raw
    if ($sampleContent -like '*"TableName"*') {
        $failures.Add("Executor sample still contains legacy/incorrect SqlOperationalWorkItemQueue property 'TableName'. Use 'WorkItemsTableName'.")
    }
}

if ($failures.Count -gt 0) {
    Write-Error 'P7.8A runtime contract cleanup validation failed.'
    foreach ($failure in $failures) { Write-Error $failure }
    exit 1
}

Write-Host 'P7.8A runtime contract cleanup validation passed.'
