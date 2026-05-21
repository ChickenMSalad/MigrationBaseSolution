$repoRoot = (Resolve-Path ".").Path
$startupPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"

if (-not (Test-Path $startupPath)) {
    throw "Could not find $startupPath"
}

$startup = Get-Content $startupPath -Raw

if ($startup -notmatch "MapOperationalQueuePressureStabilityIndexApi\(") {
    if ($startup -match "api\.MapOperationalQueuePressurePostRecoveryReviewApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressurePostRecoveryReviewApi\(\);", "api.MapOperationalQueuePressurePostRecoveryReviewApi();`r`n        api.MapOperationalQueuePressureStabilityIndexApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureRecoveryWorkflowApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureRecoveryWorkflowApi\(\);", "api.MapOperationalQueuePressureRecoveryWorkflowApi();`r`n        api.MapOperationalQueuePressureStabilityIndexApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureRunbookApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureRunbookApi\(\);", "api.MapOperationalQueuePressureRunbookApi();`r`n        api.MapOperationalQueuePressureStabilityIndexApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureIncidentSummaryApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureIncidentSummaryApi\(\);", "api.MapOperationalQueuePressureIncidentSummaryApi();`r`n        api.MapOperationalQueuePressureStabilityIndexApi();"
    }
    else {
        throw "Could not locate operational endpoint insertion point. Expected a queue-pressure endpoint map call."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped operational queue pressure stability index endpoint."
}
else {
    Write-Host "Operational queue pressure stability index endpoint already mapped."
}
