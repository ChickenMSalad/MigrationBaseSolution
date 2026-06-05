param([string]$BaseUrl = "https://localhost:55436")
$ErrorActionPreference = "Stop"
$uri = "$BaseUrl/api/operational/queue-pressure/control-tower?pressureLevel=High&queueTrend=Worsening&dispatcherState=Degraded"
$response = Invoke-RestMethod -Uri $uri -Method Get
if ($null -eq $response.controlTower) { throw "Missing controlTower root." }
if ([string]::IsNullOrWhiteSpace($response.controlTower.summary.severity)) { throw "Missing severity." }
Write-Host "Smoke test passed for operational queue pressure control tower."
