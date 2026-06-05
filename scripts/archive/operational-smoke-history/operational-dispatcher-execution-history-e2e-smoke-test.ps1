param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Dispatcher execution history end-to-end smoke ==="

Write-Host ""
Write-Host "Step 1: Readiness"
$readiness = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/dispatcher/executions/readiness" `
    -ContentType "application/json"

$readiness | ConvertTo-Json -Depth 10

if (-not $readiness.ready) {
    throw "Dispatcher execution history is not ready."
}

Write-Host ""
Write-Host "Step 2: History count before dispatcher run"
$before = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/dispatcher/executions" `
    -ContentType "application/json"

$beforeCount = @($before).Count
Write-Host "BeforeCount: $beforeCount"

Write-Host ""
Write-Host "Step 3: Run dispatcher once"
$runOnce = Invoke-RestMethod `
    -Method Post `
    -Uri "$BaseUrl/api/operational/dispatcher/run-once" `
    -ContentType "application/json"

$runOnce | ConvertTo-Json -Depth 10

Write-Host ""
Write-Host "Step 4: History count after dispatcher run"
$after = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/dispatcher/executions" `
    -ContentType "application/json"

$afterCount = @($after).Count
Write-Host "AfterCount: $afterCount"

if ($afterCount -le $beforeCount) {
    throw "Dispatcher execution history count did not increase."
}

$latest = @($after)[0]

Write-Host ""
Write-Host "Step 5: Latest execution summary"
Write-Host "ExecutionId: $($latest.executionId)"
Write-Host "WorkerId: $($latest.workerId)"
Write-Host "Outcome: $($latest.outcome)"
Write-Host "RequestedLeaseCount: $($latest.requestedLeaseCount)"
Write-Host "LeasedCount: $($latest.leasedCount)"
Write-Host "CompletedCount: $($latest.completedCount)"
Write-Host "FailedCount: $($latest.failedCount)"
Write-Host "DurationMilliseconds: $($latest.durationMilliseconds)"
Write-Host "Message: $($latest.message)"

if (-not $latest.executionId) {
    throw "Latest dispatcher execution does not include executionId."
}

if (-not $latest.workerId) {
    throw "Latest dispatcher execution does not include workerId."
}

if ($null -eq $latest.durationMilliseconds) {
    throw "Latest dispatcher execution does not include durationMilliseconds."
}

Write-Host ""
Write-Host "Step 6: Read latest execution detail"
$detail = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/operational/dispatcher/executions/$($latest.executionId)" `
    -ContentType "application/json"

$detail | ConvertTo-Json -Depth 10

if ($detail.executionId -ne $latest.executionId) {
    throw "Execution detail did not match latest execution id."
}

Write-Host ""
Write-Host "Dispatcher execution history end-to-end smoke passed."
