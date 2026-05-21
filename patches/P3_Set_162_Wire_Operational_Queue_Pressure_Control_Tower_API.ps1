$repoRoot = (Resolve-Path ".").Path
$startupPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"

if (-not (Test-Path $startupPath)) {
    throw "Could not find $startupPath"
}

$startup = Get-Content $startupPath -Raw

if ($startup -notmatch "MapOperationalQueuePressureControlTowerApi\(") {
    if ($startup -match "api\.MapOperationalQueuePressureRiskBandingApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureRiskBandingApi\(\);", "api.MapOperationalQueuePressureRiskBandingApi();`r`n        api.MapOperationalQueuePressureControlTowerApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureDecisionMatrixApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureDecisionMatrixApi\(\);", "api.MapOperationalQueuePressureDecisionMatrixApi();`r`n        api.MapOperationalQueuePressureControlTowerApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureOperatorAdvisoryApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureOperatorAdvisoryApi\(\);", "api.MapOperationalQueuePressureOperatorAdvisoryApi();`r`n        api.MapOperationalQueuePressureControlTowerApi();"
    }
    else {
        throw "Could not locate operational endpoint insertion point. Expected a queue-pressure endpoint map call."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped operational queue pressure control tower endpoint."
}
else {
    Write-Host "Operational queue pressure control tower endpoint already mapped."
}
