Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $current = Get-Location
    while ($null -ne $current) {
        $candidate = [System.IO.Path]::Combine($current.Path, 'src', 'Admin', 'Migration.Admin.Web', 'src', 'App.tsx')
        if (Test-Path -Path $candidate -PathType Leaf) {
            return $current.Path
        }
        $parent = Split-Path -Path $current.Path -Parent
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $current.Path) {
            break
        }
        $current = Get-Item -LiteralPath $parent
    }
    throw 'Unable to find repository root from the current directory.'
}

function Read-TextFile {
    param([Parameter(Mandatory = $true)][string]$Path)
    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Required file was not found: {0}' -f $Path)
    }
    return [System.IO.File]::ReadAllText($Path)
}

function Write-TextFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Content
    )
    [System.IO.File]::WriteAllText($Path, $Content, [System.Text.UTF8Encoding]::new($false))
}

function Ensure-Directory {
    param([Parameter(Mandatory = $true)][string]$Path)
    if (-not (Test-Path -Path $Path -PathType Container)) {
        New-Item -Path $Path -ItemType Directory -Force | Out-Null
    }
}

function Move-IfNeeded {
    param(
        [Parameter(Mandatory = $true)][string]$Source,
        [Parameter(Mandatory = $true)][string]$Destination,
        [Parameter(Mandatory = $true)][string]$Label
    )
    if (Test-Path -Path $Destination -PathType Leaf) {
        Write-Host ('Already moved {0}: {1}' -f $Label, $Destination)
        return
    }
    if (-not (Test-Path -Path $Source -PathType Leaf)) {
        throw ('Neither destination nor source exists for {0}. Destination: {1}. Source: {2}' -f $Label, $Destination, $Source)
    }
    Ensure-Directory -Path (Split-Path -Path $Destination -Parent)
    Move-Item -Path $Source -Destination $Destination
    Write-Host ('Moved {0}: {1}' -f $Label, $Destination)
}

function Replace-ImportSourceBySuffix {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$SourceSuffix,
        [Parameter(Mandatory = $true)][string]$NewSource,
        [Parameter(Mandatory = $true)][string]$Label
    )
    $content = Read-TextFile -Path $Path
    $escapedSuffix = [Regex]::Escape($SourceSuffix)
    $pattern = '(?m)^(\s*import\s+[^;]+\s+from\s+[''\"])([^''\"]*' + $escapedSuffix + ')([''\"]\s*;?\s*)$'
    $replacement = '$1' + $NewSource + '$3'
    $updated = [Regex]::Replace($content, $pattern, $replacement)
    if ($updated -ne $content) {
        Write-TextFile -Path $Path -Content $updated
        Write-Host ('Updated {0}: {1}' -f $Label, $Path)
        return
    }
    if ($content -match ('(?m)^\s*import\s+[^;]+\s+from\s+[''\"]' + [Regex]::Escape($NewSource) + '[''\"]\s*;?\s*$')) {
        Write-Host ('Already updated {0}: {1}' -f $Label, $Path)
        return
    }
    Write-Host ('No matching import found for {0}; leaving file unchanged: {1}' -f $Label, $Path)
}

function Replace-AppImport {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Symbol,
        [Parameter(Mandatory = $true)][string]$NewSource,
        [Parameter(Mandatory = $true)][string]$Label
    )
    $content = Read-TextFile -Path $Path
    $symbolEscaped = [Regex]::Escape($Symbol)
    $pattern = '(?m)^(\s*import\s+\{\s*' + $symbolEscaped + '\s*\}\s+from\s+[''\"])([^''\"]+)([''\"]\s*;?\s*)$'
    $replacement = '$1' + $NewSource + '$3'
    $updated = [Regex]::Replace($content, $pattern, $replacement)
    if ($updated -ne $content) {
        Write-TextFile -Path $Path -Content $updated
        Write-Host ('Updated {0}: {1}' -f $Label, $Path)
        return
    }
    if ($content -match ('(?m)^\s*import\s+\{\s*' + $symbolEscaped + '\s*\}\s+from\s+[''\"]' + [Regex]::Escape($NewSource) + '[''\"]\s*;?\s*$')) {
        Write-Host ('Already updated {0}: {1}' -f $Label, $Path)
        return
    }
    Write-Host ('App.tsx does not import {0}; no App import update needed.' -f $Symbol)
}

$repoRoot = Get-RepoRoot
$adminSrc = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src')
$featureRoot = [System.IO.Path]::Combine($adminSrc, 'features', 'connectors', 'configuration')
$pagePath = [System.IO.Path]::Combine($featureRoot, 'pages', 'ConnectorConfiguration.tsx')
$apiPath = [System.IO.Path]::Combine($featureRoot, 'api', 'connectorConfigurationApi.ts')
$typePath = [System.IO.Path]::Combine($featureRoot, 'types', 'connectorConfiguration.ts')
$appPath = [System.IO.Path]::Combine($adminSrc, 'App.tsx')

Move-IfNeeded -Source ([System.IO.Path]::Combine($adminSrc, 'pages', 'ConnectorConfiguration.tsx')) -Destination $pagePath -Label 'Connector Configuration page'
Move-IfNeeded -Source ([System.IO.Path]::Combine($adminSrc, 'api', 'connectorConfigurationApi.ts')) -Destination $apiPath -Label 'Connector Configuration API'
Move-IfNeeded -Source ([System.IO.Path]::Combine($adminSrc, 'types', 'connectorConfiguration.ts')) -Destination $typePath -Label 'Connector Configuration types'

Replace-ImportSourceBySuffix -Path $pagePath -SourceSuffix 'connectorConfigurationApi' -NewSource '../api/connectorConfigurationApi' -Label 'Connector Configuration page API import'
Replace-ImportSourceBySuffix -Path $pagePath -SourceSuffix 'connectorConfiguration' -NewSource '../types/connectorConfiguration' -Label 'Connector Configuration page types import'
Replace-ImportSourceBySuffix -Path $pagePath -SourceSuffix 'components/Card' -NewSource '../../../../components/Card' -Label 'Connector Configuration page Card import'
Replace-ImportSourceBySuffix -Path $pagePath -SourceSuffix 'components/LoadingError' -NewSource '../../../../components/LoadingError' -Label 'Connector Configuration page LoadingError import'
Replace-ImportSourceBySuffix -Path $apiPath -SourceSuffix 'adminApiClient' -NewSource '../../../../api/core/adminApiClient' -Label 'Connector Configuration API client import'
Replace-ImportSourceBySuffix -Path $apiPath -SourceSuffix 'connectorConfiguration' -NewSource '../types/connectorConfiguration' -Label 'Connector Configuration API types import'
Replace-AppImport -Path $appPath -Symbol 'ConnectorConfiguration' -NewSource './features/connectors/configuration/pages/ConnectorConfiguration' -Label 'App.tsx ConnectorConfiguration import'

Write-Host 'P10.2AO Repair3 Admin Web Connector Configuration feature move repair applied.'
