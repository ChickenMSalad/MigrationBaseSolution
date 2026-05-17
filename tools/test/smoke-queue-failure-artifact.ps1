param(
    [string]$BaseUrl = "http://localhost:5173"
)

$ErrorActionPreference = "Stop"

Write-Host "Queue Failure Artifact Smoke Test"
Write-Host "Base URL: $BaseUrl"
Write-Host ""

$plan = Invoke-RestMethod "$BaseUrl/api/cloud/queue/failure-artifact/plan"

Write-Host "Artifact kind : $($plan.descriptor.artifactKind)"
Write-Host "Artifact id   : $($plan.descriptor.artifactId)"
Write-Host "File name     : $($plan.descriptor.fileName)"

if ([string]::IsNullOrWhiteSpace($plan.descriptor.objectKey)) {
    throw "Failure artifact object key was empty."
}

$probe = Invoke-RestMethod -Method Post "$BaseUrl/api/cloud/queue/failure-artifact/probe"

if ($null -eq $probe.artifact -or [string]::IsNullOrWhiteSpace($probe.artifact.objectKey)) {
    throw "Failure artifact probe did not return an artifact descriptor."
}

Write-Host "Wrote artifact: $($probe.artifact.objectKey)"
Write-Host ""
Write-Host "Queue failure artifact smoke test completed successfully."
