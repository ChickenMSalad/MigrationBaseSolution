param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path ".").Path
$operationalEndpointRoot = Join-Path $repoRoot "src\Migration.Admin.Api\Endpoints\Operational"

if (-not (Test-Path $operationalEndpointRoot)) {
    throw "Could not find $operationalEndpointRoot"
}

Write-Host "=== Operational route prefix source/runtime check ==="
Write-Host ""

Write-Host "Checking source for hardcoded /api/operational route prefixes..."
$hardcodedApiRoutes = Get-ChildItem -Path $operationalEndpointRoot -Recurse -File -Filter "*.cs" |
    Select-String -Pattern 'Map(Get|Post|Put|Delete|Patch)\(\s*"/api/operational/'

if ($hardcodedApiRoutes) {
    Write-Host "Found hardcoded /api operational route prefixes:"
    $hardcodedApiRoutes | ForEach-Object {
        Write-Host " - $($_.Path):$($_.LineNumber): $($_.Line.Trim())"
    }

    throw "Source still contains hardcoded /api/operational route prefixes under Endpoints\Operational."
}

Write-Host "Source check passed."
Write-Host ""

Write-Host "Checking runtime for accidental /api/api/operational routes..."
$endpointMap = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/system/endpoints" `
    -ContentType "application/json"

$doubleApiRoutes = $endpointMap |
    Where-Object { $_.routePattern -like "/api/api/operational/*" } |
    Sort-Object routePattern

if ($doubleApiRoutes) {
    Write-Host "Runtime still exposes accidental /api/api/operational routes:"
    $doubleApiRoutes | ForEach-Object {
        Write-Host " - $($_.routePattern)"
    }

    throw "Runtime still exposes /api/api/operational routes. Rebuild/restart may be needed, or source still contains route prefixes."
}

Write-Host "Runtime double-prefix check passed."
Write-Host ""

Write-Host "Checking required diagnostic routes now resolve at canonical /api/operational paths..."

$requiredRoutes = @(
    "/api/operational/mirror/status",
    "/api/operational/mirror/readiness",
    "/api/operational/mirror/enablement-guard",
    "/api/operational/mirror/write-verification",
    "/api/operational/mirror/last-invocation",
    "/api/operational/sql/schema/smoke-test"
)

foreach ($route in $requiredRoutes) {
    $match = $endpointMap | Where-Object { $_.routePattern -eq $route }

    if (-not $match) {
        throw "Missing canonical route after prefix repair: $route"
    }

    Write-Host "Found canonical route: $route"
}

Write-Host ""
Write-Host "Operational route prefix source/runtime check passed."
