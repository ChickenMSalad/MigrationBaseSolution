$repoRoot = (Resolve-Path ".").Path
$startupPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"

if (-not (Test-Path $startupPath)) {
    throw "Could not find $startupPath"
}

$content = Get-Content $startupPath -Raw

if ($content -notmatch "MapOperationalMetricsEndpoints\(") {
    if ($content -match "api\.MapOperationalWorkItemLeaseExpirationEndpoints\(\);") {
        $content = $content -replace "api\.MapOperationalWorkItemLeaseExpirationEndpoints\(\);", "api.MapOperationalWorkItemLeaseExpirationEndpoints();`r`n        api.MapOperationalMetricsEndpoints();"
    }
    elseif ($content -match "api\.MapOperationalWorkItemLeaseEndpoints\(\);") {
        $content = $content -replace "api\.MapOperationalWorkItemLeaseEndpoints\(\);", "api.MapOperationalWorkItemLeaseEndpoints();`r`n        api.MapOperationalMetricsEndpoints();"
    }
    else {
        throw "Could not locate operational endpoint mapping section in AdminApiEndpointStartupExtensions.cs"
    }

    Set-Content -Path $startupPath -Value $content -NoNewline
    Write-Host "Mapped operational metrics endpoints."
}
else {
    Write-Host "Operational metrics endpoints already mapped."
}
