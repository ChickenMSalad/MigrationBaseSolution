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
    'docs\p7\P7.9E-Dev-Cloud-Reset-Readiness.md',
    'database\sql\p7\016_runtime_dev_reset_readiness.sql',
    'database\sql\p7\017_runtime_dev_reset_cleanup_template.sql',
    'tools\runtime\New-RuntimeDevCloudResetPlan.ps1',
    'tools\runtime\Invoke-RuntimeDevResetReadiness.ps1'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw ('Required P7.9E file is missing: {0}' -f $relativePath)
    }
}

$readinessSqlPath = Join-Path $repoRoot 'database\sql\p7\016_runtime_dev_reset_readiness.sql'
$readinessSql = Get-Content -LiteralPath $readinessSqlPath -Raw

foreach ($requiredTerm in @('sys.tables', 'sys.schemas', 'sys.foreign_keys', 'WorkItemStateSummary', 'RecentWorkItems')) {
    if ($readinessSql.IndexOf($requiredTerm, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw ('Readiness SQL is missing expected diagnostic term: {0}' -f $requiredTerm)
    }
}

foreach ($forbiddenTerm in @('DROP TABLE', 'TRUNCATE TABLE', 'DELETE FROM', 'ALTER TABLE', 'INSERT INTO', 'UPDATE ')) {
    if ($readinessSql.IndexOf($forbiddenTerm, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw ('Readiness SQL must be read-only but contains: {0}' -f $forbiddenTerm)
    }
}

$cleanupSqlPath = Join-Path $repoRoot 'database\sql\p7\017_runtime_dev_reset_cleanup_template.sql'
$cleanupSql = Get-Content -LiteralPath $cleanupSqlPath -Raw

foreach ($requiredTerm in @('@AllowDestructiveReset bit = 0', 'THROW 51090', 'BEGIN TRANSACTION', 'COMMIT TRANSACTION')) {
    if ($cleanupSql.IndexOf($requiredTerm, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw ('Guarded cleanup SQL is missing safety term: {0}' -f $requiredTerm)
    }
}

$runtimeScripts = @(
    'tools\runtime\New-RuntimeDevCloudResetPlan.ps1',
    'tools\runtime\Invoke-RuntimeDevResetReadiness.ps1'
)

foreach ($relativeScript in $runtimeScripts) {
    $scriptPath = Join-Path $repoRoot $relativeScript
    $tokens = $null
    $parseErrors = $null
    [System.Management.Automation.Language.Parser]::ParseFile($scriptPath, [ref] $tokens, [ref] $parseErrors) | Out-Null
    if ($null -ne $parseErrors -and @($parseErrors).Count -gt 0) {
        $message = (@($parseErrors) | ForEach-Object { $_.Message }) -join '; '
        throw ('PowerShell parser errors in {0}: {1}' -f $relativeScript, $message)
    }

    $scriptText = Get-Content -LiteralPath $scriptPath -Raw
    if ($scriptText.IndexOf('MyInvocation', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw ('Runtime script contains fragile invocation text: {0}' -f $relativeScript)
    }
    if ($scriptText -match '\$[A-Za-z_][A-Za-z0-9_]*:') {
        $badMatches = @([regex]::Matches($scriptText, '\$[A-Za-z_][A-Za-z0-9_]*:')) |
            Where-Object { $_.Value -notmatch '^\$(script|global|local|private|using|env):$' }
        if (@($badMatches).Count -gt 0) {
            throw ('Runtime script contains fragile colon interpolation: {0}' -f $relativeScript)
        }
    }
}

Write-Host 'P7.9E dev cloud reset readiness validation passed.'
