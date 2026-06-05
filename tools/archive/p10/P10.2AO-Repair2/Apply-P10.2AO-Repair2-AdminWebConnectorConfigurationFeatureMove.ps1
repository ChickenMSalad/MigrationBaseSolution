Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $current = (Get-Location).Path
    while ($null -ne $current -and $current.Length -gt 0) {
        $marker = [System.IO.Path]::Combine($current, 'MigrationBaseSolution.sln')
        if (Test-Path -Path $marker -PathType Leaf) {
            return $current
        }
        $parent = Split-Path -Path $current -Parent
        if ($parent -eq $current) { break }
        $current = $parent
    }
    throw 'Unable to locate repo root containing MigrationBaseSolution.sln.'
}

function Ensure-Directory {
    param([Parameter(Mandatory=$true)][string]$Path)
    if (-not (Test-Path -Path $Path -PathType Container)) {
        New-Item -Path $Path -ItemType Directory -Force | Out-Null
    }
}

function Move-IfNeeded {
    param(
        [Parameter(Mandatory=$true)][string]$Source,
        [Parameter(Mandatory=$true)][string]$Destination,
        [Parameter(Mandatory=$true)][string]$Label
    )

    if (Test-Path -Path $Destination -PathType Leaf) {
        Write-Host ('Already moved {0}: {1}' -f $Label, $Destination)
        return
    }

    if (-not (Test-Path -Path $Source -PathType Leaf)) {
        throw ('Required source file was not found for {0}: {1}' -f $Label, $Source)
    }

    Ensure-Directory -Path (Split-Path -Path $Destination -Parent)
    Move-Item -Path $Source -Destination $Destination
    Write-Host ('Moved {0}: {1}' -f $Label, $Destination)
}

function Read-TextFile {
    param([Parameter(Mandatory=$true)][string]$Path)
    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Required file was not found: {0}' -f $Path)
    }
    return [System.IO.File]::ReadAllText($Path)
}

function Write-TextFile {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$Content
    )
    [System.IO.File]::WriteAllText($Path, $Content, [System.Text.UTF8Encoding]::new($false))
}

function Normalize-ImportSource {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$ModuleSuffix,
        [Parameter(Mandatory=$true)][string]$NewSource,
        [Parameter(Mandatory=$true)][string]$Label
    )

    $content = Read-TextFile -Path $Path
    $escapedSuffix = [regex]::Escape($ModuleSuffix)
    $pattern = '(?m)(import\s+[^;]+?\s+from\s+["''])([^"'']*' + $escapedSuffix + ')(["'']\s*;)'
    $match = [regex]::Match($content, $pattern)
    if (-not $match.Success) {
        Write-Host ('No import normalization needed for {0}; import was not found.' -f $Label)
        return
    }

    $currentSource = $match.Groups[2].Value
    if ($currentSource -eq $NewSource) {
        Write-Host ('Already updated {0}: {1}' -f $Label, $Path)
        return
    }

    $replacement = '${1}' + $NewSource + '${3}'
    $updated = [regex]::Replace($content, $pattern, $replacement, 1)
    Write-TextFile -Path $Path -Content $updated
    Write-Host ('Updated {0}: {1}' -f $Label, $Path)
}

function Normalize-AppImport {
    param(
        [Parameter(Mandatory=$true)][string]$AppPath
    )

    $content = Read-TextFile -Path $AppPath
    $newImport = 'import { ConnectorConfiguration } from "./features/connectors/configuration/pages/ConnectorConfiguration";'

    if ($content.Contains($newImport)) {
        Write-Host ('Already updated App.tsx ConnectorConfiguration import: {0}' -f $AppPath)
        return
    }

    $pattern = '(?m)^import\s+\{\s*ConnectorConfiguration\s*\}\s+from\s+["''][^"'']*ConnectorConfiguration["'']\s*;'
    if ([regex]::IsMatch($content, $pattern)) {
        $updated = [regex]::Replace($content, $pattern, $newImport, 1)
        Write-TextFile -Path $AppPath -Content $updated
        Write-Host ('Updated App.tsx ConnectorConfiguration import: {0}' -f $AppPath)
        return
    }

    if ($content.Contains('<ConnectorConfiguration')) {
        $lines = $content -split "`r?`n"
        $insertIndex = 0
        for ($i = 0; $i -lt $lines.Count; $i++) {
            if ($lines[$i] -match '^import\s+') { $insertIndex = $i + 1 }
        }
        $before = @()
        $after = @()
        if ($insertIndex -gt 0) { $before = $lines[0..($insertIndex - 1)] }
        if ($insertIndex -lt $lines.Count) { $after = $lines[$insertIndex..($lines.Count - 1)] }
        $updatedLines = @($before) + @($newImport) + @($after)
        Write-TextFile -Path $AppPath -Content ($updatedLines -join [Environment]::NewLine)
        Write-Host ('Inserted App.tsx ConnectorConfiguration import: {0}' -f $AppPath)
        return
    }

    Write-Host 'App.tsx does not reference ConnectorConfiguration; no import change required.'
}

$repoRoot = Get-RepoRoot
$adminSrc = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src')

$pageSource = [System.IO.Path]::Combine($adminSrc, 'pages', 'ConnectorConfiguration.tsx')
$apiSource = [System.IO.Path]::Combine($adminSrc, 'api', 'connectorConfigurationApi.ts')
$typeSource = [System.IO.Path]::Combine($adminSrc, 'types', 'connectorConfiguration.ts')

$featureRoot = [System.IO.Path]::Combine($adminSrc, 'features', 'connectors', 'configuration')
$pageDestination = [System.IO.Path]::Combine($featureRoot, 'pages', 'ConnectorConfiguration.tsx')
$apiDestination = [System.IO.Path]::Combine($featureRoot, 'api', 'connectorConfigurationApi.ts')
$typeDestination = [System.IO.Path]::Combine($featureRoot, 'types', 'connectorConfiguration.ts')
$appPath = [System.IO.Path]::Combine($adminSrc, 'App.tsx')

Move-IfNeeded -Source $pageSource -Destination $pageDestination -Label 'Connector Configuration page'
Move-IfNeeded -Source $apiSource -Destination $apiDestination -Label 'Connector Configuration API'
Move-IfNeeded -Source $typeSource -Destination $typeDestination -Label 'Connector Configuration types'

Normalize-ImportSource -Path $pageDestination -ModuleSuffix 'connectorConfigurationApi' -NewSource '../api/connectorConfigurationApi' -Label 'Connector Configuration page API import'
Normalize-ImportSource -Path $pageDestination -ModuleSuffix 'connectorConfiguration' -NewSource '../types/connectorConfiguration' -Label 'Connector Configuration page types import'
Normalize-ImportSource -Path $pageDestination -ModuleSuffix 'components/Card' -NewSource '../../../../components/Card' -Label 'Connector Configuration page Card import'
Normalize-ImportSource -Path $pageDestination -ModuleSuffix 'components/LoadingError' -NewSource '../../../../components/LoadingError' -Label 'Connector Configuration page LoadingError import'
Normalize-ImportSource -Path $apiDestination -ModuleSuffix 'types/connectorConfiguration' -NewSource '../types/connectorConfiguration' -Label 'Connector Configuration API types import'
Normalize-ImportSource -Path $apiDestination -ModuleSuffix 'api/core/adminApiClient' -NewSource '../../../../api/core/adminApiClient' -Label 'Connector Configuration API client import'
Normalize-ImportSource -Path $apiDestination -ModuleSuffix 'api/core/client' -NewSource '../../../../api/core/client' -Label 'Connector Configuration API legacy client import'
Normalize-AppImport -AppPath $appPath

Write-Host 'P10.2AO Repair2 Admin Web Connector Configuration feature move repair applied.'
