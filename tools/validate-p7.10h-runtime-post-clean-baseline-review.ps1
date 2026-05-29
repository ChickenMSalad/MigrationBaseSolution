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
    'docs\p7\P7.10H-Post-Clean-Baseline-Review.md',
    'docs\operations\runtime-post-clean-baseline-review.md',
    'config-samples\runtime-post-clean-baseline-review.sample.json',
    'tools\runtime\New-RuntimePostCleanBaselineReview.ps1'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw ('Required P7.10H file is missing: {0}' -f $relativePath)
    }
}

$scriptsToParse = @(
    'tools\runtime\New-RuntimePostCleanBaselineReview.ps1'
)

foreach ($relativeScript in $scriptsToParse) {
    $scriptPath = Join-Path $repoRoot $relativeScript
    $parseErrors = $null
    [System.Management.Automation.PSParser]::Tokenize((Get-Content -LiteralPath $scriptPath -Raw), [ref]$parseErrors) | Out-Null
    if ($null -ne $parseErrors -and @($parseErrors).Count -gt 0) {
        $message = (@($parseErrors) | ForEach-Object { $_.Message }) -join '; '
        throw ('PowerShell parser errors in {0}: {1}' -f $relativeScript, $message)
    }
}

$configPath = Join-Path $repoRoot 'config-samples\runtime-post-clean-baseline-review.sample.json'
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
foreach ($propertyName in @('baselineName', 'requiredEvidence', 'allowedRemainingMigrationPrefixedKeys', 'requiredRuntimeSettings')) {
    if ($null -eq $config.PSObject.Properties[$propertyName]) {
        throw ('Sample config missing property: {0}' -f $propertyName)
    }
}

$runtimeScript = Get-Content -LiteralPath (Join-Path $repoRoot 'tools\runtime\New-RuntimePostCleanBaselineReview.ps1') -Raw
if ($runtimeScript.IndexOf('.PackageReference', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
    $runtimeScript.IndexOf('.ItemGroup', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
    $runtimeScript.IndexOf('.None', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
    throw 'Runtime script contains XML property text that is not expected in this set.'
}

Write-Host 'P7.10H runtime post-clean baseline review validation passed.'
