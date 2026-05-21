$repoRoot = (Resolve-Path ".").Path
$startupPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"

if (-not (Test-Path $startupPath)) {
    throw "Could not find $startupPath"
}

$startup = Get-Content $startupPath -Raw

if ($startup -notmatch "MapOperationalQueuePressureExecutionReadinessApi\(") {
    if ($startup -match "api\.MapOperationalQueuePressureSafetyReviewApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureSafetyReviewApi\(\);", "api.MapOperationalQueuePressureSafetyReviewApi();`r`n        api.MapOperationalQueuePressureExecutionReadinessApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureAutoMitigationApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureAutoMitigationApi\(\);", "api.MapOperationalQueuePressureAutoMitigationApi();`r`n        api.MapOperationalQueuePressureExecutionReadinessApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureThrottlePolicyApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureThrottlePolicyApi\(\);", "api.MapOperationalQueuePressureThrottlePolicyApi();`r`n        api.MapOperationalQueuePressureExecutionReadinessApi();"
    }
    else {
        throw "Could not locate operational endpoint insertion point. Expected a queue-pressure endpoint map call."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped operational queue pressure execution readiness endpoint."
}
else {
    Write-Host "Operational queue pressure execution readiness endpoint already mapped."
}
