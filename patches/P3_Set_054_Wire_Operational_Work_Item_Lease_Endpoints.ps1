$repoRoot = (Resolve-Path ".").Path
$startupPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"

if (-not (Test-Path $startupPath)) {
    throw "Could not find $startupPath"
}

$content = Get-Content $startupPath -Raw

if ($content -notmatch "MapOperationalWorkItemLeaseEndpoints\(") {
    if ($content -match "api\.MapOperationalRunStatusProjectionEndpoints\(\);") {
        $content = $content -replace "api\.MapOperationalRunStatusProjectionEndpoints\(\);", "api.MapOperationalRunStatusProjectionEndpoints();`r`n        api.MapOperationalWorkItemLeaseEndpoints();"
    }
    elseif ($content -match "api\.MapOperationalMirrorReadEndpoints\(\);") {
        $content = $content -replace "api\.MapOperationalMirrorReadEndpoints\(\);", "api.MapOperationalMirrorReadEndpoints();`r`n        api.MapOperationalWorkItemLeaseEndpoints();"
    }
    else {
        throw "Could not locate operational endpoint mapping section in AdminApiEndpointStartupExtensions.cs"
    }

    Set-Content -Path $startupPath -Value $content -NoNewline
    Write-Host "Mapped operational work-item lease endpoints."
}
else {
    Write-Host "Operational work-item lease endpoints already mapped."
}
