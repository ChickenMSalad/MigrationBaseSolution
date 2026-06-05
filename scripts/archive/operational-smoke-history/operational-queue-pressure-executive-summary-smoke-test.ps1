param([string]$BaseUrl = "https://localhost:55436")
$ErrorActionPreference = "Stop"
$uri = "$BaseUrl/api/operational/queue-pressure/executive-summary?pressureLevel=High&trend=Worsening&mitigationState=Active&recoveryState=Recovering"
$response = Invoke-RestMethod -Uri $uri -Method Get
if ($null -eq $response.executiveSummary) { throw "Missing executiveSummary root." }
if ([string]::IsNullOrWhiteSpace($response.executiveSummary.status.executiveStatus)) { throw "Missing executive status." }
if ([string]::IsNullOrWhiteSpace($response.executiveSummary.status.operatorMode)) { throw "Missing operator mode." }
Write-Host "Smoke test passed for operational queue pressure executive summary."
