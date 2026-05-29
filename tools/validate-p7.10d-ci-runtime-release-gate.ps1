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
    'docs\p7\P7.10D-CI-Runtime-Release-Gate.md',
    'config-samples\runtime-ci-release-gate.sample.json',
    'tools\runtime\Invoke-RuntimeCiReleaseGate.ps1',
    '.github\workflows\runtime-ci-release-gate.sample.yml'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw ('Required P7.10D file is missing: {0}' -f $relativePath)
    }
}

$scriptsToParse = @(
    'tools\runtime\Invoke-RuntimeCiReleaseGate.ps1'
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

$configPath = Join-Path $repoRoot 'config-samples\runtime-ci-release-gate.sample.json'
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
foreach ($propertyName in @('validators', 'requiredFiles')) {
    if ($null -eq $config.PSObject.Properties[$propertyName]) {
        throw ('Runtime CI release gate config is missing property: {0}' -f $propertyName)
    }
}

$gateScriptPath = Join-Path $repoRoot 'tools\runtime\Invoke-RuntimeCiReleaseGate.ps1'
$gateScriptText = Get-Content -LiteralPath $gateScriptPath -Raw
$fragileInvocationPattern = '$' + 'MyInvocation' + '.ScriptName'
if ($gateScriptText.IndexOf($fragileInvocationPattern, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
    throw 'Runtime CI release gate script contains fragile invocation-root usage.'
}
if ($gateScriptText -match '\$[A-Za-z_][A-Za-z0-9_]*:' -and $gateScriptText -notmatch '\$(script|global|local|private|using|env):') {
    throw 'Runtime CI release gate script contains potential fragile colon interpolation.'
}

Write-Host 'P7.10D CI runtime release gate validation passed.'
