$repoRoot = (Resolve-Path ".").Path
$startupPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"

if (-not (Test-Path $startupPath)) {
    throw "Could not find $startupPath"
}

$startup = Get-Content $startupPath -Raw

if ($startup -notmatch "MapOperationalQueuePressureOperatorChecklistApi\(") {
    if ($startup -match "api\.MapOperationalQueuePressureRecommendationCatalogApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureRecommendationCatalogApi\(\);", "api.MapOperationalQueuePressureRecommendationCatalogApi();`r`n        api.MapOperationalQueuePressureOperatorChecklistApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureActionPlanApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureActionPlanApi\(\);", "api.MapOperationalQueuePressureActionPlanApi();`r`n        api.MapOperationalQueuePressureOperatorChecklistApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureTrendApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureTrendApi\(\);", "api.MapOperationalQueuePressureTrendApi();`r`n        api.MapOperationalQueuePressureOperatorChecklistApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureDashboardApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureDashboardApi\(\);", "api.MapOperationalQueuePressureDashboardApi();`r`n        api.MapOperationalQueuePressureOperatorChecklistApi();"
    }
    else {
        throw "Could not locate operational endpoint insertion point. Expected a queue-pressure endpoint map call."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped operational queue pressure operator-checklist endpoint."
}
else {
    Write-Host "Operational queue pressure operator-checklist endpoint already mapped."
}
