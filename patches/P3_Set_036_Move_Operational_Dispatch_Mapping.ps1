$repoRoot = (Resolve-Path ".").Path

$programPath = Join-Path $repoRoot "src\Migration.Admin.Api\Program.cs"
$startupPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"

if (-not (Test-Path $programPath)) {
    throw "Could not find $programPath"
}

if (-not (Test-Path $startupPath)) {
    throw "Could not find $startupPath"
}

$program = Get-Content $programPath -Raw
$startup = Get-Content $startupPath -Raw

# Remove direct operational endpoint mapping from Program.cs.
$program = $program -replace "\r?\napi\.MapOperationalDispatchEndpoints\(\);", ""

# Add route-group mapping inside AdminApiEndpointStartupExtensions if missing.
if ($startup -notmatch "MapOperationalDispatchEndpoints\(") {
    $pattern = "(MapMigrationAdminApiRouteGroupEndpoints\s*\(\s*RouteGroupBuilder\s+api\s*\)\s*\{)"
    if ($startup -match $pattern) {
        $startup = $startup -replace $pattern, "`$1`r`n        api.MapOperationalDispatchEndpoints();"
    }
    else {
        throw "Could not locate MapMigrationAdminApiRouteGroupEndpoints(RouteGroupBuilder api) in $startupPath. Apply manually using README instructions."
    }
}

Set-Content -Path $programPath -Value $program -NoNewline
Set-Content -Path $startupPath -Value $startup -NoNewline

Write-Host "Moved operational dispatch endpoint mapping into AdminApiEndpointStartupExtensions."
