param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

$base = $BaseUrl.TrimEnd('/')
$response = Invoke-RestMethod -Uri ($base + "/api/operational/queue-pressure/stability-index?horizon=current-run") -Method Get
$readiness = Invoke-RestMethod -Uri ($base + "/api/operational/queue-pressure/stability-index/readiness") -Method Get

if ($response.stabilityIndex.filters.horizon -ne "current-run") {
    throw "Expected horizon filter to echo current-run."
}

if ($true -ne $readiness.readiness.isAvailable) {
    throw "Readiness endpoint did not report availability."
}

if ($readiness.readiness.bandCount -lt 1) {
    throw "Readiness endpoint did not report stability bands."
}

Write-Host "Queue pressure stability index consistency smoke test passed."
