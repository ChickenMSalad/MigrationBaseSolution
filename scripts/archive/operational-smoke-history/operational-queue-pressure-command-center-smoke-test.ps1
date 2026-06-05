param([string]$BaseUrl = "https://localhost:55436")
$ErrorActionPreference = "Stop"
$uri = "$BaseUrl/api/operational/queue-pressure/command-center?pressureLevel=High&queueTrend=Worsening&dispatcherState=Degraded&incidentState=Watching"
$response = Invoke-RestMethod -Uri $uri -Method Get
if ($null -eq $response.commandCenter) { throw "Missing commandCenter root." }
if ([string]::IsNullOrWhiteSpace($response.commandCenter.commandSummary.severity)) { throw "Missing severity." }
if ([string]::IsNullOrWhiteSpace($response.commandCenter.commandSummary.operatingMode)) { throw "Missing operating mode." }
Write-Host "Smoke test passed for operational queue pressure command center."
