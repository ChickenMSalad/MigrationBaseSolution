$repoRoot = (Resolve-Path ".").Path
$startupPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"

if (-not (Test-Path $startupPath)) {
    throw "Could not find $startupPath"
}

$startup = Get-Content $startupPath -Raw

if ($startup -notmatch "MapOperationalQueuePressureCapacityGuardrailsApi\(") {
    if ($startup -match "api\.MapOperationalQueuePressureStabilityIndexApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureStabilityIndexApi\(\);", "api.MapOperationalQueuePressureStabilityIndexApi();`r`n        api.MapOperationalQueuePressureCapacityGuardrailsApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressurePostRecoveryReviewApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressurePostRecoveryReviewApi\(\);", "api.MapOperationalQueuePressurePostRecoveryReviewApi();`r`n        api.MapOperationalQueuePressureCapacityGuardrailsApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureRecoveryWorkflowApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureRecoveryWorkflowApi\(\);", "api.MapOperationalQueuePressureRecoveryWorkflowApi();`r`n        api.MapOperationalQueuePressureCapacityGuardrailsApi();"
    }
    else {
        throw "Could not locate operational endpoint insertion point. Expected a queue-pressure endpoint map call."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped operational queue pressure capacity guardrails endpoint."
}
else {
    Write-Host "Operational queue pressure capacity guardrails endpoint already mapped."
}
