$repoRoot = (Resolve-Path ".").Path
$startupPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"

if (-not (Test-Path $startupPath)) {
    throw "Could not find $startupPath"
}

$startup = Get-Content $startupPath -Raw

if ($startup -notmatch "MapOperationalQueuePressureCommandCenterApi\(") {
    if ($startup -match "api\.MapOperationalQueuePressureControlTowerApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureControlTowerApi\(\);", "api.MapOperationalQueuePressureControlTowerApi();`r`n        api.MapOperationalQueuePressureCommandCenterApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureRiskBandingApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureRiskBandingApi\(\);", "api.MapOperationalQueuePressureRiskBandingApi();`r`n        api.MapOperationalQueuePressureCommandCenterApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureDecisionMatrixApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureDecisionMatrixApi\(\);", "api.MapOperationalQueuePressureDecisionMatrixApi();`r`n        api.MapOperationalQueuePressureCommandCenterApi();"
    }
    else {
        throw "Could not locate operational endpoint insertion point. Expected a queue-pressure endpoint map call."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped operational queue pressure command center endpoint."
}
else {
    Write-Host "Operational queue pressure command center endpoint already mapped."
}
