Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($scriptRoot)) { $scriptRoot = (Get-Location).Path }
$repoRoot = Split-Path -Parent $scriptRoot

function Assert-FileContains {
    param([string] $Path, [string] $Text)
    if (-not (Test-Path -LiteralPath $Path)) { throw ('Missing expected file: ' + $Path) }
    $content = Get-Content -LiteralPath $Path -Raw
    if ($content.IndexOf($Text, [StringComparison]::Ordinal) -lt 0) {
        throw ('Missing expected text in ' + $Path + ': ' + $Text)
    }
}

$runtimeRegistration = Join-Path $repoRoot 'src\Workers\Migration.Workers.QueueExecutor\Registration\SqlOperationalMigrationJobRuntimeRegistrationExtensions.cs'
$preflightService = Join-Path $repoRoot 'src\Core\Migration.Orchestration\Preflight\MigrationPreflightService.cs'

Assert-FileContains -Path $runtimeRegistration -Text 'BuildOperationalRuntimeConfiguration(configuration)'
Assert-FileContains -Path $runtimeRegistration -Text 'GenericMigrationRuntime:EnabledSources:0'
Assert-FileContains -Path $runtimeRegistration -Text 'AzureBlob'
Assert-FileContains -Path $runtimeRegistration -Text 'Bynder'
Assert-FileContains -Path $preflightService -Text 'ValidateRuntimeConnectorComposition(job, issues)'
Assert-FileContains -Path $preflightService -Text 'runtime.sourceConnector.missing'
Assert-FileContains -Path $preflightService -Text 'runtime.targetConnector.missing'

Write-Host 'P7 runtime connector composition fix validation passed.'
