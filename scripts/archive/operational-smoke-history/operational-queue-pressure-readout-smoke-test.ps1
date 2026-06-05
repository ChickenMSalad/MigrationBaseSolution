param([string]$BaseUrl = "https://localhost:55436")
$ErrorActionPreference = "Stop"
$uri = "$BaseUrl/api/operational/queue-pressure/readout?pressureLevel=High&posture=Degraded&readiness=Limited&recoveryState=Active"
$response = Invoke-RestMethod -Uri $uri -Method Get
if ($null -eq $response.queuePressureReadout) { throw "Missing queuePressureReadout root." }
if ([string]::IsNullOrWhiteSpace($response.queuePressureReadout.status.readoutLevel)) { throw "Missing readout level." }
if ([string]::IsNullOrWhiteSpace($response.queuePressureReadout.status.operatorSummary)) { throw "Missing operator summary." }
Write-Host "Smoke test passed for operational queue pressure readout."
