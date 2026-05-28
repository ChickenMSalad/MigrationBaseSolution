[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$OutputPath,

    [Parameter(Mandatory = $false)]
    [string]$DispatcherAppSettingsPath,

    [Parameter(Mandatory = $false)]
    [string]$ExecutorAppSettingsPath,

    [Parameter(Mandatory = $false)]
    [string]$SqlSchemaPath,

    [Parameter(Mandatory = $false)]
    [string]$EnvironmentName = 'unknown'
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Resolve-FullPath {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$Path
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return (Join-Path -Path (Get-Location).Path -ChildPath $Path)
}

function Read-TextFileOrNull {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $false)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$Description
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $null
    }

    $fullPath = Resolve-FullPath -Path $Path
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw ('{0} file not found: {1}' -f $Description, $fullPath)
    }

    return [System.IO.File]::ReadAllText($fullPath)
}

function Read-JsonFileOrNull {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $false)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$Description
    )

    $raw = Read-TextFileOrNull -Path $Path -Description $Description
    if ([string]::IsNullOrWhiteSpace($raw)) {
        return $null
    }

    try {
        return ConvertFrom-Json -InputObject $raw
    }
    catch {
        throw ('{0} file is not valid JSON. Path={1}. Error={2}' -f $Description, $Path, $_.Exception.Message)
    }
}

function Convert-AppSettingsToMap {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $false)]
        $Settings
    )

    $map = [ordered]@{}
    if ($null -eq $Settings) {
        return $map
    }

    foreach ($item in @($Settings)) {
        if ($null -eq $item) {
            continue
        }

        $nameProperty = $item.PSObject.Properties['name']
        if ($null -eq $nameProperty) {
            continue
        }

        $name = [string]$nameProperty.Value
        if ([string]::IsNullOrWhiteSpace($name)) {
            continue
        }

        $value = $null
        $valueProperty = $item.PSObject.Properties['value']
        if ($null -ne $valueProperty) {
            $value = $valueProperty.Value
        }

        $map[$name] = $value
    }

    return $map
}

$outputFullPath = Resolve-FullPath -Path $OutputPath
$outputDirectory = Split-Path -Path $outputFullPath -Parent
if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

$dispatcherSettingsObject = Read-JsonFileOrNull -Path $DispatcherAppSettingsPath -Description 'Dispatcher app settings'
$executorSettingsObject = Read-JsonFileOrNull -Path $ExecutorAppSettingsPath -Description 'Executor app settings'
$schemaText = Read-TextFileOrNull -Path $SqlSchemaPath -Description 'SQL schema export'

$dispatcherSettings = Convert-AppSettingsToMap -Settings $dispatcherSettingsObject
$executorSettings = Convert-AppSettingsToMap -Settings $executorSettingsObject

$snapshot = [ordered]@{
    environmentName = $EnvironmentName
    generatedUtc = [DateTimeOffset]::UtcNow.ToString('o')
    inputs = [ordered]@{
        dispatcherAppSettingsPath = $DispatcherAppSettingsPath
        executorAppSettingsPath = $ExecutorAppSettingsPath
        sqlSchemaPath = $SqlSchemaPath
    }
    dispatcherAppSettings = $dispatcherSettings
    executorAppSettings = $executorSettings
    sqlSchemaText = $schemaText
}

try {
    $json = ConvertTo-Json -InputObject $snapshot -Depth 12
    [System.IO.File]::WriteAllText($outputFullPath, $json, [System.Text.Encoding]::UTF8)
}
catch {
    throw ('Failed to write runtime environment snapshot to {0}. Error={1}' -f $outputFullPath, $_.Exception.Message)
}

Write-Host ('Runtime environment snapshot written to {0}' -f $outputFullPath)
