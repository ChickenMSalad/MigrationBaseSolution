$repoRoot = (Resolve-Path ".").Path
$endpointRoot = Join-Path $repoRoot "src\Migration.Admin.Api"

if (-not (Test-Path $endpointRoot)) {
    throw "Could not find $endpointRoot"
}

Write-Host "Searching for existing run endpoint candidates..."

Get-ChildItem -Path $endpointRoot -Recurse -Filter "*.cs" |
    Where-Object {
        $content = Get-Content $_.FullName -Raw
        $content -match "IMigrationRunQueue" -or
        $content -match "EnqueueAsync\(run" -or
        $content -match "projects/\{projectId\}/runs" -or
        $content -match "MapPost" -and $content -match "runs"
    } |
    Select-Object FullName |
    Format-Table -AutoSize
