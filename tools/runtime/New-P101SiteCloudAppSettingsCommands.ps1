[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $SettingsTemplatePath,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $ResourceGroup,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $AppName,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $OutputPath
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

$templateFullPath = Resolve-FullPath -PathValue $SettingsTemplatePath
if (-not (Test-Path -LiteralPath $templateFullPath)) {
    throw ('Settings template not found: {0}' -f $templateFullPath)
}

$outputFullPath = Resolve-FullPath -PathValue $OutputPath
$outputParent = Split-Path -Parent $outputFullPath
if (-not [string]::IsNullOrWhiteSpace($outputParent) -and -not (Test-Path -LiteralPath $outputParent)) {
    New-Item -ItemType Directory -Path $outputParent -Force | Out-Null
}

$templateRaw = Get-Content -LiteralPath $templateFullPath -Raw
$template = ConvertFrom-Json -InputObject $templateRaw

if ($null -eq $template.PSObject.Properties['settings']) {
    throw 'Settings template is missing settings object.'
}

$lines = @()
$lines += '# Generated P10.1B Admin API/site appsettings apply commands.'
$lines += '# Review placeholder values before running.'
$lines += '$ErrorActionPreference = ''Stop'''
$lines += ''
$lines += 'az webapp config appsettings set `'
$lines += ('  --resource-group {0} `' -f $ResourceGroup)
$lines += ('  --name {0} `' -f $AppName)
$lines += '  --settings `'

$properties = @($template.settings.PSObject.Properties | Sort-Object Name)
for ($index = 0; $index -lt $properties.Count; $index++) {
    $property = $properties[$index]
    $settingText = ('    "{0}={1}"' -f $property.Name, $property.Value)
    if ($index -lt ($properties.Count - 1)) {
        $settingText += ' `'
    }
    $lines += $settingText
}

$lines += ''
$lines += '# Optional verification export:'
$lines += ('az webapp config appsettings list --resource-group {0} --name {1} -o json' -f $ResourceGroup, $AppName)

Set-Content -LiteralPath $outputFullPath -Value $lines -Encoding UTF8
Write-Host ('Generated appsettings apply commands: {0}' -f $outputFullPath)
