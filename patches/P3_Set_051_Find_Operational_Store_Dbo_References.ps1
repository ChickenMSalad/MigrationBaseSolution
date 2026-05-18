$repoRoot = (Resolve-Path ".").Path
$sqlStoreRoot = Join-Path $repoRoot "src\Migration.Infrastructure\State\OperationalStore\Sql"

if (-not (Test-Path $sqlStoreRoot)) {
    throw "Could not find $sqlStoreRoot"
}

Write-Host "Searching for remaining dbo operational table references..."

$matches = Select-String `
    -Path (Join-Path $sqlStoreRoot "**\*.cs") `
    -Pattern "dbo\.Migration|\[dbo\]" `
    -AllMatches `
    -ErrorAction SilentlyContinue

if (-not $matches) {
    Write-Host "No remaining dbo operational table references found."
    exit 0
}

$matches | ForEach-Object {
    "{0}:{1}: {2}" -f $_.Path, $_.LineNumber, $_.Line.Trim()
}
