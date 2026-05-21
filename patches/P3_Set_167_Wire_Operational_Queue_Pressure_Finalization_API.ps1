$repoRoot = (Resolve-Path ".").Path
$startupPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"

if (-not (Test-Path $startupPath)) {
    throw "Could not find $startupPath"
}

$startup = Get-Content $startupPath -Raw

if ($startup -notmatch "MapOperationalQueuePressureFinalizationApi\(") {
    if ($startup -match "api\.MapOperationalQueuePressureReadoutApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureReadoutApi\(\);", "api.MapOperationalQueuePressureReadoutApi();`r`n        api.MapOperationalQueuePressureFinalizationApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureOperationalPostureApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureOperationalPostureApi\(\);", "api.MapOperationalQueuePressureOperationalPostureApi();`r`n        api.MapOperationalQueuePressureFinalizationApi();"
    }
    elseif ($startup -match "api\.MapOperationalQueuePressureExecutiveSummaryApi\(\);") {
        $startup = $startup -replace "api\.MapOperationalQueuePressureExecutiveSummaryApi\(\);", "api.MapOperationalQueuePressureExecutiveSummaryApi();`r`n        api.MapOperationalQueuePressureFinalizationApi();"
    }
    else {
        throw "Could not locate operational endpoint insertion point. Expected a queue-pressure endpoint map call."
    }

    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped operational queue pressure finalization endpoint."
}
else {
    Write-Host "Operational queue pressure finalization endpoint already mapped."
}
