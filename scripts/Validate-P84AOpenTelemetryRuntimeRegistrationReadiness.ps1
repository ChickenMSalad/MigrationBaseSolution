Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    if ($PSScriptRoot) { return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path }
    if ($MyInvocation -and $MyInvocation.MyCommand -and $MyInvocation.MyCommand.Path) {
        return (Resolve-Path (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) '..')).Path
    }
    return (Get-Location).Path
}

function Test-IsIgnoredPath {
    param([string] $Path)
    if ([string]::IsNullOrWhiteSpace($Path)) { return $false }
    $normalized = $Path.Replace('/', '\').ToLowerInvariant()
    return ($normalized.Contains('\bin\') -or $normalized.Contains('\obj\'))
}

function Assert-PathExists {
    param([string] $RootPath, [string] $RelativePath)
    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) { throw "Required path missing: $RelativePath" }
}

function Assert-FileContains {
    param([string] $RootPath, [string] $RelativePath, [string] $Text)
    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) { throw "Required file missing: $RelativePath" }
    $content = Get-Content -LiteralPath $path -Raw
    if ($null -eq $content -or -not $content.Contains($Text)) {
        throw "Required text missing from $RelativePath : $Text"
    }
}

function Assert-AnySourceContains {
    param([string] $RootPath, [string] $UnderRelativePath, [string] $Text, [string] $Description)
    $searchRoot = Join-Path $RootPath $UnderRelativePath
    if (-not (Test-Path -LiteralPath $searchRoot)) { throw "Required search root missing: $UnderRelativePath" }
    $files = Get-ChildItem -Path $searchRoot -Filter '*.cs' -File -Recurse | Where-Object { -not (Test-IsIgnoredPath $_.FullName) }
    foreach ($file in $files) {
        $content = Get-Content -LiteralPath $file.FullName -Raw
        if ($null -ne $content -and $content.Contains($Text)) { return }
    }
    throw "Required source text missing for $Description under $UnderRelativePath : $Text"
}

$root = Get-RepositoryRoot
Write-Host "Repository root: $root"

Assert-PathExists -RootPath $root -RelativePath 'docs\p8\P8.4A-OpenTelemetry-Runtime-Registration-Readiness.md'
Assert-PathExists -RootPath $root -RelativePath 'config\templates\p8.4a-opentelemetry-runtime-registration.template.json'
Assert-FileContains -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalExecutionActivitySources.cs' -Text 'Migration.Operational.Execution'
Assert-FileContains -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalExecutionActivity.cs' -Text 'StartSqlQueueWorkItemExecution'
Assert-FileContains -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalExecutionActivity.cs' -Text 'StartServiceBusWorkItemExecution'
Assert-FileContains -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalExecutionActivity.cs' -Text 'StartServiceBusDispatch'
Assert-FileContains -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Services\SqlOperationalWorkItemWorker.cs' -Text 'StartSqlQueueWorkItemExecution'
Assert-FileContains -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Runtime\SqlServiceBusExecutorWorker.cs' -Text 'StartServiceBusWorkItemExecution'
Assert-FileContains -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusDispatcher\Dispatching\SqlWorkItemDispatcher.cs' -Text 'StartServiceBusDispatch'
Assert-AnySourceContains -RootPath $root -UnderRelativePath 'src' -Text 'AddEnvironmentVariables(prefix: "MIGRATION_")' -Description 'MIGRATION environment configuration provider'
Assert-AnySourceContains -RootPath $root -UnderRelativePath 'src' -Text 'APPLICATIONINSIGHTS_CONNECTION_STRING' -Description 'Application Insights configuration awareness'
Write-Host 'P8.4A OpenTelemetry runtime registration readiness validation passed.'
