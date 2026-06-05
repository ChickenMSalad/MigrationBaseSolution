Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    if ($PSScriptRoot) { return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path }
    if ($MyInvocation -and $MyInvocation.MyCommand -and $MyInvocation.MyCommand.Path) {
        return (Resolve-Path (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) '..')).Path
    }
    return (Get-Location).Path
}

function Add-UsingIfMissing {
    param([string]$Content, [string]$UsingText)

    if ($Content.Contains($UsingText)) { return $Content }

    $lines = New-Object 'System.Collections.Generic.List[string]'
    $Content -split "`r?`n" | ForEach-Object { $lines.Add($_) | Out-Null }

    $insertAt = 0
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i].StartsWith('using ', [System.StringComparison]::Ordinal)) {
            $insertAt = $i + 1
        }
    }

    $lines.Insert($insertAt, $UsingText)
    return ($lines -join [Environment]::NewLine)
}

$root = Get-RepositoryRoot
$relativePath = 'src\Workers\Migration.Workers.ServiceBusExecutor\Runtime\SqlServiceBusExecutorWorker.cs'
$path = Join-Path $root $relativePath

if (-not (Test-Path -LiteralPath $path)) {
    throw "Required file missing: $relativePath"
}

$helperPath = Join-Path $root 'src\Core\Migration.Application\Operational\Telemetry\OperationalExecutionTelemetryScope.cs'
if (-not (Test-Path -LiteralPath $helperPath)) {
    throw 'OperationalExecutionTelemetryScope helper is missing. Apply P8.2F first.'
}

$helper = Get-Content -LiteralPath $helperPath -Raw
if ($null -eq $helper -or -not $helper.Contains('OperationalExecutionTelemetryScope')) {
    throw 'OperationalExecutionTelemetryScope helper did not contain the expected type name.'
}

$content = Get-Content -LiteralPath $path -Raw
if ($null -eq $content) { throw "Unable to read $relativePath" }

if ($content.Contains('OperationalExecutionTelemetryScope.Create(') -or $content.Contains('OperationalExecutionTelemetryFields.ServiceBusCorrelationId')) {
    Write-Host 'Service Bus runtime correlation scope already appears to be applied.'
    return
}

$content = Add-UsingIfMissing -Content $content -UsingText 'using Migration.Application.Operational.Telemetry;'

$needle = 'var result = await _executor.ExecuteAsync(workItem, args.CancellationToken).ConfigureAwait(false);'
if (-not $content.Contains($needle)) {
    throw "Unable to apply Service Bus runtime correlation scope; expected executor invocation was not found: $needle"
}

$scope = @'
using var telemetryScope = _logger.BeginScope(OperationalExecutionTelemetryScope.Create(
            workItem.RunId,
            workItem.WorkItemId,
            workItem.ManifestRowId,
            workItem.WorkItemType,
            workItem.AttemptCount,
            workItem.PartitionKey,
            args.Message.CorrelationId));

        var result = await _executor.ExecuteAsync(workItem, args.CancellationToken).ConfigureAwait(false);
'@

$content = $content.Replace($needle, $scope.TrimEnd())
Set-Content -LiteralPath $path -Value $content -Encoding UTF8
Write-Host "Applied Service Bus runtime correlation scope to $relativePath"
