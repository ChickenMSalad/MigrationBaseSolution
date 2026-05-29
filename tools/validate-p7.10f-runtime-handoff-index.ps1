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
    'docs\p7\P7.10F-Runtime-Handoff-Index.md',
    'docs\operations\runtime-clean-handoff-index.md',
    'config-samples\runtime-handoff-index.sample.json',
    'tools\runtime\New-RuntimeHandoffIndexReport.ps1'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = [System.IO.Path]::Combine($repoRoot, $relativePath)
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw ('Required P7.10F file is missing: {0}' -f $relativePath)
    }
}

$scriptPath = [System.IO.Path]::Combine($repoRoot, 'tools\runtime\New-RuntimeHandoffIndexReport.ps1')
$parseErrors = $null
[System.Management.Automation.PSParser]::Tokenize((Get-Content -LiteralPath $scriptPath -Raw), [ref]$parseErrors) | Out-Null
if ($null -ne $parseErrors -and @($parseErrors).Count -gt 0) {
    $message = (@($parseErrors) | ForEach-Object { $_.Message }) -join '; '
    throw ('PowerShell parser errors in tools\runtime\New-RuntimeHandoffIndexReport.ps1: {0}' -f $message)
}

$scriptText = Get-Content -LiteralPath $scriptPath -Raw
$fragileInvocationPattern = '$' + 'MyInvocation' + '.ScriptName'
if ($scriptText.IndexOf($fragileInvocationPattern, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
    throw 'Runtime handoff report script contains fragile invocation-root usage.'
}

$forbiddenXmlPropertyFragments = @('.PackageReference', '.ItemGroup', '.None', '.Content')
foreach ($fragment in $forbiddenXmlPropertyFragments) {
    if ($scriptText.IndexOf($fragment, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw ('Runtime handoff report script contains potential StrictMode-unsafe XML property access: {0}' -f $fragment)
    }
}

$configPath = [System.IO.Path]::Combine($repoRoot, 'config-samples\runtime-handoff-index.sample.json')
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
foreach ($propertyName in @('handoffName', 'requiredValidators', 'requiredEvidence')) {
    if ($null -eq $config.PSObject.Properties[$propertyName]) {
        throw ('Runtime handoff config is missing property: {0}' -f $propertyName)
    }
}

Write-Host 'P7.10F runtime handoff index validation passed.'
