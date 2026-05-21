$repoRoot = (Resolve-Path ".").Path
$startupPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"

if (-not (Test-Path $startupPath)) {
    throw "Could not find $startupPath"
}

$startup = Get-Content $startupPath -Raw

if ($startup -notmatch "MapOperationalQueuePressureRecoveryWorkflowApi\(") {
    if ($startup -match "api\.MapOperationalQueuePressureRunbookApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureRunbookApi\(\);", "api.MapOperationalQueuePressureRunbookApi();`r`n        api.MapOperationalQueuePressureRecoveryWorkflowApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureIncidentSummaryApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureIncidentSummaryApi\(\);", "api.MapOperationalQueuePressureIncidentSummaryApi();`r`n        api.MapOperationalQueuePressureRecoveryWorkflowApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureEscalationGuideApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureEscalationGuideApi\(\);", "api.MapOperationalQueuePressureEscalationGuideApi();`r`n        api.MapOperationalQueuePressureRecoveryWorkflowApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureOperatorChecklistApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureOperatorChecklistApi\(\);", "api.MapOperationalQueuePressureOperatorChecklistApi();`r`n        api.MapOperationalQueuePressureRecoveryWorkflowApi();"
    }
    else {
        throw "Could not locate operational endpoint insertion point. Expected a queue-pressure endpoint map call."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped operational queue pressure recovery workflow endpoint."
}
else {
    Write-Host "Operational queue pressure recovery workflow endpoint already mapped."
}
