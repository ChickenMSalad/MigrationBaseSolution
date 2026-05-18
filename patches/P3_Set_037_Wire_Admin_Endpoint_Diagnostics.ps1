$repoRoot = (Resolve-Path ".").Path
$programPath = Join-Path $repoRoot "src\Migration.Admin.Api\Program.cs"

if (-not (Test-Path $programPath)) {
    throw "Could not find $programPath"
}

$content = Get-Content $programPath -Raw

if ($content -notmatch "MapAdminEndpointDiagnostics\(") {
    $content = $content -replace "app\.MapOperationalHealthEndpoints\(\);", "app.MapOperationalHealthEndpoints();`r`napp.MapAdminEndpointDiagnostics();"
}

Set-Content -Path $programPath -Value $content -NoNewline

Write-Host "Admin endpoint diagnostics mapping applied to $programPath"
