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
    'docs\p7\P7.9D-Runtime-NoOp-Smoke-Harness.md',
    'database\sql\p7\015_runtime_noop_smoke_seed.sql',
    'config-samples\runtime-noop-smoke-job-definition.sample.json',
    'tools\runtime\Invoke-RuntimeNoOpSmoke.ps1',
    'tools\runtime\Test-RuntimeNoOpSmokeState.ps1'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw ('Required P7.9D file is missing: {0}' -f $relativePath)
    }
}

$scripts = @(
    'tools\runtime\Invoke-RuntimeNoOpSmoke.ps1',
    'tools\runtime\Test-RuntimeNoOpSmokeState.ps1',
    'tools\validate-p7.9d-runtime-noop-smoke-harness.ps1'
)

foreach ($relativeScript in $scripts) {
    $scriptPath = Join-Path $repoRoot $relativeScript
    $tokens = $null
    $errors = $null
    [System.Management.Automation.Language.Parser]::ParseFile($scriptPath, [ref] $tokens, [ref] $errors) | Out-Null
    if ($null -ne $errors -and @($errors).Count -gt 0) {
        $message = (@($errors) | ForEach-Object { $_.Message }) -join '; '
        throw ('PowerShell parser errors in {0}: {1}' -f $relativeScript, $message)
    }

    $text = Get-Content -LiteralPath $scriptPath -Raw
    $forbiddenInvocationPattern = '$' + 'MyInvocation' + '.ScriptName'
    if ($text.IndexOf($forbiddenInvocationPattern, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw ('Script uses forbidden invocation-root pattern: {0}' -f $relativeScript)
    }
    $forbiddenPackageReferenceText = 'PackageReference' + ' Version='
    if ($text.IndexOf($forbiddenPackageReferenceText, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw ('Script contains inline package version text: {0}' -f $relativeScript)
    }
    if ($text -match '\$[A-Za-z_][A-Za-z0-9_]*:' -and $text -notmatch '\$(script|global|local|private|using|env):') {
        throw ('Script contains potential fragile colon interpolation: {0}' -f $relativeScript)
    }
}

$sqlPath = Join-Path $repoRoot 'database\sql\p7\015_runtime_noop_smoke_seed.sql'
$sqlText = Get-Content -LiteralPath $sqlPath -Raw
foreach ($term in @('RuntimeSmoke', 'MigrationJobDefinition', 'migration.WorkItems', 'migration.Runs', 'migration.MigrationRuns')) {
    if ($sqlText.IndexOf($term, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw ('Smoke seed SQL is missing required term: {0}' -f $term)
    }
}

$configPath = Join-Path $repoRoot 'config-samples\runtime-noop-smoke-job-definition.sample.json'
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
if ($config.manifestType -ne 'RuntimeSmoke') {
    throw 'NoOp smoke sample must use manifestType RuntimeSmoke.'
}
if ($config.sourceType -ne 'RuntimeSmoke') {
    throw 'NoOp smoke sample must use sourceType RuntimeSmoke.'
}
if ($config.targetType -ne 'RuntimeSmoke') {
    throw 'NoOp smoke sample must use targetType RuntimeSmoke.'
}
if ($config.mappingProfilePath -ne 'runtime-smoke.mapping.json') {
    throw 'NoOp smoke sample must use runtime-smoke.mapping.json.'
}

Write-Host 'P7.9D runtime NoOp smoke harness validation passed.'
