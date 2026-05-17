param(
    [string]$BaseUrl = "http://localhost:5173"
)

$ErrorActionPreference = "Stop"

Write-Host "Queue Failure Handler Smoke Test"
Write-Host "Base URL: $BaseUrl"
Write-Host ""

$probe = Invoke-RestMethod -Method Post "$BaseUrl/api/cloud/queue/failure-handler/probe"

Write-Host "Strategy              : $($probe.result.strategy)"
Write-Host "Failure artifact wrote: $($probe.result.failureArtifactWritten)"
Write-Host "Artifact object key   : $($probe.result.artifactObjectKey)"

if ($probe.result.failureArtifactWritten -ne $true) {
    throw "Expected failure handler to write a failure artifact."
}

if ([string]::IsNullOrWhiteSpace($probe.result.artifactObjectKey)) {
    throw "Expected failure handler to return artifact object key."
}

Write-Host ""
Write-Host "Queue failure handler smoke test completed successfully."
