param([string]$BaseUrl = "https://localhost:55436")
$ErrorActionPreference = "Stop"
$endpointMap = Invoke-RestMethod -Method Get -Uri "$BaseUrl/api/system/endpoints" -ContentType "application/json"
$route = "/api/operational/dispatcher/pressure"
if (-not ($endpointMap | Where-Object { $_.routePattern -eq $route })) { throw "Missing route: $route" }
Write-Host "Found route: $route"
