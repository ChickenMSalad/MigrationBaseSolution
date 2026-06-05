param([string]$BaseUrl = "https://localhost:55436")
$ErrorActionPreference = "Stop"
$uri = "$BaseUrl/api/operational/queue-pressure/finalization?pressureLevel=Normal&posture=Normal&readiness=Ready&recoveryState=None&openActions=0"
$response = Invoke-RestMethod -Uri $uri -Method Get
if ($null -eq $response.queuePressureFinalization) { throw "Missing queuePressureFinalization root." }
if ([string]::IsNullOrWhiteSpace($response.queuePressureFinalization.status.finalizationState)) { throw "Missing finalization state." }
if ($null -eq $response.queuePressureFinalization.closeoutChecklist) { throw "Missing closeout checklist." }
Write-Host "Smoke test passed for operational queue pressure finalization."
