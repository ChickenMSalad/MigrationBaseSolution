$repoRoot = (Resolve-Path ".").Path
$startupPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"

if (-not (Test-Path $startupPath)) {
    throw "Could not find $startupPath"
}

$startup = Get-Content $startupPath -Raw

if ($startup -notmatch "MapOperationalQueuePressureRiskBandingApi\(") {
    if ($startup -match "api\.MapOperationalQueuePressureDecisionMatrixApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureDecisionMatrixApi\(\);", "api.MapOperationalQueuePressureDecisionMatrixApi();`r`n        api.MapOperationalQueuePressureRiskBandingApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureOperatorAdvisoryApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureOperatorAdvisoryApi\(\);", "api.MapOperationalQueuePressureOperatorAdvisoryApi();`r`n        api.MapOperationalQueuePressureRiskBandingApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureRecoveryReadinessApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureRecoveryReadinessApi\(\);", "api.MapOperationalQueuePressureRecoveryReadinessApi();`r`n        api.MapOperationalQueuePressureRiskBandingApi();"
    }
    else {
        throw "Could not locate operational endpoint insertion point. Expected a queue-pressure endpoint map call."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped operational queue pressure risk banding endpoint."
}
else {
    Write-Host "Operational queue pressure risk banding endpoint already mapped."
}
