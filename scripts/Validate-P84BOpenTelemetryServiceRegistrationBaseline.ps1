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
    if (-not (Test-Path -LiteralPath $path)) { throw ("Required path missing: {0}" -f $RelativePath) }
}

function Assert-FileContains {
    param([string]$RootPath, [string]$RelativePath, [string]$Text)
    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) { throw ("Required file missing: {0}" -f $RelativePath) }
    $content = Get-Content -LiteralPath $path -Raw
    if ($null -eq $content -or -not $content.Contains($Text)) {
        throw ("Required text missing from {0} : {1}" -f $RelativePath, $Text)
    }
}

$root = Get-RepositoryRoot
Write-Host ("Repository root: {0}" -f $root)

Assert-PathExists -RootPath $root -RelativePath 'docs\p8\P8.4B-OpenTelemetry-Service-Registration-Baseline.md'
Assert-PathExists -RootPath $root -RelativePath 'config\templates\p8.4b-opentelemetry-registration-settings.template.json'
Assert-PathExists -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalTelemetryRegistrationOptions.cs'
Assert-PathExists -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalTelemetryConfigurationKeys.cs'

Assert-FileContains -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalTelemetryRegistrationOptions.cs' -Text 'public const string SectionName = "OpenTelemetry";'
Assert-FileContains -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalTelemetryRegistrationOptions.cs' -Text 'EnableAzureMonitorExporter'
Assert-FileContains -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalTelemetryRegistrationOptions.cs' -Text 'TraceSamplingRatio'
Assert-FileContains -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalTelemetryConfigurationKeys.cs' -Text 'APPLICATIONINSIGHTS_CONNECTION_STRING'
Assert-FileContains -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalExecutionActivitySources.cs' -Text 'Migration.Operational.Execution'
Assert-FileContains -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalExecutionActivity.cs' -Text 'StartServiceBusWorkItemExecution'

Write-Host 'P8.4B OpenTelemetry service registration baseline validation passed.'
