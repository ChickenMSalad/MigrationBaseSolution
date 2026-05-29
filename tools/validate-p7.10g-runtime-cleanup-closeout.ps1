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
    'docs\p7\P7.10G-Runtime-Cleanup-Closeout-Gate.md',
    'config-samples\runtime-p710-closeout-gate.sample.json',
    'tools\runtime\Invoke-RuntimeP710CloseoutGate.ps1'
)
foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw ('Required P7.10G file is missing: {0}' -f $relativePath)
    }
}

$scriptsToParse = @('tools\runtime\Invoke-RuntimeP710CloseoutGate.ps1')
foreach ($relativeScript in $scriptsToParse) {
    $scriptPath = Join-Path $repoRoot $relativeScript
    $parseErrors = $null
    [System.Management.Automation.PSParser]::Tokenize((Get-Content -LiteralPath $scriptPath -Raw), [ref]$parseErrors) | Out-Null
    if ($null -ne $parseErrors -and @($parseErrors).Count -gt 0) {
        $message = (@($parseErrors) | ForEach-Object { $_.Message }) -join '; '
        throw ('PowerShell parser errors in {0}: {1}' -f $relativeScript, $message)
    }
}

$configPath = Join-Path $repoRoot 'config-samples\runtime-p710-closeout-gate.sample.json'
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
if ($null -eq $config.PSObject.Properties['validators']) {
    throw 'Closeout gate sample configuration is missing validators.'
}
if (@($config.validators).Count -lt 6) {
    throw 'Closeout gate sample configuration must include P7.10A through P7.10F validators.'
}

Write-Host 'P7.10G runtime cleanup closeout validation passed.'
