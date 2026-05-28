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
    'docs\p7\P7.9G-Runtime-Validator-Hardening.md',
    'config-samples\runtime-script-quality-baseline.sample.json',
    'tools\runtime\Test-RuntimePowerShellScriptQuality.ps1',
    'tools\runtime\New-RuntimeValidatorHardeningReport.ps1'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw ('Required P7.9G file is missing: {0}' -f $relativePath)
    }
}

$scriptsToParse = @(
    'tools\runtime\Test-RuntimePowerShellScriptQuality.ps1',
    'tools\runtime\New-RuntimeValidatorHardeningReport.ps1',
    'tools\validate-p7.9g-runtime-validator-hardening.ps1'
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

# Quality-scan runtime scripts introduced by this set only. Do not scan this validator itself;
# its rule definitions may contain tokens that target scripts must avoid.
$qualityScript = Join-Path $repoRoot 'tools\runtime\Test-RuntimePowerShellScriptQuality.ps1'
$reportScript = Join-Path $repoRoot 'tools\runtime\New-RuntimeValidatorHardeningReport.ps1'
& $qualityScript -Path @($reportScript)

$configPath = Join-Path $repoRoot 'config-samples\runtime-script-quality-baseline.sample.json'
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
foreach ($propertyName in @('description', 'defaultTargetPathFragments', 'rules')) {
    if ($null -eq $config.PSObject.Properties[$propertyName]) {
        throw ('Script quality baseline sample is missing property: {0}' -f $propertyName)
    }
}

$rules = $config.rules
foreach ($ruleName in @('avoidMyInvocationScriptName', 'avoidFragileColonInterpolation', 'avoidStrictModeUnsafeXmlPropertyAccess', 'avoidInlinePackageReferenceVersion', 'avoidSecretEcho', 'avoidBinObjRegexPathChecks')) {
    if ($null -eq $rules.PSObject.Properties[$ruleName]) {
        throw ('Script quality baseline sample is missing rule: {0}' -f $ruleName)
    }
}

Write-Host 'P7.9G runtime validator hardening validation passed.'
