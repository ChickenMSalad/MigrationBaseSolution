[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $SettingsPath,

    [Parameter(Mandatory = $true)]
    [ValidateSet('Dispatcher', 'Executor')]
    [string] $Role,

    [Parameter(Mandatory = $false)]
    [switch] $FailOnLegacy
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $PSCommandPath
}
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    throw 'Unable to resolve script root.'
}

$planScript = Join-Path $scriptRoot 'Get-RuntimeCanonicalAppSettingsPlan.ps1'
if (-not (Test-Path -LiteralPath $planScript)) {
    throw ('Required helper script is missing: {0}' -f $planScript)
}

$planJson = & $planScript -SettingsPath $SettingsPath -Role $Role
$plan = ConvertFrom-Json -InputObject ($planJson | Out-String)

$issues = New-Object System.Collections.Generic.List[string]

foreach ($key in @($plan.missingCanonicalKeys)) {
    $issues.Add(('Missing canonical key: {0}' -f $key)) | Out-Null
}

if ($FailOnLegacy) {
    foreach ($key in @($plan.legacyKeysToReviewForDeletion)) {
        $issues.Add(('Legacy key still present: {0}' -f $key)) | Out-Null
    }
}

if (@($issues).Count -gt 0) {
    $message = [string]::Join([Environment]::NewLine, @($issues))
    throw ('Runtime canonical AppSettings validation failed for {0}.{1}{2}' -f $Role, [Environment]::NewLine, $message)
}

Write-Host ('Runtime canonical AppSettings validation passed for {0}.' -f $Role)
