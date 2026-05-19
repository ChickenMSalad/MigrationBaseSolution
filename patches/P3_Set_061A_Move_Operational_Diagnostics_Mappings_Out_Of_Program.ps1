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

$program = $program -replace "\r?\napp\.MapOperationalMirrorDiagnosticsEndpoints\(\);", ""
$program = $program -replace "\r?\napp\.MapOperationalSqlSchemaDiagnosticsEndpoints\(\);", ""

if ($startup -notmatch "MapOperationalMirrorDiagnosticsEndpoints\(") {
    if ($startup -match "api\.MapOperationalDispatchEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalDispatchEndpoints\(\);", "api.MapOperationalDispatchEndpoints();`r`n        api.MapOperationalMirrorDiagnosticsEndpoints();"
    }
    else {
        throw "Could not find api.MapOperationalDispatchEndpoints(); in AdminApiEndpointStartupExtensions.cs"
    }
}

if ($startup -notmatch "MapOperationalSqlSchemaDiagnosticsEndpoints\(") {
    if ($startup -match "api\.MapOperationalMirrorDiagnosticsEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalMirrorDiagnosticsEndpoints\(\);", "api.MapOperationalMirrorDiagnosticsEndpoints();`r`n        api.MapOperationalSqlSchemaDiagnosticsEndpoints();"
    }
    else {
        throw "Could not find api.MapOperationalMirrorDiagnosticsEndpoints(); in AdminApiEndpointStartupExtensions.cs"
    }
}

Set-Content -Path $programPath -Value $program -NoNewline
Set-Content -Path $startupPath -Value $startup -NoNewline

Write-Host "Moved operational diagnostics mappings from Program.cs into AdminApiEndpointStartupExtensions.cs."
