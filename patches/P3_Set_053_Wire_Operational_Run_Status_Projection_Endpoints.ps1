$repoRoot = (Resolve-Path ".").Path
$startupPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"

if (-not (Test-Path $startupPath)) {
    throw "Could not find $startupPath"
}

$content = Get-Content $startupPath -Raw

if ($content -notmatch "MapOperationalRunStatusProjectionEndpoints\(") {
    if ($content -match "api\.MapOperationalMirrorReadEndpoints\(\);") {
        $content = $content -replace "api\.MapOperationalMirrorReadEndpoints\(\);", "api.MapOperationalMirrorReadEndpoints();`r`n        api.MapOperationalRunStatusProjectionEndpoints();"
    }
    elseif ($content -match "api\.MapOperationalDispatchEndpoints\(\);") {
        $content = $content -replace "api\.MapOperationalDispatchEndpoints\(\);", "api.MapOperationalDispatchEndpoints();`r`n        api.MapOperationalRunStatusProjectionEndpoints();"
    }
    else {
        throw "Could not locate operational endpoint mapping section in AdminApiEndpointStartupExtensions.cs"
    }

    Set-Content -Path $startupPath -Value $content -NoNewline
    Write-Host "Mapped operational run status projection endpoints."
}
else {
    Write-Host "Operational run status projection endpoints already mapped."
}
