$repoRoot = (Resolve-Path ".").Path

$startupPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"
$registrationPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiOperationalStoreMirrorRegistrationExtensions.cs"
$dispatcherServicePath = Join-Path $repoRoot "src\Migration.Admin.Api\OperationalStore\OperationalDispatcherService.cs"

if (-not (Test-Path $startupPath)) { throw "Missing $startupPath" }
if (-not (Test-Path $registrationPath)) { throw "Missing $registrationPath" }
if (-not (Test-Path $dispatcherServicePath)) { throw "Missing $dispatcherServicePath" }

$startup = Get-Content $startupPath -Raw

if ($startup -notmatch "MapOperationalDispatcherExecutionHistoryEndpoints\(") {
    $startup = $startup -replace "api\.MapOperationalDispatcherDiagnosticsEndpoints\(\);", "api.MapOperationalDispatcherDiagnosticsEndpoints();`r`n        api.MapOperationalDispatcherExecutionHistoryEndpoints();"

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped dispatcher execution history endpoints."
}

$registration = Get-Content $registrationPath -Raw

if ($registration -notmatch "IDispatcherExecutionHistoryService") {
    $registration = $registration -replace "services\.AddScoped<IOperationalDispatcherDiagnosticsService, OperationalDispatcherDiagnosticsService>\(\);", "services.AddScoped<IOperationalDispatcherDiagnosticsService, OperationalDispatcherDiagnosticsService>();`r`n        services.AddScoped<IDispatcherExecutionHistoryService, DispatcherExecutionHistoryService>();"

    Set-Content -Path $registrationPath -Value $registration -NoNewline
    Write-Host "Registered dispatcher execution history service."
}

$dispatcher = Get-Content $dispatcherServicePath -Raw

if ($dispatcher -notmatch "_historyService") {

    $dispatcher = $dispatcher -replace "private readonly ILogger<OperationalDispatcherService> _logger;", "private readonly ILogger<OperationalDispatcherService> _logger;`r`n    private readonly IDispatcherExecutionHistoryService _historyService;"

    $dispatcher = $dispatcher -replace "ILogger<OperationalDispatcherService> logger\)", "ILogger<OperationalDispatcherService> logger,`r`n        IDispatcherExecutionHistoryService historyService)"

    $dispatcher = $dispatcher -replace "_logger = logger;", "_logger = logger;`r`n        _historyService = historyService;"

    $dispatcher = $dispatcher -replace "var options = NormalizeOptions\(\);", "var executionStartedAt = DateTimeOffset.UtcNow;`r`n        var executionId = Guid.NewGuid();`r`n`r`n        var options = NormalizeOptions();"

    $dispatcher = $dispatcher -replace "return new OperationalDispatcherRunOnceResponse", "var executionCompletedAt = DateTimeOffset.UtcNow;`r`n`r`n        await _historyService.RecordAsync(`r`n            new DispatcherExecutionRecord`r`n            {`r`n                ExecutionId = executionId,`r`n                WorkerId = options.WorkerId,`r`n                StartedAt = executionStartedAt,`r`n                CompletedAt = executionCompletedAt,`r`n                DurationMilliseconds = (long)(executionCompletedAt - executionStartedAt).TotalMilliseconds,`r`n                RequestedLeaseCount = options.LeaseCount,`r`n                LeasedCount = lease.LeasedCount,`r`n                CompletedCount = completed,`r`n                FailedCount = failed,`r`n                Outcome = failed > 0 ? \"CompletedWithFailures\" : \"Completed\",`r`n                Message = lease.LeasedCount == 0`r`n                    ? \"No eligible work items were leased.\"`r`n                    : $\"Dispatcher processed {completed} completed and {failed} failed work item(s).\"`r`n            },`r`n            cancellationToken);`r`n`r`n        return new OperationalDispatcherRunOnceResponse"

    Set-Content -Path $dispatcherServicePath -Value $dispatcher -NoNewline

    Write-Host "Wired dispatcher execution history recording."
}

Write-Host ""
Write-Host "IMPORTANT:"
Write-Host "Create migration.DispatcherExecutions table using:"
Write-Host "src\Migration.Admin.Api\OperationalStore\DispatcherExecutionsTable.sql"
