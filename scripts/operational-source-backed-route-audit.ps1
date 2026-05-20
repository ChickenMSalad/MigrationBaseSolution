param(
    [string]$BaseUrl = "https://localhost:55436"
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path ".").Path
$endpointFile = Join-Path $repoRoot "src\Migration.Admin.Api\Endpoints\Operational\Diagnostics\OperationalMirrorDiagnosticsEndpointExtensions.cs"
$sqlEndpointFile = Join-Path $repoRoot "src\Migration.Admin.Api\Endpoints\Operational\Diagnostics\OperationalSqlSchemaDiagnosticsEndpointExtensions.cs"
$startupFile = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"

if (-not (Test-Path $endpointFile)) {
    throw "Missing source file: $endpointFile"
}

if (-not (Test-Path $sqlEndpointFile)) {
    throw "Missing source file: $sqlEndpointFile"
}

if (-not (Test-Path $startupFile)) {
    throw "Missing source file: $startupFile"
}

Write-Host "=== Source-backed operational route audit ==="
Write-Host ""

Write-Host "Startup mapping evidence:"
$startup = Get-Content $startupFile -Raw
$startup |
    Select-String -Pattern "MapOperationalMirrorDiagnosticsEndpoints|MapOperationalSqlSchemaDiagnosticsEndpoints" -AllMatches |
    ForEach-Object { $_.Matches.Value } |
    Sort-Object -Unique |
    ForEach-Object { Write-Host " - $_" }

Write-Host ""
Write-Host "Mirror diagnostics source route strings:"
$mirrorSource = Get-Content $endpointFile -Raw
$mirrorRoutes = [regex]::Matches($mirrorSource, 'Map(Get|Post|Put|Delete)\(\s*"([^"]+)"') |
    ForEach-Object {
        [pscustomobject]@{
            Method = $_.Groups[1].Value.ToUpperInvariant()
            Route = $_.Groups[2].Value
            ExpectedRuntimeRoute = "/api" + $_.Groups[2].Value
        }
    }

$mirrorRoutes | Format-Table -AutoSize

Write-Host ""
Write-Host "SQL diagnostics source route strings:"
$sqlSource = Get-Content $sqlEndpointFile -Raw
$sqlRoutes = [regex]::Matches($sqlSource, 'Map(Get|Post|Put|Delete)\(\s*"([^"]+)"') |
    ForEach-Object {
        [pscustomobject]@{
            Method = $_.Groups[1].Value.ToUpperInvariant()
            Route = $_.Groups[2].Value
            ExpectedRuntimeRoute = "/api" + $_.Groups[2].Value
        }
    }

$sqlRoutes | Format-Table -AutoSize

$sourceRoutes = @($mirrorRoutes + $sqlRoutes)

Write-Host ""
Write-Host "Runtime endpoint map operational diagnostics subset:"
$runtimeRoutes = Invoke-RestMethod `
    -Method Get `
    -Uri "$BaseUrl/api/system/endpoints" `
    -ContentType "application/json"

$runtimeDiagnosticRoutes = $runtimeRoutes |
    Where-Object {
        $_.routePattern -like "/api/operational/mirror/*" -or
        $_.routePattern -like "/api/operational/sql/*"
    } |
    Sort-Object routePattern

$runtimeDiagnosticRoutes |
    Select-Object routePattern, methods, displayName |
    Format-Table -AutoSize

Write-Host ""
Write-Host "Comparing source-derived expected routes to runtime:"

$missing = @()

foreach ($route in $sourceRoutes) {
    $match = $runtimeRoutes | Where-Object { $_.routePattern -eq $route.ExpectedRuntimeRoute }

    if ($match) {
        Write-Host "FOUND   $($route.ExpectedRuntimeRoute)"
    }
    else {
        Write-Host "MISSING $($route.ExpectedRuntimeRoute)"
        $missing += $route.ExpectedRuntimeRoute
    }
}

Write-Host ""
Write-Host "Summary:"
Write-Host "Source-derived diagnostic route count: $($sourceRoutes.Count)"
Write-Host "Missing source-derived diagnostic route count: $($missing.Count)"

if ($missing.Count -gt 0) {
    Write-Host ""
    Write-Host "Missing routes:"
    $missing | ForEach-Object { Write-Host " - $_" }

    throw "Source-backed operational diagnostics route audit failed."
}

Write-Host "Source-backed operational diagnostics route audit passed."
