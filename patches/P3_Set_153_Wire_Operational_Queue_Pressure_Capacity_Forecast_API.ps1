$repoRoot = (Resolve-Path ".").Path
$startupPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"

if (-not (Test-Path $startupPath)) {
    throw "Could not find $startupPath"
}

$startup = Get-Content $startupPath -Raw

if ($startup -notmatch "MapOperationalQueuePressureCapacityForecastApi\(") {
    if ($startup -match "api\.MapOperationalQueuePressureCapacityGuardrailsApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureCapacityGuardrailsApi\(\);", "api.MapOperationalQueuePressureCapacityGuardrailsApi();`r`n        api.MapOperationalQueuePressureCapacityForecastApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureStabilityIndexApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureStabilityIndexApi\(\);", "api.MapOperationalQueuePressureStabilityIndexApi();`r`n        api.MapOperationalQueuePressureCapacityForecastApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressurePostRecoveryReviewApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressurePostRecoveryReviewApi\(\);", "api.MapOperationalQueuePressurePostRecoveryReviewApi();`r`n        api.MapOperationalQueuePressureCapacityForecastApi();"
    }
    else {
        throw "Could not locate operational endpoint insertion point. Expected a queue-pressure endpoint map call."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped operational queue pressure capacity forecast endpoint."
}
else {
    Write-Host "Operational queue pressure capacity forecast endpoint already mapped."
}
