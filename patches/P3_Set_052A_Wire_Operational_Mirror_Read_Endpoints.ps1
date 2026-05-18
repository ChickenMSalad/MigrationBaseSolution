$repoRoot = (Resolve-Path ".").Path
$startupPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"
$programPath = Join-Path $repoRoot "src\Migration.Admin.Api\Program.cs"

if (-not (Test-Path $startupPath)) {
    throw "Could not find $startupPath"
}

$startup = Get-Content $startupPath -Raw

if ($startup -notmatch "MapOperationalMirrorReadEndpoints\(") {
    $startup = $startup -replace "api\.MapOperationalDispatchEndpoints\(\);", "api.MapOperationalDispatchEndpoints();`r`n        api.MapOperationalMirrorReadEndpoints();"
    Set-Content -Path $startupPath -Value $startup -NoNewline
    Write-Host "Mapped operational mirror read endpoints in AdminApiEndpointStartupExtensions."
}
else {
    Write-Host "Operational mirror read endpoints already mapped in AdminApiEndpointStartupExtensions."
}

if (Test-Path $programPath) {
    $program = Get-Content $programPath -Raw
    if ($program -match "app\.MapOperationalMirrorReadEndpoints\(\);") {
        $program = $program -replace "\r?\napp\.MapOperationalMirrorReadEndpoints\(\);", ""
        Set-Content -Path $programPath -Value $program -NoNewline
        Write-Host "Removed direct Program.cs operational mirror read mapping."
    }
}
