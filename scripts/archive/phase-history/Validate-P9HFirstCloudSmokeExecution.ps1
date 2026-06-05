Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    if ($PSScriptRoot) { return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path }
    if ($MyInvocation -and $MyInvocation.MyCommand -and $MyInvocation.MyCommand.Path) {
        return (Resolve-Path (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) '..')).Path
    }
    return (Get-Location).Path
}

function Assert-PathExists {
    param([string]$RootPath, [string]$RelativePath)
    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) { throw "Required path missing: $RelativePath" }
}

function Assert-FileContains {
    param([string]$RootPath, [string]$RelativePath, [string]$Text)
    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) { throw "Required file missing: $RelativePath" }
    $content = Get-Content -LiteralPath $path -Raw
    if ($null -eq $content -or -not $content.Contains($Text)) {
        throw "Required text missing from $RelativePath : $Text"
    }
}

$root = Get-RepositoryRoot
Write-Host "Repository root: $root"

Assert-PathExists -RootPath $root -RelativePath 'docs\p9\P9H-First-Cloud-Smoke-Execution.md'
Assert-PathExists -RootPath $root -RelativePath 'config\templates\p9h-first-cloud-smoke-execution-settings.template.json'
Assert-PathExists -RootPath $root -RelativePath 'scripts\sql\P9H-InspectCloudSmokeState.sql'

Assert-FileContains -RootPath $root -RelativePath 'docs\p9\P9H-First-Cloud-Smoke-Execution.md' -Text 'Migration.Operational.Execution'
Assert-FileContains -RootPath $root -RelativePath 'docs\p9\P9H-First-Cloud-Smoke-Execution.md' -Text 'Do not configure a production RunId override'
Assert-FileContains -RootPath $root -RelativePath 'config\templates\p9h-first-cloud-smoke-execution-settings.template.json' -Text 'MigrationOperationalStore'
Assert-FileContains -RootPath $root -RelativePath 'config\templates\p9h-first-cloud-smoke-execution-settings.template.json' -Text 'ServiceBusDispatcher'
Assert-FileContains -RootPath $root -RelativePath 'config\templates\p9h-first-cloud-smoke-execution-settings.template.json' -Text 'ServiceBusExecutor'
Assert-FileContains -RootPath $root -RelativePath 'config\templates\p9h-first-cloud-smoke-execution-settings.template.json' -Text 'OpenTelemetry'
Assert-FileContains -RootPath $root -RelativePath 'scripts\sql\P9H-InspectCloudSmokeState.sql' -Text 'sys.tables'
Assert-FileContains -RootPath $root -RelativePath 'scripts\sql\P9H-InspectCloudSmokeState.sql' -Text 'RunId'
Assert-FileContains -RootPath $root -RelativePath 'scripts\sql\P9H-InspectCloudSmokeState.sql' -Text 'WorkItemId'

Assert-FileContains -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalOpenTelemetryServiceCollectionExtensions.cs' -Text 'AddAzureMonitorTraceExporter'
Assert-FileContains -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalExecutionActivitySources.cs' -Text 'Migration.Operational.Execution'
Assert-FileContains -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusDispatcher\Dispatching\SqlWorkItemDispatcher.cs' -Text 'StartServiceBusDispatch'
Assert-FileContains -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Runtime\SqlServiceBusExecutorWorker.cs' -Text 'StartServiceBusWorkItemExecution'
Assert-FileContains -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Services\SqlOperationalWorkItemWorker.cs' -Text 'StartSqlQueueWorkItemExecution'

Write-Host 'P9H first cloud smoke execution validation passed.'
