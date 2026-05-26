[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Resolve-RepoRoot {
    if (-not [string]::IsNullOrWhiteSpace($PSScriptRoot)) {
        $candidate = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..") -ErrorAction SilentlyContinue
        if ($null -ne $candidate) {
            return $candidate.ProviderPath
        }
    }

    return (Get-Location).Path
}

function Assert-FileExists {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Missing required file: $Path"
    }
}

function Assert-FileContains {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Pattern,
        [Parameter(Mandatory = $true)][string]$Description
    )

    $match = Select-String -LiteralPath $Path -Pattern $Pattern -SimpleMatch -Quiet
    if (-not $match) {
        throw "Validation failed for $Path. Missing: $Description"
    }
}

$repoRoot = Resolve-RepoRoot

$requiredFiles = @(
    "docs\p7\P7.8B-SQL-Baseline-Consolidation.md",
    "database\sql\p7\P7.8B_RUNTIME_SQL_DEPLOYMENT_ORDER.md",
    "database\sql\p7\009_runtime_sql_contract_validator.sql",
    "tools\runtime\Export-RuntimeSqlSchema.ps1",
    "tools\runtime\Compare-RuntimeSqlSchema.ps1",
    "tools\runtime\Invoke-RuntimeSqlContractValidator.ps1"
)

foreach ($relativePath in $requiredFiles) {
    Assert-FileExists -Path (Join-Path $repoRoot $relativePath)
}

$validator = Join-Path $repoRoot "database\sql\p7\009_runtime_sql_contract_validator.sql"
Assert-FileContains -Path $validator -Pattern "migration.WorkItems" -Description "canonical WorkItems validation"
Assert-FileContains -Path $validator -Pattern "WorkItemId', N'bigint" -Description "WorkItemId bigint validation"
Assert-FileContains -Path $validator -Pattern "ManifestRowId', N'bigint" -Description "ManifestRowId bigint validation"
Assert-FileContains -Path $validator -Pattern "IX_WorkItems_ClaimQueue" -Description "claim queue index validation"
Assert-FileContains -Path $validator -Pattern "THROW 51780" -Description "validator failure throw"

$order = Join-Path $repoRoot "database\sql\p7\P7.8B_RUNTIME_SQL_DEPLOYMENT_ORDER.md"
Assert-FileContains -Path $order -Pattern "006_sql_operational_runtime_bootstrap_compatibility.sql" -Description "bootstrap deployment step"
Assert-FileContains -Path $order -Pattern "007_sql_operational_execution_history.sql" -Description "execution history deployment step"
Assert-FileContains -Path $order -Pattern "009_runtime_sql_contract_validator.sql" -Description "validator deployment step"

Write-Host "P7.8B SQL baseline consolidation validation passed."
