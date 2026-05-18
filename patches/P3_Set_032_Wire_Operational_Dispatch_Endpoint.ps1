$repoRoot = (Resolve-Path ".").Path
$programPath = Join-Path $repoRoot "src\Migration.Admin.Api\Program.cs"

if (-not (Test-Path $programPath)) {
    throw "Could not find $programPath"
}

$content = Get-Content $programPath -Raw

if ($content -notmatch "using Migration\.Infrastructure\.DependencyInjection;") {
    $content = $content -replace "using Migration\.Infrastructure\.Taxonomy;", "using Migration.Infrastructure.Taxonomy; using Migration.Infrastructure.DependencyInjection;"
}

if ($content -notmatch "AddOperationalStore\(") {
    $content = $content -replace "builder\.Services\.AddMigrationAdminApiRuntime\(builder\.Configuration\);", "builder.Services.AddMigrationAdminApiRuntime(builder.Configuration); builder.Services.AddOperationalStore();"
}

if ($content -notmatch "MapOperationalDispatchEndpoints\(") {
    $content = $content -replace "api\.MapPreflightEndpoints\(\);", "api.MapPreflightEndpoints(); api.MapOperationalDispatchEndpoints();"
}

Set-Content -Path $programPath -Value $content -NoNewline

Write-Host "Operational dispatch endpoint wiring applied to $programPath"
