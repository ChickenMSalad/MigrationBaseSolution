$repoRoot = (Resolve-Path ".").Path
$startupPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"

if (-not (Test-Path $startupPath)) {
    throw "Could not find $startupPath"
}

$startup = Get-Content $startupPath -Raw

if ($startup -notmatch "MapOperationalQueuePressureEscalationGuideApi\(") {
    if ($startup -match "api\.MapOperationalQueuePressureOperatorChecklistApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureOperatorChecklistApi\(\);", "api.MapOperationalQueuePressureOperatorChecklistApi();`r`n        api.MapOperationalQueuePressureEscalationGuideApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureRecommendationCatalogApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureRecommendationCatalogApi\(\);", "api.MapOperationalQueuePressureRecommendationCatalogApi();`r`n        api.MapOperationalQueuePressureEscalationGuideApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureActionPlanApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureActionPlanApi\(\);", "api.MapOperationalQueuePressureActionPlanApi();`r`n        api.MapOperationalQueuePressureEscalationGuideApi();"
    }
    else {
        throw "Could not locate operational endpoint insertion point. Expected a queue-pressure endpoint map call."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped operational queue pressure escalation-guide endpoint."
}
else {
    Write-Host "Operational queue pressure escalation-guide endpoint already mapped."
}
