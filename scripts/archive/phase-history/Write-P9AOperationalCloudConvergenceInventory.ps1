Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    if ($PSScriptRoot) { return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path }
    if ($MyInvocation -and $MyInvocation.MyCommand -and $MyInvocation.MyCommand.Path) {
        return (Resolve-Path (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) '..')).Path
    }
    return (Get-Location).Path
}

function Add-FileSummary {
    param([string]$RelativePath, [string[]]$Patterns)
    $script:Lines.Add('') | Out-Null
    $script:Lines.Add("## $RelativePath") | Out-Null
    $script:Lines.Add('') | Out-Null
    $path = Join-Path $script:Root $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        $script:Lines.Add('Missing.') | Out-Null
        return
    }
    $script:Lines.Add('Present.') | Out-Null
    $content = Get-Content -LiteralPath $path -Raw
    foreach ($pattern in $Patterns) {
        if ($null -ne $content -and $content.Contains($pattern)) {
            $script:Lines.Add(('- Contains: `{0}`' -f $pattern)) | Out-Null
        }
        else {
            $script:Lines.Add(('- Missing: `{0}`' -f $pattern)) | Out-Null
        }
    }
}

$script:Root = Get-RepositoryRoot
$outDir = Join-Path $script:Root 'docs\p9'
if (-not (Test-Path -LiteralPath $outDir)) { New-Item -ItemType Directory -Path $outDir | Out-Null }
$out = Join-Path $outDir 'P9A-Operational-Cloud-Convergence-Inventory.generated.md'

$script:Lines = New-Object System.Collections.Generic.List[string]
$script:Lines.Add('# P9A Operational Cloud Convergence Inventory') | Out-Null
$script:Lines.Add('') | Out-Null
$script:Lines.Add(('GeneratedUtc: {0:O}' -f [DateTimeOffset]::UtcNow)) | Out-Null
$script:Lines.Add('') | Out-Null
$script:Lines.Add('This inventory verifies that P8 runtime execution, telemetry, cloud host, and proof-of-life surfaces are ready for P9 operational convergence.') | Out-Null

Add-FileSummary 'src\Core\Migration.Application\Operational\Telemetry\OperationalOpenTelemetryServiceCollectionExtensions.cs' @('AddOperationalOpenTelemetry','AddOpenTelemetry','AddSource','AddAzureMonitorTraceExporter')
Add-FileSummary 'src\Core\Migration.Application\Operational\Telemetry\OperationalExecutionActivitySources.cs' @('Migration.Operational.Execution','SqlQueueWorkItemExecution','ServiceBusDispatch','ServiceBusWorkItemExecution')
Add-FileSummary 'src\Hosts\Migration.Hosts.SqlOperationalWorker\Program.cs' @('AddOperationalOpenTelemetry','AddEnvironmentVariables(prefix: "MIGRATION_")')
Add-FileSummary 'src\Workers\Migration.Workers.ServiceBusDispatcher\Program.cs' @('AddOperationalOpenTelemetry')
Add-FileSummary 'src\Workers\Migration.Workers.ServiceBusExecutor\Program.cs' @('AddOperationalOpenTelemetry')
Add-FileSummary 'src\Workers\Migration.Workers.QueueExecutor\Services\SqlOperationalWorkItemWorker.cs' @('StartSqlQueueWorkItemExecution','SetExecutionDuration','SetExecutionResult')
Add-FileSummary 'src\Workers\Migration.Workers.ServiceBusDispatcher\Dispatching\SqlWorkItemDispatcher.cs' @('StartServiceBusDispatch','SetExecutionDuration','SetExecutionResult')
Add-FileSummary 'src\Workers\Migration.Workers.ServiceBusExecutor\Runtime\SqlServiceBusExecutorWorker.cs' @('StartServiceBusWorkItemExecution','SetExecutionDuration','SetExecutionResult')
Add-FileSummary 'docs\p9\P9A-Operational-Cloud-Convergence-Proof.md' @('Proof order','Success criteria')
Add-FileSummary 'config\templates\p9a-operational-cloud-proof-settings.template.json' @('EnableTracing','EnableAzureMonitorExporter','TraceSamplingRatio')

Set-Content -LiteralPath $out -Value $script:Lines -Encoding UTF8
Write-Host "Wrote $out"
