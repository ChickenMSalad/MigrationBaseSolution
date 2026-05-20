$repoRoot = (Resolve-Path ".").Path
$startupPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"
$auditPath = Join-Path $repoRoot "scripts\operational-api-surface-audit.ps1"

if (-not (Test-Path $startupPath)) {
    throw "Could not find $startupPath"
}

if (-not (Test-Path $auditPath)) {
    throw "Could not find $auditPath"
}

$startup = Get-Content $startupPath -Raw

# Restore diagnostic endpoint mappings that should live in AdminApiEndpointStartupExtensions,
# not Program.cs. These endpoint classes already exist under Endpoints\Operational\Diagnostics.
if ($startup -notmatch "MapOperationalMirrorDiagnosticsEndpoints\(") {
    if ($startup -match "api\.MapOperationalMetricsEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalMetricsEndpoints\(\);", "api.MapOperationalMirrorDiagnosticsEndpoints();`r`n        api.MapOperationalMetricsEndpoints();"
        Write-Host "Mapped operational mirror diagnostics endpoints."
    }
    elseif ($startup -match "api\.MapOperationalDispatcherEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalDispatcherEndpoints\(\);", "api.MapOperationalMirrorDiagnosticsEndpoints();`r`n        api.MapOperationalDispatcherEndpoints();"
        Write-Host "Mapped operational mirror diagnostics endpoints."
    }
    else {
        throw "Could not locate insertion point for MapOperationalMirrorDiagnosticsEndpoints."
    }
}
else {
    Write-Host "Operational mirror diagnostics endpoints already mapped."
}

if ($startup -notmatch "MapOperationalSqlSchemaDiagnosticsEndpoints\(") {
    if ($startup -match "api\.MapOperationalMirrorDiagnosticsEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalMirrorDiagnosticsEndpoints\(\);", "api.MapOperationalMirrorDiagnosticsEndpoints();`r`n        api.MapOperationalSqlSchemaDiagnosticsEndpoints();"
        Write-Host "Mapped operational SQL schema diagnostics endpoints."
    }
    else {
        throw "Could not locate insertion point for MapOperationalSqlSchemaDiagnosticsEndpoints."
    }
}
else {
    Write-Host "Operational SQL schema diagnostics endpoints already mapped."
}

Set-Content -Path $startupPath -Value $startup -NoNewline

# Correct the Set 100 audit route expectation.
# The lease expiration API uses the same route pattern for GET detection and POST reclaim.
# /expired-leases/reclaim is not an actual route in the current API surface.
$audit = Get-Content $auditPath -Raw
if ($audit -match '"/api/operational/work-items/expired-leases/reclaim"') {
    $audit = $audit -replace '\s*"/api/operational/work-items/expired-leases/reclaim",?\r?\n', ''
    Set-Content -Path $auditPath -Value $audit -NoNewline
    Write-Host "Removed non-existent expired lease reclaim route expectation from audit."
}
else {
    Write-Host "Expired lease reclaim route expectation already absent."
}

Write-Host ""
Write-Host "100A repair complete."
Write-Host "Rebuild/restart Admin API, then rerun:"
Write-Host "./scripts/operational-api-surface-audit.ps1 -BaseUrl `"https://localhost:55436`""
