$repoRoot = (Resolve-Path ".").Path
$startupPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"

if (-not (Test-Path $startupPath)) {
    throw "Could not find $startupPath"
}

$startup = Get-Content $startupPath -Raw

if ($startup -notmatch "MapOperationalQueuePressureOperatorAdvisoryApi\(") {
    if ($startup -match "api\.MapOperationalQueuePressureRecoveryReadinessApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureRecoveryReadinessApi\(\);", "api.MapOperationalQueuePressureRecoveryReadinessApi();`r`n        api.MapOperationalQueuePressureOperatorAdvisoryApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureExecutionReadinessApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureExecutionReadinessApi\(\);", "api.MapOperationalQueuePressureExecutionReadinessApi();`r`n        api.MapOperationalQueuePressureOperatorAdvisoryApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureSafetyReviewApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureSafetyReviewApi\(\);", "api.MapOperationalQueuePressureSafetyReviewApi();`r`n        api.MapOperationalQueuePressureOperatorAdvisoryApi();"
    }
    else {
        throw "Could not locate operational endpoint insertion point. Expected a queue-pressure endpoint map call."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped operational queue pressure operator advisory endpoint."
}
else {
    Write-Host "Operational queue pressure operator advisory endpoint already mapped."
}
