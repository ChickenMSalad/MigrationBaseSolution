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
    'docs\p7\P7.10E-Stale-Runtime-Artifact-Quarantine.md',
    'config-samples\runtime-stale-artifact-quarantine.sample.json',
    'tools\runtime\New-RuntimeStaleArtifactInventory.ps1',
    'tools\runtime\New-RuntimeStaleArtifactQuarantineReport.ps1'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw ('Required P7.10E file is missing: {0}' -f $relativePath)
    }
}

$scriptsToParse = @(
    'tools\runtime\New-RuntimeStaleArtifactInventory.ps1',
    'tools\runtime\New-RuntimeStaleArtifactQuarantineReport.ps1'
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

$qualityScript = Join-Path $repoRoot 'tools\runtime\Test-RuntimePowerShellScriptQuality.ps1'
if (Test-Path -LiteralPath $qualityScript) {
    $paths = @()
    foreach ($relativeScript in $scriptsToParse) {
        $paths += (Join-Path $repoRoot $relativeScript)
    }

    & $qualityScript -Path $paths
}

$configPath = Join-Path $repoRoot 'config-samples\runtime-stale-artifact-quarantine.sample.json'
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
foreach ($propertyName in @('includePathFragments', 'staleReferenceTerms', 'excludePathFragments')) {
    if ($null -eq $config.PSObject.Properties[$propertyName]) {
        throw ('Sample quarantine configuration is missing property: {0}' -f $propertyName)
    }
}

Write-Host 'P7.10E stale runtime artifact quarantine validation passed.'
