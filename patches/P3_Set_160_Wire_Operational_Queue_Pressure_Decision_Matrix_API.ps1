$repoRoot = (Resolve-Path ".").Path
$startupPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"

if (-not (Test-Path $startupPath)) {
    throw "Could not find $startupPath"
}

$startup = Get-Content $startupPath -Raw

if ($startup -notmatch "MapOperationalQueuePressureDecisionMatrixApi\(") {
    if ($startup -match "api\.MapOperationalQueuePressureOperatorAdvisoryApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureOperatorAdvisoryApi\(\);", "api.MapOperationalQueuePressureOperatorAdvisoryApi();`r`n        api.MapOperationalQueuePressureDecisionMatrixApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureRecoveryReadinessApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureRecoveryReadinessApi\(\);", "api.MapOperationalQueuePressureRecoveryReadinessApi();`r`n        api.MapOperationalQueuePressureDecisionMatrixApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureExecutionReadinessApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureExecutionReadinessApi\(\);", "api.MapOperationalQueuePressureExecutionReadinessApi();`r`n        api.MapOperationalQueuePressureDecisionMatrixApi();"
    }
    else {
        throw "Could not locate operational endpoint insertion point. Expected a queue-pressure endpoint map call."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped operational queue pressure decision matrix endpoint."
}
else {
    Write-Host "Operational queue pressure decision matrix endpoint already mapped."
}
