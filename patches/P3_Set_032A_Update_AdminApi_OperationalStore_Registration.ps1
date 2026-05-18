$repoRoot = (Resolve-Path ".").Path
$programPath = Join-Path $repoRoot "src\Migration.Admin.Api\Program.cs"

if (-not (Test-Path $programPath)) {
    throw "Could not find $programPath"
}

$content = Get-Content $programPath -Raw

$content = $content -replace "builder\.Services\.AddOperationalStore\(\);", "builder.Services.AddOperationalStore(builder.Configuration);"

Set-Content -Path $programPath -Value $content -NoNewline

Write-Host "Updated Admin API operational store registration to pass builder.Configuration."
