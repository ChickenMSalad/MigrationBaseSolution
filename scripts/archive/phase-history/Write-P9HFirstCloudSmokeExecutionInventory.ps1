Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    if ($PSScriptRoot) { return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path }
    if ($MyInvocation -and $MyInvocation.MyCommand -and $MyInvocation.MyCommand.Path) {
        return (Resolve-Path (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) '..')).Path
    }
    return (Get-Location).Path
}

function Add-Line { param([string]$Text) $script:Lines.Add($Text) | Out-Null }

function Add-FileSummary {
    param([string]$RootPath, [string]$RelativePath, [string[]]$Patterns)
    Add-Line ""
    Add-Line "## $RelativePath"
    Add-Line ""
    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        Add-Line 'Missing.'
        return
    }
    Add-Line 'Present.'
    $content = Get-Content -LiteralPath $path -Raw
    foreach ($pattern in $Patterns) {
        if ($null -ne $content -and $content.Contains($pattern)) {
            Add-Line (('- Contains: {0}' -f $pattern))
        }
        else {
            Add-Line (('- Missing: {0}' -f $pattern))
        }
    }
}

$root = Get-RepositoryRoot
$out = Join-Path $root 'docs\p9\P9H-First-Cloud-Smoke-Execution-Inventory.generated.md'
$script:Lines = New-Object System.Collections.Generic.List[string]

Add-Line '# P9H First Cloud Smoke Execution Inventory'
Add-Line ''
Add-Line ('GeneratedUtc: {0:O}' -f [DateTimeOffset]::UtcNow)
Add-Line ''
Add-Line 'This inventory verifies repository-side first cloud smoke execution readiness before running deployed workers against Azure SQL, Service Bus, and Azure Monitor.'

Add-FileSummary -RootPath $root -RelativePath 'docs\p9\P9H-First-Cloud-Smoke-Execution.md' -Patterns @('Proof order', 'Success criteria', 'Do not configure a production RunId override')
Add-FileSummary -RootPath $root -RelativePath 'config\templates\p9h-first-cloud-smoke-execution-settings.template.json' -Patterns @('MigrationOperationalStore', 'ServiceBusDispatcher', 'ServiceBusExecutor', 'OpenTelemetry')
Add-FileSummary -RootPath $root -RelativePath 'scripts\sql\P9H-InspectCloudSmokeState.sql' -Patterns @('sys.tables', 'sys.columns', 'RunId', 'WorkItemId')
Add-FileSummary -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalOpenTelemetryServiceCollectionExtensions.cs' -Patterns @('AddOperationalOpenTelemetry', 'AddAzureMonitorTraceExporter')
Add-FileSummary -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalExecutionActivitySources.cs' -Patterns @('Migration.Operational.Execution', 'SqlQueueWorkItemExecution', 'ServiceBusDispatch', 'ServiceBusWorkItemExecution')
Add-FileSummary -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusDispatcher\Dispatching\SqlWorkItemDispatcher.cs' -Patterns @('StartServiceBusDispatch', 'SetExecutionDuration', 'SetExecutionResult')
Add-FileSummary -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Runtime\SqlServiceBusExecutorWorker.cs' -Patterns @('StartServiceBusWorkItemExecution', 'SetExecutionDuration', 'SetExecutionResult')
Add-FileSummary -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Services\SqlOperationalWorkItemWorker.cs' -Patterns @('StartSqlQueueWorkItemExecution', 'SetExecutionDuration', 'SetExecutionResult')

Add-Line ''
Add-Line '## Recommended next checks'
Add-Line ''
Add-Line '- Run scripts/sql/P9H-InspectCloudSmokeState.sql against the target Azure SQL operational database.'
Add-Line '- Deploy worker roles disabled first if enabled flags are available.'
Add-Line '- Enable SQL worker, dispatcher, and executor in that order.'
Add-Line '- Run a tiny operational manifest smoke test.'
Add-Line '- Verify Azure Monitor traces for Migration.Operational.Execution.'

$directory = Split-Path -Parent $out
if (-not (Test-Path -LiteralPath $directory)) { New-Item -ItemType Directory -Path $directory | Out-Null }
Set-Content -LiteralPath $out -Value $Lines -Encoding UTF8
Write-Host "Wrote $out"
