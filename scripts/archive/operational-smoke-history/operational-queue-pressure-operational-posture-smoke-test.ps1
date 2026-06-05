param([string]$BaseUrl = "https://localhost:55436")
$ErrorActionPreference = "Stop"
$uri = "$BaseUrl/api/operational/queue-pressure/operational-posture?pressureLevel=High&trend=Worsening&readiness=Limited&mitigationState=Active"
$response = Invoke-RestMethod -Uri $uri -Method Get
if ($null -eq $response.operationalPosture) { throw "Missing operationalPosture root." }
if ([string]::IsNullOrWhiteSpace($response.operationalPosture.status.posture)) { throw "Missing posture." }
if ([string]::IsNullOrWhiteSpace($response.operationalPosture.status.operatingMode)) { throw "Missing operating mode." }
Write-Host "Smoke test passed for operational queue pressure operational posture."
