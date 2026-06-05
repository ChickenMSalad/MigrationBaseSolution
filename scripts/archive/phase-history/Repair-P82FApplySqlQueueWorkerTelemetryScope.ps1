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
$relativePath = 'src\Workers\Migration.Workers.QueueExecutor\Services\SqlOperationalWorkItemWorker.cs'
$path = Join-Path $root $relativePath
if (-not (Test-Path -LiteralPath $path)) { throw "Required file missing: $relativePath" }

$content = Get-Content -LiteralPath $path -Raw
if ($content.Contains('OperationalExecutionTelemetryScope.Create(')) {
    Write-Host 'SQL queue worker telemetry scope already applied.'
    return
}

foreach ($required in @('private async Task ExecuteClaimedItemAsync', 'OperationalWorkItemRecord item', '_executor.ExecuteAsync(item', '_logger')) {
    if (-not $content.Contains($required)) { throw "Expected source pattern missing from $relativePath : $required" }
}

if (-not $content.Contains('using Migration.Application.Operational.Telemetry;')) {
    $anchor = 'using Migration.Application.Operational.WorkItems;'
    if (-not $content.Contains($anchor)) { throw "Unable to insert telemetry using; missing anchor: $anchor" }
    $content = $content.Replace($anchor, "$anchor`r`nusing Migration.Application.Operational.Telemetry;")
}

$old = '    private async Task ExecuteClaimedItemAsync(
        OperationalWorkItemRecord item,
        SqlOperationalQueueExecutorOptions options,
        CancellationToken cancellationToken)
    {
        try'
$new = '    private async Task ExecuteClaimedItemAsync(
        OperationalWorkItemRecord item,
        SqlOperationalQueueExecutorOptions options,
        CancellationToken cancellationToken)
    {
        using var telemetryScope = _logger.BeginScope(OperationalExecutionTelemetryScope.Create(
            item.RunId,
            item.WorkItemId,
            options.WorkerId));

        try'
if (-not $content.Contains($old)) { throw 'Unable to apply SQL queue worker telemetry scope; expected method opening was not found.' }
$content = $content.Replace($old, $new)
Set-Content -LiteralPath $path -Value $content -Encoding UTF8
Write-Host "Applied SQL queue worker telemetry scope to $relativePath"
