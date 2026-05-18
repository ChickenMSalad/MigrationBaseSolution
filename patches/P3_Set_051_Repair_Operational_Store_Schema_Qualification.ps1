$repoRoot = (Resolve-Path ".").Path
$sqlStoreRoot = Join-Path $repoRoot "src\Migration.Infrastructure\State\OperationalStore\Sql"

if (-not (Test-Path $sqlStoreRoot)) {
    throw "Could not find $sqlStoreRoot"
}

$tableNames = @(
    "MigrationRuns",
    "MigrationManifestRecords",
    "MigrationWorkItems",
    "MigrationFailures",
    "MigrationCheckpoints",
    "MigrationIdentifierMaps"
)

$files = Get-ChildItem -Path $sqlStoreRoot -Recurse -Filter "*.cs"

$changedFiles = @()

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $updated = $content

    foreach ($tableName in $tableNames) {
        $updated = $updated.Replace("dbo.$tableName", "migration.$tableName")
        $updated = $updated.Replace("[dbo].[$tableName]", "[migration].[$tableName]")
        $updated = $updated.Replace("[dbo].$tableName", "[migration].$tableName")
        $updated = $updated.Replace("dbo.[$tableName]", "migration.[$tableName]")
    }

    if ($updated -ne $content) {
        Set-Content -Path $file.FullName -Value $updated -NoNewline
        $changedFiles += $file.FullName
    }
}

if ($changedFiles.Count -eq 0) {
    Write-Host "No dbo operational table references were found under $sqlStoreRoot."
}
else {
    Write-Host "Updated operational SQL schema references in:"
    $changedFiles | ForEach-Object { Write-Host " - $_" }
}
