param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"
$response = Invoke-RestMethod -Uri "$BaseUrl/api/operational/queue-pressure/operator-checklist" -Method Get

$requiredPhases = @("Validation", "Triage", "Response", "FollowUp")
foreach ($phase in $requiredPhases) {
    if ($response.operatorChecklist.phases -notcontains $phase) {
        throw "Missing expected checklist phase: $phase"
    }
}

$readiness = Invoke-RestMethod -Uri "$BaseUrl/api/operational/queue-pressure/operator-checklist/readiness" -Method Get
if ($true -ne $readiness.readiness.isAvailable) {
    throw "Readiness endpoint did not report availability."
}

Write-Host "Queue pressure operator-checklist consistency smoke test passed."
