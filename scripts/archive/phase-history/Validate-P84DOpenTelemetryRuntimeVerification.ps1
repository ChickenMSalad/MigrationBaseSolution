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
    param([string] $RootPath, [string] $RelativePath)
    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required path missing: $RelativePath"
    }
}

function Assert-FileContains {
    param([string] $RootPath, [string] $RelativePath, [string] $Text)
    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required file missing: $RelativePath"
    }
    $content = Get-Content -LiteralPath $path -Raw
    if ($null -eq $content -or -not $content.Contains($Text)) {
        throw "Required text missing from $RelativePath : $Text"
    }
}

$root = Get-RepositoryRoot
Write-Host "Repository root: $root"

Assert-PathExists -RootPath $root -RelativePath 'docs\p8\P8.4D-OpenTelemetry-Runtime-Verification.md'
Assert-PathExists -RootPath $root -RelativePath 'config\templates\p8.4d-local-otel-smoke-settings.template.json'

Assert-FileContains -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalOpenTelemetryServiceCollectionExtensions.cs' -Text 'AddOperationalOpenTelemetry'
Assert-FileContains -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalOpenTelemetryServiceCollectionExtensions.cs' -Text 'AddOpenTelemetry'
Assert-FileContains -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalOpenTelemetryServiceCollectionExtensions.cs' -Text 'AddSource'
Assert-FileContains -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalOpenTelemetryServiceCollectionExtensions.cs' -Text 'OperationalExecutionActivitySources.Name'
Assert-FileContains -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalTelemetryRegistrationOptions.cs' -Text 'EnableTracing'
Assert-FileContains -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalTelemetryRegistrationOptions.cs' -Text 'EnableAzureMonitorExporter'
Assert-FileContains -RootPath $root -RelativePath 'src\Hosts\Migration.Hosts.SqlOperationalWorker\Program.cs' -Text 'AddOperationalOpenTelemetry'
Assert-FileContains -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusDispatcher\Program.cs' -Text 'AddOperationalOpenTelemetry'
Assert-FileContains -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Program.cs' -Text 'AddOperationalOpenTelemetry'
Assert-FileContains -RootPath $root -RelativePath 'Directory.Packages.props' -Text 'OpenTelemetry.Extensions.Hosting'
Assert-FileContains -RootPath $root -RelativePath 'Directory.Packages.props' -Text 'Azure.Monitor.OpenTelemetry.Exporter'

Write-Host 'P8.4D OpenTelemetry runtime verification validation passed.'
