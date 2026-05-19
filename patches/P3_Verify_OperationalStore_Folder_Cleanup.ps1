$repoRoot = (Resolve-Path ".").Path
$operationalStoreRoot = Join-Path $repoRoot "src\Migration.Admin.Api\OperationalStore"

if (-not (Test-Path $operationalStoreRoot)) {
    throw "Could not find $operationalStoreRoot"
}

Write-Host "Checking OperationalStore cleanup..."
Write-Host ""

$topLevelCsFiles = Get-ChildItem -Path $operationalStoreRoot -File -Filter "*.cs" |
    Sort-Object Name

$topLevelSqlFiles = Get-ChildItem -Path $operationalStoreRoot -File -Filter "*.sql" |
    Sort-Object Name

if ($topLevelCsFiles.Count -gt 0) {
    Write-Host "Top-level .cs files still present under OperationalStore:"
    $topLevelCsFiles | ForEach-Object {
        Write-Host " - $($_.FullName)"
    }
}
else {
    Write-Host "No top-level .cs files under OperationalStore."
}

Write-Host ""

if ($topLevelSqlFiles.Count -gt 0) {
    Write-Host "Top-level .sql files still present under OperationalStore:"
    $topLevelSqlFiles | ForEach-Object {
        Write-Host " - $($_.FullName)"
    }

    throw "OperationalStore cleanup failed: loose SQL files remain."
}
else {
    Write-Host "No top-level .sql files under OperationalStore."
}

Write-Host ""
Write-Host "OperationalStore folder summary:"
Get-ChildItem -Path $operationalStoreRoot -Directory |
    Sort-Object Name |
    ForEach-Object {
        $count = (Get-ChildItem -Path $_.FullName -Recurse -File -Filter "*.cs" | Measure-Object).Count
        Write-Host " - $($_.Name): $count C# file(s)"
    }

Write-Host ""
Write-Host "SQL scripts:"
$sqlScriptRoot = Join-Path $operationalStoreRoot "Sql\Scripts"

if (Test-Path $sqlScriptRoot) {
    Get-ChildItem -Path $sqlScriptRoot -File -Filter "*.sql" |
        Sort-Object Name |
        ForEach-Object {
            Write-Host " - $($_.Name)"
        }
}
else {
    Write-Host " - Sql\Scripts folder does not exist."
}
