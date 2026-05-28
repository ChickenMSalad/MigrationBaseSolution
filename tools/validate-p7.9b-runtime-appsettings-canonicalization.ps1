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
    throw 'Unable to resolve validator script root.'
}

$repoRoot = Split-Path -Parent $toolRoot

$requiredFiles = @(
    'docs\p7\P7.9B-Runtime-AppSettings-Canonicalization.md',
    'config-samples\appsettings.SqlServiceBusRuntime.clean.canonical.json',
    'tools\runtime\Get-RuntimeCanonicalAppSettingsPlan.ps1',
    'tools\runtime\New-RuntimeCanonicalAppSettingsApplyCommands.ps1',
    'tools\runtime\Test-RuntimeCanonicalAppSettings.ps1'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw ('Required P7.9B file is missing: {0}' -f $relativePath)
    }
}

$samplePath = Join-Path $repoRoot 'config-samples\appsettings.SqlServiceBusRuntime.clean.canonical.json'
$sample = Get-Content -LiteralPath $samplePath -Raw | ConvertFrom-Json

if ($null -eq $sample.ConnectionStrings) {
    throw 'Canonical sample is missing ConnectionStrings.'
}
if ($null -eq $sample.SqlServiceBusDispatcher) {
    throw 'Canonical sample is missing SqlServiceBusDispatcher.'
}
if ($null -eq $sample.SqlServiceBusExecutor) {
    throw 'Canonical sample is missing SqlServiceBusExecutor.'
}
if ($null -eq $sample.SqlOperationalWorkItemQueue) {
    throw 'Canonical sample is missing SqlOperationalWorkItemQueue.'
}

$runtimeScripts = @(
    'tools\runtime\Get-RuntimeCanonicalAppSettingsPlan.ps1',
    'tools\runtime\New-RuntimeCanonicalAppSettingsApplyCommands.ps1',
    'tools\runtime\Test-RuntimeCanonicalAppSettings.ps1'
)

foreach ($relativeScript in $runtimeScripts) {
    $scriptPath = Join-Path $repoRoot $relativeScript
    $tokens = $null
    $errors = $null
    [System.Management.Automation.Language.Parser]::ParseFile($scriptPath, [ref]$tokens, [ref]$errors) | Out-Null
    if ($null -ne $errors -and @($errors).Count -gt 0) {
        $messages = @($errors | ForEach-Object { $_.Message }) -join '; '
        throw ('PowerShell parser errors in {0}: {1}' -f $relativeScript, $messages)
    }

    $scriptText = Get-Content -LiteralPath $scriptPath -Raw
    if ($scriptText.IndexOf('$MyInvocation.ScriptName', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw ('Runtime script uses fragile invocation-root pattern: {0}' -f $relativeScript)
    }
    if ($scriptText.IndexOf('PackageReference Version=', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw ('Runtime script contains inline PackageReference Version text: {0}' -f $relativeScript)
    }
}

Write-Host 'P7.9B runtime AppSettings canonicalization drop-in validation passed.'
