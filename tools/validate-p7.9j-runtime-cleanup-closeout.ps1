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
    'docs\p7\P7.9J-Runtime-Cleanup-Closeout-Gate.md',
    'config-samples\runtime-cleanup-closeout-gate.sample.json',
    'tools\runtime\Invoke-RuntimeCleanupCloseoutGate.ps1'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw ('Required P7.9J file is missing: {0}' -f $relativePath)
    }
}

$scriptsToParse = @(
    'tools\runtime\Invoke-RuntimeCleanupCloseoutGate.ps1',
    'tools\validate-p7.9j-runtime-cleanup-closeout.ps1'
)

foreach ($relativeScript in $scriptsToParse) {
    $scriptPath = Join-Path $repoRoot $relativeScript
    $parseErrors = $null
    $scriptText = Get-Content -LiteralPath $scriptPath -Raw
    [System.Management.Automation.PSParser]::Tokenize($scriptText, [ref]$parseErrors) | Out-Null
    if ($null -ne $parseErrors -and @($parseErrors).Count -gt 0) {
        $message = (@($parseErrors) | ForEach-Object { $_.Message }) -join '; '
        throw ('PowerShell parser errors in {0}: {1}' -f $relativeScript, $message)
    }
}

$configPath = Join-Path $repoRoot 'config-samples\runtime-cleanup-closeout-gate.sample.json'
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
foreach ($propertyName in @('phase', 'validatorScripts', 'missingValidatorBehavior')) {
    if ($null -eq $config.PSObject.Properties[$propertyName]) {
        throw ('Closeout gate sample configuration is missing property: {0}' -f $propertyName)
    }
}

$validatorScripts = @($config.validatorScripts)
if ($validatorScripts.Count -lt 5) {
    throw 'Closeout gate sample configuration must include the P7.9 validator list.'
}

foreach ($validatorScript in $validatorScripts) {
    if ([string]::IsNullOrWhiteSpace([string]$validatorScript)) {
        throw 'Closeout gate sample configuration contains an empty validator script entry.'
    }
    if (([string]$validatorScript) -notmatch '^tools/') {
        throw ('Validator script path must be repo-relative under tools/: {0}' -f $validatorScript)
    }
}

$runnerText = Get-Content -LiteralPath (Join-Path $repoRoot 'tools\runtime\Invoke-RuntimeCleanupCloseoutGate.ps1') -Raw
foreach ($requiredTerm in @('validatorScripts', 'ContinueOnFailure', 'Set-Content', 'Runtime cleanup closeout report')) {
    if ($runnerText.IndexOf($requiredTerm, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw ('Closeout gate runner is missing expected term: {0}' -f $requiredTerm)
    }
}

Write-Host 'P7.9J runtime cleanup closeout gate validation passed.'
