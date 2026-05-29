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
    'docs\p7\P7.10I-Runtime-Stabilization-Closeout.md',
    'docs\operations\runtime-stabilization-closeout.md',
    'config-samples\runtime-stabilization-closeout.sample.json',
    'tools\runtime\New-RuntimeStabilizationCloseoutReport.ps1'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw ('Required P7.10I file is missing: {0}' -f $relativePath)
    }
}

$scriptToParse = Join-Path $repoRoot 'tools\runtime\New-RuntimeStabilizationCloseoutReport.ps1'
$parseErrors = $null
[System.Management.Automation.PSParser]::Tokenize((Get-Content -LiteralPath $scriptToParse -Raw), [ref]$parseErrors) | Out-Null
if ($null -ne $parseErrors -and @($parseErrors).Count -gt 0) {
    $message = (@($parseErrors) | ForEach-Object { $_.Message }) -join '; '
    throw ('PowerShell parser errors in runtime closeout report script: {0}' -f $message)
}

$scriptText = Get-Content -LiteralPath $scriptToParse -Raw
$fragileInvocationPattern = '$' + 'MyInvocation' + '.ScriptName'
if ($scriptText.IndexOf($fragileInvocationPattern, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
    throw 'Runtime closeout report script contains fragile invocation-root usage.'
}

$forbiddenXmlFragments = @('.PackageReference', '.None', '.Content', '.ItemGroup')
foreach ($fragment in $forbiddenXmlFragments) {
    if ($scriptText.IndexOf($fragment, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw ('Runtime closeout report script contains StrictMode-risk XML fragment: {0}' -f $fragment)
    }
}

$configPath = Join-Path $repoRoot 'config-samples\runtime-stabilization-closeout.sample.json'
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
foreach ($propertyName in @('requiredEvidence', 'retainedMigrationPrefixedKeys', 'canonicalSmokeManifestType')) {
    if ($null -eq $config.PSObject.Properties[$propertyName]) {
        throw ('Closeout config is missing property: {0}' -f $propertyName)
    }
}

Write-Host 'P7.10I runtime stabilization closeout validation passed.'
