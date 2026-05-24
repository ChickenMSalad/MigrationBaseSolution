Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    if ($PSScriptRoot) { return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path }
    if ($MyInvocation -and $MyInvocation.MyCommand -and $MyInvocation.MyCommand.Path) {
        return (Resolve-Path (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) '..')).Path
    }
    return (Get-Location).Path
}

$root = Get-RepositoryRoot
$relativePath = 'src\Workers\Migration.Workers.ServiceBusExecutor\Runtime\SqlServiceBusExecutorWorker.cs'
$path = Join-Path $root $relativePath
if (-not (Test-Path -LiteralPath $path)) { throw "Required file missing: $relativePath" }

$content = Get-Content -LiteralPath $path -Raw
if ($content.Contains('OperationalExecutionTelemetryScope.Create(')) {
    Write-Host 'Service Bus executor telemetry scope already applied.'
    return
}

foreach ($required in @('ServiceBusProcessor', 'ServiceBusWorkItemMessage? message', 'message.WorkItemId', 'message.RunId', 'args.Message')) {
    if (-not $content.Contains($required)) { throw "Expected source pattern missing from $relativePath : $required" }
}

if (-not $content.Contains('using Migration.Application.Operational.Telemetry;')) {
    $firstUsing = 'using Azure.Messaging.ServiceBus;'
    if (-not $content.Contains($firstUsing)) { throw "Unable to insert telemetry using; missing anchor: $firstUsing" }
    $content = $content.Replace($firstUsing, "$firstUsing`r`nusing Migration.Application.Operational.Telemetry;")
}

$anchor = '            var workItem = await _workItemQueue.GetAsync(message.WorkItemId, args.CancellationToken).ConfigureAwait(false);'
if (-not $content.Contains($anchor)) { throw "Unable to locate Service Bus work-item lookup anchor in $relativePath" }
$insert = @'
            using var telemetryScope = _logger.BeginScope(OperationalExecutionTelemetryScope.Create(
                message.RunId,
                message.WorkItemId,
                _options.Value.WorkerId,
                args.Message.MessageId,
                args.Message.CorrelationId,
                args.Message.DeliveryCount));

'@
$content = $content.Replace($anchor, $insert + $anchor)
Set-Content -LiteralPath $path -Value $content -Encoding UTF8
Write-Host "Applied Service Bus executor telemetry scope to $relativePath"
