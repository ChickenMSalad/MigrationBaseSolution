[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$toolRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($toolRoot)) {
    if (-not [string]::IsNullOrWhiteSpace($PSCommandPath)) {
        $toolRoot = Split-Path -Parent $PSCommandPath
    }
}
if ([string]::IsNullOrWhiteSpace($toolRoot)) {
    throw 'Unable to resolve validator root.'
}

$repoRoot = Split-Path -Parent $toolRoot

$requiredFiles = @(
    'docs\p10\P10.0B-Real-Migration-Job-Manifest-Mapping-Wiring.md',
    'config-samples\p10-localstorage-real-migration.sample.json',
    'profiles\jobs\p10-localstorage-real-migration.job.json',
    'profiles\mappings\p10-localstorage-real-migration.mapping.json',
    'database\sql\p10\001_seed_p10_localstorage_real_migration_work_item.sql',
    'tools\runtime\Test-P100RealMigrationArtifacts.ps1'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = [System.IO.Path]::Combine($repoRoot, $relativePath)
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw ('Required P10.0B file is missing: {0}' -f $relativePath)
    }
}

$scriptsToParse = @(
    'tools\runtime\Test-P100RealMigrationArtifacts.ps1'
)

foreach ($relativeScript in $scriptsToParse) {
    $scriptPath = [System.IO.Path]::Combine($repoRoot, $relativeScript)
    $parseErrors = $null
    [System.Management.Automation.PSParser]::Tokenize((Get-Content -LiteralPath $scriptPath -Raw), [ref]$parseErrors) | Out-Null
    if ($null -ne $parseErrors -and @($parseErrors).Count -gt 0) {
        $message = (@($parseErrors) | ForEach-Object { $_.Message }) -join '; '
        throw ('PowerShell parser errors in {0}: {1}' -f $relativeScript, $message)
    }
}

& ([System.IO.Path]::Combine($repoRoot, 'tools\runtime\Test-P100RealMigrationArtifacts.ps1')) -RepoRoot $repoRoot

$sqlPath = [System.IO.Path]::Combine($repoRoot, 'database\sql\p10\001_seed_p10_localstorage_real_migration_work_item.sql')
$sqlText = Get-Content -LiteralPath $sqlPath -Raw
foreach ($term in @('migration.Runs', 'migration.WorkItems', 'MigrationJobDefinition', 'P10LocalStorageRealMigration')) {
    if ($sqlText.IndexOf($term, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw ('Seed SQL is missing expected semantic term: {0}' -f $term)
    }
}

Write-Host 'P10.0B real migration artifact wiring validation passed.'
