$repoRoot = (Resolve-Path ".").Path
$startupPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"

if (-not (Test-Path $startupPath)) {
    throw "Could not find $startupPath"
}

$startup = Get-Content $startupPath -Raw

if ($startup -notmatch "MapOperationalQueuePressureDashboardApi\(") {
    if ($startup -match "api\.MapOperationalGlobalDispatcherPressureAnalyticsEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalDispatcherPressureAnalyticsEndpoints\(\);", "api.MapOperationalGlobalDispatcherPressureAnalyticsEndpoints();`r`n        api.MapOperationalQueuePressureDashboardApi();"
    }
    elseif ($startup -match "api\.MapOperationalGlobalQueueDepthAnalyticsEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalQueueDepthAnalyticsEndpoints\(\);", "api.MapOperationalGlobalQueueDepthAnalyticsEndpoints();`r`n        api.MapOperationalQueuePressureDashboardApi();"
    }
    elseif ($startup -match "api\.MapOperationalGlobalRunHealthOperationsCenterEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalRunHealthOperationsCenterEndpoints\(\);", "api.MapOperationalGlobalRunHealthOperationsCenterEndpoints();`r`n        api.MapOperationalQueuePressureDashboardApi();"
    }
    elseif ($startup -match "api\.MapOperationalGlobalRunHealthActionPlanEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalRunHealthActionPlanEndpoints\(\);", "api.MapOperationalGlobalRunHealthActionPlanEndpoints();`r`n        api.MapOperationalQueuePressureDashboardApi();"
    }
    else {
        throw "Could not locate operational endpoint insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped operational queue pressure dashboard endpoint."
}
else {
    Write-Host "Operational queue pressure dashboard endpoint already mapped."
}
