$repoRoot = (Resolve-Path ".").Path
$startupPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"

if (-not (Test-Path $startupPath)) {
    throw "Could not find $startupPath"
}

$startup = Get-Content $startupPath -Raw

if ($startup -notmatch "MapOperationalQueuePressureExecutiveSummaryApi\(") {
    if ($startup -match "api\.MapOperationalQueuePressureCommandCenterApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureCommandCenterApi\(\);", "api.MapOperationalQueuePressureCommandCenterApi();`r`n        api.MapOperationalQueuePressureExecutiveSummaryApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureControlTowerApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureControlTowerApi\(\);", "api.MapOperationalQueuePressureControlTowerApi();`r`n        api.MapOperationalQueuePressureExecutiveSummaryApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureRiskBandingApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureRiskBandingApi\(\);", "api.MapOperationalQueuePressureRiskBandingApi();`r`n        api.MapOperationalQueuePressureExecutiveSummaryApi();"
    }
    else {
        throw "Could not locate operational endpoint insertion point. Expected a queue-pressure endpoint map call."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped operational queue pressure executive summary endpoint."
}
else {
    Write-Host "Operational queue pressure executive summary endpoint already mapped."
}
