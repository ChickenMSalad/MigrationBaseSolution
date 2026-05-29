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
    'docs\p7\P7.10J-Runtime-Final-Closeout-Gate.md',
    'config-samples\runtime-p710-final-closeout-gate.sample.json',
    'tools\runtime\Invoke-RuntimeP710FinalCloseoutGate.ps1'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw ('Required P7.10J file is missing: {0}' -f $relativePath)
    }
}

$scriptsToParse = @(
    'tools\runtime\Invoke-RuntimeP710FinalCloseoutGate.ps1'
)

foreach ($relativeScript in $scriptsToParse) {
    $scriptPath = Join-Path $repoRoot $relativeScript
    $parseErrors = $null
    [System.Management.Automation.PSParser]::Tokenize((Get-Content -LiteralPath $scriptPath -Raw), [ref]$parseErrors) | Out-Null
    if ($null -ne $parseErrors -and @($parseErrors).Count -gt 0) {
        $message = (@($parseErrors) | ForEach-Object { $_.Message }) -join '; '
        throw ('PowerShell parser errors in {0}: {1}' -f $relativeScript, $message)
    }

    $scriptText = Get-Content -LiteralPath $scriptPath -Raw
    $fragileInvocationPattern = '$' + 'MyInvocation' + '.ScriptName'
    if ($scriptText.IndexOf($fragileInvocationPattern, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw ('Avoid fragile invocation-root usage in {0}' -f $relativeScript)
    }
    if ($scriptText -match '\$[A-Za-z_][A-Za-z0-9_]*:' -and $scriptText -notmatch '\$(script|global|local|private|using|env):') {
        throw ('Potential fragile colon interpolation in {0}' -f $relativeScript)
    }
}

$configPath = Join-Path $repoRoot 'config-samples\runtime-p710-final-closeout-gate.sample.json'
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
if ($null -eq $config.PSObject.Properties['validators']) {
    throw 'Final closeout configuration is missing validators.'
}
if (@($config.validators).Count -lt 5) {
    throw 'Final closeout configuration should include the P7.10 validators.'
}

Write-Host 'P7.10J runtime final closeout validation passed.'
