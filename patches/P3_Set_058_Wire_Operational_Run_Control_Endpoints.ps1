$repoRoot = (Resolve-Path ".").Path
$startupPath = Join-Path $repoRoot "src\Migration.Admin.Api\Registration\AdminApiEndpointStartupExtensions.cs"

$content = Get-Content $startupPath -Raw

if ($content -notmatch "MapOperationalRunControlEndpoints") {

    $content = $content -replace `
        "api\.MapOperationalMetricsEndpoints\(\);",
        "api.MapOperationalMetricsEndpoints();`r`n        api.MapOperationalRunControlEndpoints();"

    Set-Content -Path $startupPath -Value $content -NoNewline

    Write-Host "Operational run control endpoints mapped."
}
