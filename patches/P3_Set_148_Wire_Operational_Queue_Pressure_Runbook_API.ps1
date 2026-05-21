$repoRoot = (Resolve-Path ".").Path
$startupPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"

if (-not (Test-Path $startupPath)) {
    throw "Could not find $startupPath"
}

$startup = Get-Content $startupPath -Raw

if ($startup -notmatch "MapOperationalQueuePressureRunbookApi\(") {
    if ($startup -match "api\.MapOperationalQueuePressureIncidentSummaryApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureIncidentSummaryApi\(\);", "api.MapOperationalQueuePressureIncidentSummaryApi();`r`n        api.MapOperationalQueuePressureRunbookApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureEscalationGuideApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureEscalationGuideApi\(\);", "api.MapOperationalQueuePressureEscalationGuideApi();`r`n        api.MapOperationalQueuePressureRunbookApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureOperatorChecklistApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureOperatorChecklistApi\(\);", "api.MapOperationalQueuePressureOperatorChecklistApi();`r`n        api.MapOperationalQueuePressureRunbookApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureRecommendationCatalogApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureRecommendationCatalogApi\(\);", "api.MapOperationalQueuePressureRecommendationCatalogApi();`r`n        api.MapOperationalQueuePressureRunbookApi();"
    }
    else {
        throw "Could not locate operational endpoint insertion point. Expected a queue-pressure endpoint map call."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped operational queue pressure runbook endpoint."
}
else {
    Write-Host "Operational queue pressure runbook endpoint already mapped."
}
