param([string]$BaseUrl = "https://localhost:55436")
$ErrorActionPreference = "Stop"
$response = Invoke-RestMethod -Uri "$BaseUrl/api/operational/queue-pressure/executive-summary/readiness" -Method Get
if ($null -eq $response.readiness) { throw "Missing readiness root." }
if ($response.readiness.isAvailable -ne $true) { throw "Readiness endpoint did not report availability." }
Write-Host "Consistency smoke test passed for operational queue pressure executive summary."
