$repoRoot = (Resolve-Path ".").Path
$startupPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"

if (-not (Test-Path $startupPath)) {
    throw "Could not find $startupPath"
}

$startup = Get-Content $startupPath -Raw

if ($startup -notmatch "MapOperationalQueuePressureTrendApi\(") {
    if ($startup -match "api\.MapOperationalQueuePressureDashboardApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureDashboardApi\(\);", "api.MapOperationalQueuePressureDashboardApi();`r`n        api.MapOperationalQueuePressureTrendApi();"
    }
    elseif ($startup -match "api\.MapOperationalGlobalDispatcherPressureAnalyticsEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalDispatcherPressureAnalyticsEndpoints\(\);", "api.MapOperationalGlobalDispatcherPressureAnalyticsEndpoints();`r`n        api.MapOperationalQueuePressureTrendApi();"
    }
    elseif ($startup -match "api\.MapOperationalGlobalQueueDepthAnalyticsEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalQueueDepthAnalyticsEndpoints\(\);", "api.MapOperationalGlobalQueueDepthAnalyticsEndpoints();`r`n        api.MapOperationalQueuePressureTrendApi();"
    }
    else {
        throw "Could not locate operational endpoint insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped operational queue pressure trend endpoint."
}
else {
    Write-Host "Operational queue pressure trend endpoint already mapped."
}
