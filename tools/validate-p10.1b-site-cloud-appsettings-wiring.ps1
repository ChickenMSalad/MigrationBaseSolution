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
    'docs\p10\P10.1B-Site-Cloud-AppSettings-Wiring.md',
    'config-samples\p10-site-cloud-appsettings.canonical.azure.sample.json',
    'tools\runtime\Test-P101SiteCloudAppSettingsContract.ps1',
    'tools\runtime\New-P101SiteCloudAppSettingsCommands.ps1'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw ('Required P10.1B file is missing: {0}' -f $relativePath)
    }
}

$scriptsToParse = @(
    'tools\runtime\Test-P101SiteCloudAppSettingsContract.ps1',
    'tools\runtime\New-P101SiteCloudAppSettingsCommands.ps1'
)

foreach ($relativeScript in $scriptsToParse) {
    $scriptPath = Join-Path $repoRoot $relativeScript
    $parseErrors = $null
    [System.Management.Automation.PSParser]::Tokenize((Get-Content -LiteralPath $scriptPath -Raw), [ref] $parseErrors) | Out-Null
    if ($null -ne $parseErrors -and @($parseErrors).Count -gt 0) {
        $message = (@($parseErrors) | ForEach-Object { $_.Message }) -join '; '
        throw ('PowerShell parser errors in {0}: {1}' -f $relativeScript, $message)
    }
}

$templatePath = Join-Path $repoRoot 'config-samples\p10-site-cloud-appsettings.canonical.azure.sample.json'
$template = Get-Content -LiteralPath $templatePath -Raw | ConvertFrom-Json
if ($null -eq $template.PSObject.Properties['settings']) {
    throw 'P10.1B sample is missing settings object.'
}

$requiredSettingNames = @(
    'ConnectionStrings__MigrationOperationalStore',
    'SqlOperationalRuntimeReadiness__ConnectionString',
    'SqlOperationalWorkItemQueue__SchemaName',
    'SqlOperationalWorkItemQueue__WorkItemsTableName',
    'AdminApi__StorageRoot',
    'AdminApi__AllowInProcessExecution',
    'MigrationExecution__StatePath'
)

foreach ($settingName in $requiredSettingNames) {
    if ($null -eq $template.settings.PSObject.Properties[$settingName]) {
        throw ('P10.1B sample is missing required setting: {0}' -f $settingName)
    }
}

if ([string] $template.settings.AdminApi__AllowInProcessExecution -ne 'false') {
    throw 'AdminApi__AllowInProcessExecution must be false in the Azure sample.'
}

Write-Host 'P10.1B site cloud appsettings wiring validation passed.'
