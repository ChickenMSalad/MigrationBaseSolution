$repoRoot = (Resolve-Path ".").Path
$startupPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"

if (-not (Test-Path $startupPath)) {
    throw "Could not find $startupPath"
}

$startup = Get-Content $startupPath -Raw

if ($startup -notmatch "MapOperationalQueuePressureAutoMitigationApi\(") {
    if ($startup -match "api\.MapOperationalQueuePressureThrottlePolicyApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureThrottlePolicyApi\(\);", "api.MapOperationalQueuePressureThrottlePolicyApi();`r`n        api.MapOperationalQueuePressureAutoMitigationApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureCapacityForecastApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureCapacityForecastApi\(\);", "api.MapOperationalQueuePressureCapacityForecastApi();`r`n        api.MapOperationalQueuePressureAutoMitigationApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureCapacityGuardrailsApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureCapacityGuardrailsApi\(\);", "api.MapOperationalQueuePressureCapacityGuardrailsApi();`r`n        api.MapOperationalQueuePressureAutoMitigationApi();"
    }
    else {
        throw "Could not locate operational endpoint insertion point. Expected a queue-pressure endpoint map call."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped operational queue pressure auto mitigation endpoint."
}
else {
    Write-Host "Operational queue pressure auto mitigation endpoint already mapped."
}
