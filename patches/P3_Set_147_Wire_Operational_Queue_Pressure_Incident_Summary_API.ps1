$repoRoot = (Resolve-Path ".").Path
$startupPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"

if (-not (Test-Path $startupPath)) {
    throw "Could not find $startupPath"
}

$startup = Get-Content $startupPath -Raw

if ($startup -notmatch "MapOperationalQueuePressureIncidentSummaryApi\(") {
    if ($startup -match "api\.MapOperationalQueuePressureEscalationGuideApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureEscalationGuideApi\(\);", "api.MapOperationalQueuePressureEscalationGuideApi();`r`n        api.MapOperationalQueuePressureIncidentSummaryApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureOperatorChecklistApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureOperatorChecklistApi\(\);", "api.MapOperationalQueuePressureOperatorChecklistApi();`r`n        api.MapOperationalQueuePressureIncidentSummaryApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureRecommendationCatalogApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureRecommendationCatalogApi\(\);", "api.MapOperationalQueuePressureRecommendationCatalogApi();`r`n        api.MapOperationalQueuePressureIncidentSummaryApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureActionPlanApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureActionPlanApi\(\);", "api.MapOperationalQueuePressureActionPlanApi();`r`n        api.MapOperationalQueuePressureIncidentSummaryApi();"
    }
    else {
        throw "Could not locate operational endpoint insertion point. Expected a queue-pressure endpoint map call."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped operational queue pressure incident-summary endpoint."
}
else {
    Write-Host "Operational queue pressure incident-summary endpoint already mapped."
}
