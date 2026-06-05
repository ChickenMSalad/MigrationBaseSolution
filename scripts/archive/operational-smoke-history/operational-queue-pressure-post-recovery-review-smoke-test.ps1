param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

$url = "$BaseUrl/api/operational/queue-pressure/post-recovery-review"
$response = Invoke-RestMethod -Uri $url -Method Get

if ($null -eq $response.postRecoveryReview) {
    throw "Missing postRecoveryReview root object."
}

if ($response.postRecoveryReview.totalSectionCount -lt 4) {
    throw "Expected at least four post-recovery review sections."
}

Write-Host "Queue pressure post-recovery review smoke test passed."
