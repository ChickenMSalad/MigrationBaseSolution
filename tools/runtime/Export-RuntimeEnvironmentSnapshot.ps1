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
    [string]$EnvironmentName = "unknown"
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

function Resolve-FullPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return (Join-Path (Get-Location).Path $Path)
}

function Read-JsonFile {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $null
    }

    $fullPath = Resolve-FullPath -Path $Path
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "JSON file not found: $fullPath"
    }

    $raw = Get-Content -LiteralPath $fullPath -Raw
    if ([string]::IsNullOrWhiteSpace($raw)) {
        return $null
    }

    return $raw | ConvertFrom-Json
}

function Convert-AppSettingsToMap {
    param($Settings)

    $map = @{}
    if ($null -eq $Settings) {
        return $map
    }

    foreach ($item in @($Settings)) {
        if ($null -eq $item) { continue }
        $nameProperty = $item.PSObject.Properties["name"]
        $valueProperty = $item.PSObject.Properties["value"]
        if ($null -eq $nameProperty) { continue }

        $name = [string]$nameProperty.Value
        if ([string]::IsNullOrWhiteSpace($name)) { continue }

        $value = $null
        if ($null -ne $valueProperty) {
            $value = $valueProperty.Value
        }

        $map[$name] = $value
    }

    return $map
}

function Read-SchemaText {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $null
    }

    $fullPath = Resolve-FullPath -Path $Path
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "SQL schema export file not found: $fullPath"
    }

    return Get-Content -LiteralPath $fullPath -Raw
}

$outputFullPath = Resolve-FullPath -Path $OutputPath
$outputDirectory = Split-Path -Path $outputFullPath -Parent
if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

$dispatcherSettings = Convert-AppSettingsToMap -Settings (Read-JsonFile -Path $DispatcherAppSettingsPath)
$executorSettings = Convert-AppSettingsToMap -Settings (Read-JsonFile -Path $ExecutorAppSettingsPath)
$schemaText = Read-SchemaText -Path $SqlSchemaPath

$snapshot = [ordered]@{
    environmentName = $EnvironmentName
    generatedUtc = [DateTimeOffset]::UtcNow.ToString("o")
    dispatcherAppSettings = $dispatcherSettings
    executorAppSettings = $executorSettings
    sqlSchemaText = $schemaText
}

$snapshot | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $outputFullPath -Encoding UTF8
Write-Host "Runtime environment snapshot written to $outputFullPath"
