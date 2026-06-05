Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    if ($PSScriptRoot) {
        return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
    }
    return (Get-Location).Path
}

function Assert-PathExists {
    param([string]$RootPath, [string]$RelativePath)
    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required path missing: $RelativePath"
    }
}

$root = Get-RepositoryRoot
Write-Host "Repository root: $root"

Assert-PathExists -RootPath $root -RelativePath 'database\sql\p7\005_sql_operational_schema_compatibility_repair.sql'
Assert-PathExists -RootPath $root -RelativePath 'database\sql\p7\001_operational_runtime_store.sql'
Assert-PathExists -RootPath $root -RelativePath 'database\sql\p7\002_operational_queue_procedures.sql'
Assert-PathExists -RootPath $root -RelativePath 'database\sql\p7\003_operational_queue_runtime_wiring.sql'
Assert-PathExists -RootPath $root -RelativePath 'database\sql\p7\004_smoke_seed_operational_run.sql'
Assert-PathExists -RootPath $root -RelativePath 'database\sql\p7\004_sql_operational_smoke_diagnostics.sql'

$scriptPath = Join-Path $root 'database\sql\p7\005_sql_operational_schema_compatibility_repair.sql'
$content = Get-Content -LiteralPath $scriptPath -Raw

foreach ($required in @('SET QUOTED_IDENTIFIER ON', 'SET ANSI_NULLS ON', 'MigrationProjects', 'MigrationManifestRows', 'MigrationRunCheckpoints', 'MigrationAssetMappings')) {
    if (-not $content.Contains($required)) {
        throw "Schema compatibility repair script does not contain required text: $required"
    }
}

Write-Host 'P7.7F SQL operational schema compatibility validation passed.'
