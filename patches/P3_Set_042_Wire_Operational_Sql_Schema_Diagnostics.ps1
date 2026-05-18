$repoRoot = (Resolve-Path ".").Path
$programPath = Join-Path $repoRoot "src\Migration.Admin.Api\Program.cs"

$content = Get-Content $programPath -Raw

if ($content -notmatch "MapOperationalSqlSchemaDiagnosticsEndpoints\(") {
    $content = $content -replace "app\.MapOperationalMirrorDiagnosticsEndpoints\(\);", "app.MapOperationalMirrorDiagnosticsEndpoints();`r`napp.MapOperationalSqlSchemaDiagnosticsEndpoints();"
}

Set-Content -Path $programPath -Value $content -NoNewline

Write-Host "Operational SQL schema diagnostics mapped."
