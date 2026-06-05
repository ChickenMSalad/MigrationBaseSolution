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
    if (-not (Test-Path -LiteralPath $path)) {
        throw ("Required path missing: {0}" -f $RelativePath)
    }
}

function Assert-FileContains {
    param([string]$RootPath, [string]$RelativePath, [string]$Text)
    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw ("Required file missing: {0}" -f $RelativePath)
    }
    $content = Get-Content -LiteralPath $path -Raw
    if ($null -eq $content -or -not $content.Contains($Text)) {
        throw ("Required text missing from {0}: {1}" -f $RelativePath, $Text)
    }
}

$root = Get-RepositoryRoot
Write-Host ("Repository root: {0}" -f $root)

Assert-PathExists -RootPath $root -RelativePath 'docs\p8\P8.3D.1-Inventory-First-Runtime-Activity-Usage.md'
Assert-PathExists -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalExecutionActivity.cs'
Assert-FileContains -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalExecutionActivity.cs' -Text 'StartSqlQueueWorkItemExecution'
Assert-FileContains -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalExecutionActivity.cs' -Text 'StartServiceBusWorkItemExecution'
Assert-FileContains -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalExecutionActivity.cs' -Text 'StartServiceBusDispatch'
Assert-FileContains -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalExecutionActivity.cs' -Text 'SetExecutionDuration'
Assert-FileContains -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalExecutionActivity.cs' -Text 'SetExecutionResult'

Assert-PathExists -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Services\SqlOperationalWorkItemWorker.cs'
Assert-PathExists -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Runtime\SqlServiceBusExecutorWorker.cs'
Assert-PathExists -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusDispatcher\Dispatching\SqlWorkItemDispatcher.cs'

Write-Host 'P8.3D.1 inventory-first runtime Activity usage validation passed.'
