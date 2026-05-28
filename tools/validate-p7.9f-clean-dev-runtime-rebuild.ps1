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
    'docs\p7\P7.9F-Clean-Dev-Runtime-Rebuild-Execution.md',
    'config-samples\clean-dev-runtime-rebuild.sample.json',
    'database\sql\p7\018_clean_dev_runtime_post_rebuild_validator.sql',
    'tools\runtime\New-CleanDevRuntimeRebuildPlan.ps1',
    'tools\runtime\New-CleanDevRuntimeEvidenceBundle.ps1'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw ('Required P7.9F file is missing: {0}' -f $relativePath)
    }
}

$scripts = @(
    'tools\runtime\New-CleanDevRuntimeRebuildPlan.ps1',
    'tools\runtime\New-CleanDevRuntimeEvidenceBundle.ps1'
)

foreach ($relativeScript in $scripts) {
    $scriptPath = Join-Path $repoRoot $relativeScript
    $tokens = $null
    $parseErrors = $null
    [System.Management.Automation.PSParser]::Tokenize((Get-Content -LiteralPath $scriptPath -Raw), [ref]$parseErrors) | Out-Null
    if ($null -ne $parseErrors -and @($parseErrors).Count -gt 0) {
        $message = (@($parseErrors) | ForEach-Object { $_.Message }) -join '; '
        throw ('PowerShell parser errors in {0}: {1}' -f $relativeScript, $message)
    }
$fragileInvocationPattern = '$' + 'MyInvocation' + '.ScriptName'
    $scriptText = Get-Content -LiteralPath $scriptPath -Raw
    if ($scriptText.IndexOf($fragileInvocationPattern, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw ('Avoid fragile invocation-root usage in {0}' -f $relativeScript)
    }
    if ($scriptText -match '\$[A-Za-z_][A-Za-z0-9_]*:' -and $scriptText -notmatch '\$(script|global|local|private|using|env):') {
        throw ('Potential fragile colon interpolation in {0}' -f $relativeScript)
    }
    if ($scriptText.IndexOf('.PackageReference', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $scriptText.IndexOf('.None', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $scriptText.IndexOf('.ItemGroup', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw ('Potential StrictMode-unsafe XML property access in {0}' -f $relativeScript)
    }
}

$sqlText = Get-Content -LiteralPath (Join-Path $repoRoot 'database\sql\p7\018_clean_dev_runtime_post_rebuild_validator.sql') -Raw
foreach ($term in @('migration.WorkItems', 'migration.ManifestRows', 'migration.Runs', 'sys.foreign_keys')) {
    if ($sqlText.IndexOf($term, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw ('Post-rebuild SQL validator is missing expected semantic term: {0}' -f $term)
    }
}

$configPath = Join-Path $repoRoot 'config-samples\clean-dev-runtime-rebuild.sample.json'
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
foreach ($propertyName in @('resourceGroup', 'dispatcherApp', 'executorApp', 'sqlServer', 'database', 'canonicalSqlScripts')) {
    if ($null -eq $config.PSObject.Properties[$propertyName]) {
        throw ('Sample rebuild configuration is missing property: {0}' -f $propertyName)
    }
}

Write-Host 'P7.9F clean dev runtime rebuild validation passed.'
