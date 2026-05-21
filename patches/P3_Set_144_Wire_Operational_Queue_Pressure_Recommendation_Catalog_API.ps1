$repoRoot = (Resolve-Path ".").Path
$startupPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"

if (-not (Test-Path $startupPath)) {
    throw "Could not find $startupPath"
}

$startup = Get-Content $startupPath -Raw

if ($startup -notmatch "MapOperationalQueuePressureRecommendationCatalogApi\(") {
    if ($startup -match "api\.MapOperationalQueuePressureActionPlanApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureActionPlanApi\(\);", "api.MapOperationalQueuePressureActionPlanApi();`r`n        api.MapOperationalQueuePressureRecommendationCatalogApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureTrendApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureTrendApi\(\);", "api.MapOperationalQueuePressureTrendApi();`r`n        api.MapOperationalQueuePressureRecommendationCatalogApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureDashboardApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureDashboardApi\(\);", "api.MapOperationalQueuePressureDashboardApi();`r`n        api.MapOperationalQueuePressureRecommendationCatalogApi();"
    }
    elseif ($startup -match "api\.MapOperationalGlobalDispatcherPressureAnalyticsEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalDispatcherPressureAnalyticsEndpoints\(\);", "api.MapOperationalGlobalDispatcherPressureAnalyticsEndpoints();`r`n        api.MapOperationalQueuePressureRecommendationCatalogApi();"
    }
    elseif ($startup -match "api\.MapOperationalGlobalQueueDepthAnalyticsEndpoints\(\);") {
        $startup = $startup -replace "api\.MapOperationalGlobalQueueDepthAnalyticsEndpoints\(\);", "api.MapOperationalGlobalQueueDepthAnalyticsEndpoints();`r`n        api.MapOperationalQueuePressureRecommendationCatalogApi();"
    }
    else {
        throw "Could not locate operational endpoint insertion point."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped operational queue pressure recommendation-catalog endpoint."
}
else {
    Write-Host "Operational queue pressure recommendation-catalog endpoint already mapped."
}
