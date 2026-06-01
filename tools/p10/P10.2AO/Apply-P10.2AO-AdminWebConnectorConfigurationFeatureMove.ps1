Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $current = $PSScriptRoot
    while ($true) {
        $candidate = Join-Path -Path $current -ChildPath 'src/Admin/Migration.Admin.Web/src/App.tsx'
        if (Test-Path -Path $candidate -PathType Leaf) {
            return $current
        }

        $parent = Split-Path -Path $current -Parent
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $current) {
            throw 'Unable to locate repository root from script location.'
        }

        $current = $parent
    }
}

function Ensure-Directory {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -Path $Path -PathType Container)) {
        New-Item -Path $Path -ItemType Directory -Force | Out-Null
    }
}

function Get-Text {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Required file was not found: {0}' -f $Path)
    }

    return [System.IO.File]::ReadAllText($Path)
}

function Set-Text {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Text
    )

    [System.IO.File]::WriteAllText($Path, $Text, [System.Text.UTF8Encoding]::new($false))
}

function Assert-MoveState {
    param(
        [Parameter(Mandatory = $true)][object[]]$Moves
    )

    foreach ($move in $Moves) {
        $sourceExists = Test-Path -Path $move.Source -PathType Leaf
        $targetExists = Test-Path -Path $move.Target -PathType Leaf

        if ($sourceExists -and $targetExists) {
            throw ('Both source and target exist for {0}; refusing ambiguous move. Source={1} Target={2}' -f $move.Label, $move.Source, $move.Target)
        }

        if (-not $sourceExists -and -not $targetExists) {
            throw ('Neither source nor target exists for {0}. Source={1} Target={2}' -f $move.Label, $move.Source, $move.Target)
        }
    }
}

function Move-FeatureFile {
    param(
        [Parameter(Mandatory = $true)][string]$Label,
        [Parameter(Mandatory = $true)][string]$Source,
        [Parameter(Mandatory = $true)][string]$Target
    )

    if (Test-Path -Path $Target -PathType Leaf) {
        Write-Host ('Already moved {0}: {1}' -f $Label, $Target)
        return
    }

    $targetDirectory = Split-Path -Path $Target -Parent
    Ensure-Directory -Path $targetDirectory
    Move-Item -Path $Source -Destination $Target
    Write-Host ('Moved {0}: {1}' -f $Label, $Target)
}

function Normalize-ImportPath {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label,
        [Parameter(Mandatory = $true)][string]$ImportedName,
        [Parameter(Mandatory = $true)][string]$NewPath
    )

    $text = Get-Text -Path $Path
    $escapedName = [System.Text.RegularExpressions.Regex]::Escape($ImportedName)
    $pattern = '(?m)^\s*import\s+(.+?\b' + $escapedName + '\b.+?)\s+from\s+[''\"]([^''\"]+)[''\"]\s*;\s*$'
    $match = [System.Text.RegularExpressions.Regex]::Match($text, $pattern)

    if (-not $match.Success) {
        Write-Host ('No import normalization needed for {0}; import was not found.' -f $Label)
        return
    }

    $currentPath = $match.Groups[2].Value
    if ($currentPath -eq $NewPath) {
        Write-Host ('Already updated {0}: {1}' -f $Label, $Path)
        return
    }

    $replacement = $match.Groups[0].Value.Replace($currentPath, $NewPath)
    $updated = $text.Remove($match.Index, $match.Length).Insert($match.Index, $replacement)
    Set-Text -Path $Path -Text $updated
    Write-Host ('Updated {0}: {1}' -f $Label, $Path)
}

function Normalize-AppPageImport {
    param(
        [Parameter(Mandatory = $true)][string]$AppPath,
        [Parameter(Mandatory = $true)][string]$ImportedName,
        [Parameter(Mandatory = $true)][string]$NewPath
    )

    $text = Get-Text -Path $AppPath
    $escapedName = [System.Text.RegularExpressions.Regex]::Escape($ImportedName)
    $pattern = '(?m)^\s*import\s+\{\s*' + $escapedName + '\s*\}\s+from\s+[''\"]([^''\"]+)[''\"]\s*;\s*$'
    $match = [System.Text.RegularExpressions.Regex]::Match($text, $pattern)

    if (-not $match.Success) {
        if ($text.Contains(('<{0} ' -f $ImportedName)) -or $text.Contains(('<{0}/>' -f $ImportedName)) -or $text.Contains(('<{0} />' -f $ImportedName))) {
            throw ('App.tsx uses {0}, but its import line was not found.' -f $ImportedName)
        }

        Write-Host ('App.tsx does not import {0}; no App import update needed.' -f $ImportedName)
        return
    }

    $currentPath = $match.Groups[1].Value
    if ($currentPath -eq $NewPath) {
        Write-Host ('Already updated App.tsx {0} import: {1}' -f $ImportedName, $AppPath)
        return
    }

    $replacement = $match.Groups[0].Value.Replace($currentPath, $NewPath)
    $updated = $text.Remove($match.Index, $match.Length).Insert($match.Index, $replacement)
    Set-Text -Path $AppPath -Text $updated
    Write-Host ('Updated App.tsx {0} import: {1}' -f $ImportedName, $AppPath)
}

$repoRoot = Get-RepoRoot
$webSrc = Join-Path -Path $repoRoot -ChildPath 'src/Admin/Migration.Admin.Web/src'
$featureRoot = Join-Path -Path $webSrc -ChildPath 'features/connectors/configuration'

$pageSource = Join-Path -Path $webSrc -ChildPath 'pages/ConnectorConfiguration.tsx'
$apiSource = Join-Path -Path $webSrc -ChildPath 'api/connectorConfigurationApi.ts'
$typeSource = Join-Path -Path $webSrc -ChildPath 'types/connectorConfiguration.ts'

$pageTarget = Join-Path -Path $featureRoot -ChildPath 'pages/ConnectorConfiguration.tsx'
$apiTarget = Join-Path -Path $featureRoot -ChildPath 'api/connectorConfigurationApi.ts'
$typeTarget = Join-Path -Path $featureRoot -ChildPath 'types/connectorConfiguration.ts'
$appPath = Join-Path -Path $webSrc -ChildPath 'App.tsx'

$moves = @(
    [pscustomobject]@{ Label = 'Connector Configuration page'; Source = $pageSource; Target = $pageTarget },
    [pscustomobject]@{ Label = 'Connector Configuration API'; Source = $apiSource; Target = $apiTarget },
    [pscustomobject]@{ Label = 'Connector Configuration types'; Source = $typeSource; Target = $typeTarget }
)

Assert-MoveState -Moves $moves

Ensure-Directory -Path (Join-Path -Path $featureRoot -ChildPath 'pages')
Ensure-Directory -Path (Join-Path -Path $featureRoot -ChildPath 'api')
Ensure-Directory -Path (Join-Path -Path $featureRoot -ChildPath 'types')

foreach ($move in $moves) {
    Move-FeatureFile -Label $move.Label -Source $move.Source -Target $move.Target
}

Normalize-ImportPath -Path $pageTarget -Label 'Connector Configuration page API import' -ImportedName 'connectorConfigurationApi' -NewPath '../api/connectorConfigurationApi'
Normalize-ImportPath -Path $pageTarget -Label 'Connector Configuration page types import' -ImportedName 'ConnectorConfiguration' -NewPath '../types/connectorConfiguration'
Normalize-ImportPath -Path $pageTarget -Label 'Connector Configuration page Card import' -ImportedName 'Card' -NewPath '../../../../components/Card'
Normalize-ImportPath -Path $pageTarget -Label 'Connector Configuration page LoadingError import' -ImportedName 'LoadingError' -NewPath '../../../../components/LoadingError'
Normalize-ImportPath -Path $apiTarget -Label 'Connector Configuration API client import' -ImportedName 'adminApiClient' -NewPath '../../../../api/core/adminApiClient'
Normalize-ImportPath -Path $apiTarget -Label 'Connector Configuration API types import' -ImportedName 'ConnectorConfiguration' -NewPath '../types/connectorConfiguration'
Normalize-AppPageImport -AppPath $appPath -ImportedName 'ConnectorConfiguration' -NewPath './features/connectors/configuration/pages/ConnectorConfiguration'

Write-Host 'P10.2AO Admin Web Connector Configuration feature move applied.'
