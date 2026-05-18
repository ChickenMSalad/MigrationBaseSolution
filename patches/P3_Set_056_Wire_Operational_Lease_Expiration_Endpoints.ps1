$repoRoot = (Resolve-Path ".").Path
$startupPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"

if (-not (Test-Path $startupPath)) {
    throw "Could not find $startupPath"
}

$content = Get-Content $startupPath -Raw

if ($content -notmatch "MapOperationalWorkItemLeaseExpirationEndpoints\(") {
    if ($content -match "api\.MapOperationalWorkItemLeaseEndpoints\(\);") {
        $content = $content -replace "api\.MapOperationalWorkItemLeaseEndpoints\(\);", "api.MapOperationalWorkItemLeaseEndpoints();`r`n        api.MapOperationalWorkItemLeaseExpirationEndpoints();"
    }
    else {
        throw "Could not locate operational work item lease endpoint mapping in AdminApiEndpointStartupExtensions.cs"
    }

    Set-Content -Path $startupPath -Value $content -NoNewline
    Write-Host "Mapped operational lease expiration endpoints."
}
else {
    Write-Host "Operational lease expiration endpoints already mapped."
}
