$repoRoot = (Resolve-Path ".").Path
$startupPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"

if (-not (Test-Path $startupPath)) {
    throw "Could not find $startupPath"
}

$startup = Get-Content $startupPath -Raw

if ($startup -notmatch "MapOperationalQueuePressureRecoveryReadinessApi\(") {
    if ($startup -match "api\.MapOperationalQueuePressureExecutionReadinessApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureExecutionReadinessApi\(\);", "api.MapOperationalQueuePressureExecutionReadinessApi();`r`n        api.MapOperationalQueuePressureRecoveryReadinessApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureSafetyReviewApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureSafetyReviewApi\(\);", "api.MapOperationalQueuePressureSafetyReviewApi();`r`n        api.MapOperationalQueuePressureRecoveryReadinessApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureAutoMitigationApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureAutoMitigationApi\(\);", "api.MapOperationalQueuePressureAutoMitigationApi();`r`n        api.MapOperationalQueuePressureRecoveryReadinessApi();"
    }
    else {
        throw "Could not locate operational endpoint insertion point. Expected a queue-pressure endpoint map call."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped operational queue pressure recovery readiness endpoint."
}
else {
    Write-Host "Operational queue pressure recovery readiness endpoint already mapped."
}
