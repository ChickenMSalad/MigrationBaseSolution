$repoRoot = (Resolve-Path ".").Path
$startupPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"

if (-not (Test-Path $startupPath)) {
    throw "Could not find $startupPath"
}

$startup = Get-Content $startupPath -Raw

if ($startup -notmatch "MapOperationalQueuePressureActionPlanApi\(") {
    if ($startup -match "api\.MapOperationalQueuePressureTrendApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureTrendApi\(\);", "api.MapOperationalQueuePressureTrendApi();`r`n        api.MapOperationalQueuePressureActionPlanApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureDashboardApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureDashboardApi\(\);", "api.MapOperationalQueuePressureDashboardApi();`r`n        api.MapOperationalQueuePressureActionPlanApi();"
    }
    elseif ($startup -match "api\.MapOperationalGlobalDispatcherPressureAnalyticsEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalDispatcherPressureAnalyticsEndpoints\(\);", "api.MapOperationalGlobalDispatcherPressureAnalyticsEndpoints();`r`n        api.MapOperationalQueuePressureActionPlanApi();"
    }
    elseif ($startup -match "api\.MapOperationalGlobalQueueDepthAnalyticsEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalQueueDepthAnalyticsEndpoints\(\);", "api.MapOperationalGlobalQueueDepthAnalyticsEndpoints();`r`n        api.MapOperationalQueuePressureActionPlanApi();"
    }
    else {
        throw "Could not locate operational endpoint insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped operational queue pressure action-plan endpoint."
}
else {
    Write-Host "Operational queue pressure action-plan endpoint already mapped."
}
