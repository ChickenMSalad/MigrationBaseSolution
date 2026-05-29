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
    'docs\p7\P7.10A-Retire-Csv-Smoke-Path.md',
    'config-samples\runtime-smoke-deprecated-allowlist.sample.json',
    'tools\runtime\Test-RuntimeSmokePathReferences.ps1'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = [System.IO.Path]::Combine($repoRoot, $relativePath)
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw ('Required P7.10A file is missing: {0}' -f $relativePath)
    }
}

$scriptsToParse = @(
    'tools\runtime\Test-RuntimeSmokePathReferences.ps1'
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

$qualityScript = [System.IO.Path]::Combine($repoRoot, 'tools\runtime\Test-RuntimePowerShellScriptQuality.ps1')
if (Test-Path -LiteralPath $qualityScript) {
    & $qualityScript -Path @([System.IO.Path]::Combine($repoRoot, 'tools\runtime\Test-RuntimeSmokePathReferences.ps1'))
}

$allowlistPath = [System.IO.Path]::Combine($repoRoot, 'config-samples\runtime-smoke-deprecated-allowlist.sample.json')
$allowlist = Get-Content -LiteralPath $allowlistPath -Raw | ConvertFrom-Json
foreach ($propertyName in @('activeSmokeIncludePathFragments', 'excludedPathFragments', 'deprecatedActiveSmokeTerms')) {
    if ($null -eq $allowlist.PSObject.Properties[$propertyName]) {
        throw ('Allowlist sample is missing property: {0}' -f $propertyName)
    }
}

Write-Host 'P7.10A retire Csv smoke path validation passed.'
