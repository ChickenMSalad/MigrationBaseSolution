$repoRoot = (Resolve-Path ".").Path
$programPath = Join-Path $repoRoot "src\Migration.Admin.Api\Program.cs"

if (-not (Test-Path $programPath)) {
    throw "Could not find $programPath"
}

$content = Get-Content $programPath -Raw

if ($content -notmatch "MapOperationalMirrorDiagnosticsEndpoints\(") {
    $content = $content -replace "app\.MapAdminEndpointDiagnostics\(\);", "app.MapAdminEndpointDiagnostics();`r`napp.MapOperationalMirrorDiagnosticsEndpoints();"
}

Set-Content -Path $programPath -Value $content -NoNewline

Write-Host "Operational mirror diagnostics endpoint mapping applied to $programPath"
