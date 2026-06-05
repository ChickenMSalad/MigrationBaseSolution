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

function Assert-FileDoesNotContain {
    param([string]$RootPath, [string]$RelativePath, [string]$Text)
    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) { return }
    $content = Get-Content -LiteralPath $path -Raw
    if ($null -ne $content -and $content.Contains($Text)) {
        throw "Unexpected text found in $RelativePath : $Text"
    }
}

$root = Get-RepositoryRoot
Write-Host "Repository root: $root"

Assert-PathExists -RootPath $root -RelativePath 'docs\p8\P8.4C-OpenTelemetry-Runtime-Registration-Manual-Patch.md'
Assert-PathExists -RootPath $root -RelativePath 'config\templates\p8.4c-opentelemetry-runtime-registration.template.json'

Assert-FileContains -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalTelemetryRegistrationOptions.cs' -Text 'EnableTracing'
Assert-FileContains -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalExecutionActivitySources.cs' -Text 'Migration.Operational.Execution'
Assert-FileContains -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalExecutionActivity.cs' -Text 'StartServiceBusWorkItemExecution'

Assert-FileContains -RootPath $root -RelativePath 'config\templates\p8.4c-opentelemetry-runtime-registration.template.json' -Text 'EnableAzureMonitorExporter'
Assert-FileContains -RootPath $root -RelativePath 'config\templates\p8.4c-opentelemetry-runtime-registration.template.json' -Text 'TraceSamplingRatio'

Assert-FileDoesNotContain -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusDispatcher\Migration.Workers.ServiceBusDispatcher.csproj' -Text 'Version="'
Assert-FileDoesNotContain -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Migration.Workers.ServiceBusExecutor.csproj' -Text 'Version="'
Assert-FileDoesNotContain -RootPath $root -RelativePath 'src\Hosts\Migration.Hosts.SqlOperationalWorker\Migration.Hosts.SqlOperationalWorker.csproj' -Text 'Version="'

Write-Host 'P8.4C OpenTelemetry runtime registration manual patch validation passed.'
