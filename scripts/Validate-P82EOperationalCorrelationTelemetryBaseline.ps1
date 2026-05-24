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
    param([string]$Path)
    if ([string]::IsNullOrWhiteSpace($Path)) { return $false }
    $normalized = $Path.Replace('/', '\').ToLowerInvariant()
    return ($normalized.Contains('\bin\') -or $normalized.Contains('\obj\'))
}

function Assert-PathExists {
    param([string]$RootPath, [string]$RelativePath)
    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) { throw "Required path missing: $RelativePath" }
}

function Assert-NoPathExists {
    param([string]$RootPath, [string]$RelativePath)
    $path = Join-Path $RootPath $RelativePath
    if (Test-Path -LiteralPath $path) { throw "Invalid path should not exist: $RelativePath" }
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

function Assert-AnySourceContains {
    param([string]$RootPath, [string]$UnderRelativePath, [string[]]$RequiredTexts, [string]$Description)
    $searchRoot = Join-Path $RootPath $UnderRelativePath
    if (-not (Test-Path -LiteralPath $searchRoot)) { throw "Required search root missing: $UnderRelativePath" }

    $files = Get-ChildItem -Path $searchRoot -Filter '*.cs' -File -Recurse | Where-Object { -not (Test-IsIgnoredPath $_.FullName) }
    foreach ($file in $files) {
        $content = Get-Content -LiteralPath $file.FullName -Raw
        $allFound = $true
        foreach ($text in $RequiredTexts) {
            if ($null -eq $content -or -not $content.Contains($text)) { $allFound = $false; break }
        }
        if ($allFound) { return }
    }
    throw "Required source pattern not found for $Description under $UnderRelativePath"
}

$root = Get-RepositoryRoot
Write-Host "Repository root: $root"

Assert-NoPathExists -RootPath $root -RelativePath 'src\Migration.Infrastructure'
Assert-NoPathExists -RootPath $root -RelativePath 'src\Migration.Worker'

Assert-PathExists -RootPath $root -RelativePath 'docs\p8\P8.2E-Operational-Correlation-Telemetry-Baseline.md'
Assert-PathExists -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalExecutionTelemetryFields.cs'

Assert-FileContains -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalExecutionTelemetryFields.cs' -Text 'OperationalExecutionTelemetryFields'
Assert-FileContains -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalExecutionTelemetryFields.cs' -Text 'ServiceBusCorrelationId'
Assert-FileContains -RootPath $root -RelativePath 'docs\p8\P8.2E-Operational-Correlation-Telemetry-Baseline.md' -Text 'RunId'
Assert-FileContains -RootPath $root -RelativePath 'docs\p8\P8.2E-Operational-Correlation-Telemetry-Baseline.md' -Text 'WorkItemId'
Assert-FileContains -RootPath $root -RelativePath 'docs\p8\P8.2E-Operational-Correlation-Telemetry-Baseline.md' -Text 'ISqlOperationalWorkItemExecutor.ExecuteAsync'

Assert-AnySourceContains -RootPath $root -UnderRelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor' -RequiredTexts @('SqlOperationalServiceBusWorkItemExecutor', 'ISqlOperationalWorkItemExecutor') -Description 'Service Bus SQL operational execution adapter'
Assert-AnySourceContains -RootPath $root -UnderRelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor' -RequiredTexts @('CompleteOperationalWorkItemRequest', 'FailOperationalWorkItemRequest') -Description 'Service Bus SQL completion/failure persistence'
Assert-AnySourceContains -RootPath $root -UnderRelativePath 'src\Core\Migration.Infrastructure.Sql\Operational\ExecutionHistory' -RequiredTexts @('IOperationalExecutionHistoryWriter') -Description 'SQL operational execution history writer'

Write-Host 'P8.2E operational correlation telemetry baseline validation passed.'
