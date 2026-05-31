[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $SettingsPath,

    [Parameter(Mandatory = $false)]
    [string] $TemplatePath
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Resolve-FullPath {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $PathValue
    )

    if ([System.IO.Path]::IsPathRooted($PathValue)) {
        return $PathValue
    }

    return [System.IO.Path]::GetFullPath((Join-Path (Get-Location).Path $PathValue))
}

function Get-SettingMap {
    param(
        [Parameter(Mandatory = $true)]
        [string] $FullPath
    )

    if (-not (Test-Path -LiteralPath $FullPath)) {
        throw ('Settings file not found: {0}' -f $FullPath)
    }

    $raw = Get-Content -LiteralPath $FullPath -Raw
    $json = ConvertFrom-Json -InputObject $raw
    $map = @{}

    if ($json -is [System.Array]) {
        foreach ($item in @($json)) {
            if ($null -ne $item.PSObject.Properties['name']) {
                $name = [string] $item.name
                $value = $null
                if ($null -ne $item.PSObject.Properties['value']) {
                    $value = $item.value
                }
                $map[$name] = $value
            }
        }
        return $map
    }

    if ($null -ne $json.PSObject.Properties['settings']) {
        foreach ($property in @($json.settings.PSObject.Properties)) {
            $map[$property.Name] = $property.Value
        }
        return $map
    }

    foreach ($property in @($json.PSObject.Properties)) {
        $map[$property.Name] = $property.Value
    }

    return $map
}

$settingsFullPath = Resolve-FullPath -PathValue $SettingsPath

if ([string]::IsNullOrWhiteSpace($TemplatePath)) {
    $scriptRoot = $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
        if (-not [string]::IsNullOrWhiteSpace($PSCommandPath)) {
            $scriptRoot = Split-Path -Parent $PSCommandPath
        }
    }
    if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
        throw 'Unable to resolve script root.'
    }
    $repoRoot = Split-Path -Parent (Split-Path -Parent $scriptRoot)
    $TemplatePath = Join-Path $repoRoot 'config-samples\p10-site-cloud-appsettings.canonical.azure.sample.json'
}

$templateFullPath = Resolve-FullPath -PathValue $TemplatePath
$settingsMap = Get-SettingMap -FullPath $settingsFullPath
$templateMap = Get-SettingMap -FullPath $templateFullPath
$templateRaw = Get-Content -LiteralPath $templateFullPath -Raw
$template = ConvertFrom-Json -InputObject $templateRaw

$issues = @()

foreach ($name in @($templateMap.Keys | Sort-Object)) {
    if (-not $settingsMap.ContainsKey($name)) {
        $issues += ('Missing required setting: {0}' -f $name)
    }
}

if ($settingsMap.ContainsKey('AdminApi__AllowInProcessExecution')) {
    $value = [string] $settingsMap['AdminApi__AllowInProcessExecution']
    if ($value -ne 'false') {
        $issues += 'AdminApi__AllowInProcessExecution must be false for Azure site readiness.'
    }
}

if ($settingsMap.ContainsKey('SqlOperationalWorkItemQueue__SchemaName')) {
    if ([string] $settingsMap['SqlOperationalWorkItemQueue__SchemaName'] -ne 'migration') {
        $issues += 'SqlOperationalWorkItemQueue__SchemaName must be migration.'
    }
}

if ($settingsMap.ContainsKey('SqlOperationalWorkItemQueue__WorkItemsTableName')) {
    if ([string] $settingsMap['SqlOperationalWorkItemQueue__WorkItemsTableName'] -ne 'WorkItems') {
        $issues += 'SqlOperationalWorkItemQueue__WorkItemsTableName must be WorkItems.'
    }
}

if ($null -ne $template.PSObject.Properties['forbiddenRuntimeAliases']) {
    foreach ($name in @($template.forbiddenRuntimeAliases)) {
        if ($settingsMap.ContainsKey([string] $name)) {
            $issues += ('Forbidden runtime alias setting is present: {0}' -f $name)
        }
    }
}

if (@($issues).Count -gt 0) {
    throw (('P10.1B site cloud appsettings contract failed.' + [Environment]::NewLine) + (($issues | ForEach-Object { '- ' + $_ }) -join [Environment]::NewLine))
}

Write-Host 'P10.1B site cloud appsettings contract passed.'
