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

    throw 'Unable to locate repository root from current directory.'
}

function Join-RepoPath {
    param(
        [Parameter(Mandatory = $true)][string] $Root,
        [Parameter(Mandatory = $true)][string[]] $Segments
    )

    $path = $Root
    foreach ($segment in $Segments) {
        $path = [System.IO.Path]::Combine($path, $segment)
    }

    return $path
}

function Read-AllTextStrict {
    param([Parameter(Mandatory = $true)][string] $Path)
    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Required file missing: {0}' -f $Path)
    }

    return [System.IO.File]::ReadAllText($Path)
}

function Write-AllTextUtf8NoBom {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Content
    )

    $encoding = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $Content, $encoding)
}

function Ensure-FileMoved {
    param(
        [Parameter(Mandatory = $true)][string] $Source,
        [Parameter(Mandatory = $true)][string] $Destination,
        [Parameter(Mandatory = $true)][string] $Label
    )

    if (Test-Path -Path $Destination -PathType Leaf) {
        Write-Host ('Already moved {0}: {1}' -f $Label, $Destination)
        return
    }

    if (-not (Test-Path -Path $Source -PathType Leaf)) {
        throw ('Neither source nor destination exists for {0}. Source: {1}. Destination: {2}' -f $Label, $Source, $Destination)
    }

    $destinationDirectory = Split-Path -Path $Destination -Parent
    if (-not (Test-Path -Path $destinationDirectory -PathType Container)) {
        New-Item -Path $destinationDirectory -ItemType Directory -Force | Out-Null
    }

    Move-Item -Path $Source -Destination $Destination
    Write-Host ('Moved {0}: {1}' -f $Label, $Destination)
}

function Normalize-ImportSource {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $ImportedName,
        [Parameter(Mandatory = $true)][string] $NewSource,
        [Parameter(Mandatory = $true)][string] $Label
    )

    $content = Read-AllTextStrict -Path $Path
    $escapedName = [System.Text.RegularExpressions.Regex]::Escape($ImportedName)
    $pattern = '(?m)^(\s*import\s+(?:type\s+)?\{[^\r\n]*\b' + $escapedName + '\b[^\r\n]*\}\s+from\s+)["''][^"'']+(["''];?\s*)$'
    $replacement = '$1"' + $NewSource + '"$2'
    $updated = [System.Text.RegularExpressions.Regex]::Replace($content, $pattern, $replacement)

    if ($updated -eq $content) {
        Write-Host ('No import normalization needed for {0}; import was not found.' -f $Label)
        return
    }

    Write-AllTextUtf8NoBom -Path $Path -Content $updated
    Write-Host ('Updated {0}: {1}' -f $Label, $Path)
}

function Ensure-AppImport {
    param(
        [Parameter(Mandatory = $true)][string] $AppPath,
        [Parameter(Mandatory = $true)][string] $ImportedName,
        [Parameter(Mandatory = $true)][string] $NewSource
    )

    $content = Read-AllTextStrict -Path $AppPath
    $escapedName = [System.Text.RegularExpressions.Regex]::Escape($ImportedName)
    $pattern = '(?m)^(\s*import\s+\{\s*' + $escapedName + '\s*\}\s+from\s+)["''][^"'']+(["''];?\s*)$'
    $replacement = '$1"' + $NewSource + '"$2'
    $updated = [System.Text.RegularExpressions.Regex]::Replace($content, $pattern, $replacement)

    if ($updated -ne $content) {
        Write-AllTextUtf8NoBom -Path $AppPath -Content $updated
        Write-Host ('Updated App.tsx {0} import: {1}' -f $ImportedName, $AppPath)
        return
    }

    if ($content -match [System.Text.RegularExpressions.Regex]::Escape($NewSource)) {
        Write-Host ('App.tsx already references {0}: {1}' -f $ImportedName, $NewSource)
        return
    }

    if ($content -match ('<\s*' + $escapedName + '\b')) {
        throw ('App.tsx uses {0}, but no matching import was found.' -f $ImportedName)
    }

    Write-Host ('App.tsx does not use {0}; no import update required.' -f $ImportedName)
}

$repoRoot = Get-RepoRoot
$adminSrc = Join-RepoPath -Root $repoRoot -Segments @('src', 'Admin', 'Migration.Admin.Web', 'src')
$featureRoot = Join-RepoPath -Root $adminSrc -Segments @('features', 'connectors', 'configuration')

$pageSource = Join-RepoPath -Root $adminSrc -Segments @('pages', 'ConnectorConfiguration.tsx')
$apiSource = Join-RepoPath -Root $adminSrc -Segments @('api', 'connectorConfigurationApi.ts')
$typeSource = Join-RepoPath -Root $adminSrc -Segments @('types', 'connectorConfiguration.ts')
$pageDestination = Join-RepoPath -Root $featureRoot -Segments @('pages', 'ConnectorConfiguration.tsx')
$apiDestination = Join-RepoPath -Root $featureRoot -Segments @('api', 'connectorConfigurationApi.ts')
$typeDestination = Join-RepoPath -Root $featureRoot -Segments @('types', 'connectorConfiguration.ts')
$appPath = Join-RepoPath -Root $adminSrc -Segments @('App.tsx')

Ensure-FileMoved -Source $pageSource -Destination $pageDestination -Label 'Connector Configuration page'
Ensure-FileMoved -Source $apiSource -Destination $apiDestination -Label 'Connector Configuration API'
Ensure-FileMoved -Source $typeSource -Destination $typeDestination -Label 'Connector Configuration types'

Normalize-ImportSource -Path $pageDestination -ImportedName 'connectorConfigurationApi' -NewSource '../api/connectorConfigurationApi' -Label 'Connector Configuration page API import'
Normalize-ImportSource -Path $pageDestination -ImportedName 'ConnectorConfigurationSummary' -NewSource '../types/connectorConfiguration' -Label 'Connector Configuration page types import'
Normalize-ImportSource -Path $pageDestination -ImportedName 'Card' -NewSource '../../../../components/Card' -Label 'Connector Configuration page Card import'
Normalize-ImportSource -Path $pageDestination -ImportedName 'LoadingError' -NewSource '../../../../components/LoadingError' -Label 'Connector Configuration page LoadingError import'
Normalize-ImportSource -Path $apiDestination -ImportedName 'ConnectorConfigurationSummary' -NewSource '../types/connectorConfiguration' -Label 'Connector Configuration API types import'
Ensure-AppImport -AppPath $appPath -ImportedName 'ConnectorConfiguration' -NewSource './features/connectors/configuration/pages/ConnectorConfiguration'

Write-Host 'P10.2AO Repair Admin Web Connector Configuration feature move applied.'
