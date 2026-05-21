$repoRoot = (Resolve-Path ".").Path
$startupPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"

if (-not (Test-Path $startupPath)) {
    throw "Could not find $startupPath"
}

$startup = Get-Content $startupPath -Raw

if ($startup -notmatch "MapOperationalQueuePressureSafetyReviewApi\(") {
    if ($startup -match "api\.MapOperationalQueuePressureAutoMitigationApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureAutoMitigationApi\(\);", "api.MapOperationalQueuePressureAutoMitigationApi();`r`n        api.MapOperationalQueuePressureSafetyReviewApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureThrottlePolicyApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureThrottlePolicyApi\(\);", "api.MapOperationalQueuePressureThrottlePolicyApi();`r`n        api.MapOperationalQueuePressureSafetyReviewApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureCapacityForecastApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureCapacityForecastApi\(\);", "api.MapOperationalQueuePressureCapacityForecastApi();`r`n        api.MapOperationalQueuePressureSafetyReviewApi();"
    }
    else {
        throw "Could not locate operational endpoint insertion point. Expected a queue-pressure endpoint map call."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped operational queue pressure safety review endpoint."
}
else {
    Write-Host "Operational queue pressure safety review endpoint already mapped."
}
