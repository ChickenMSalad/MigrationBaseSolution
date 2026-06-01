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

function Get-Text {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Expected file missing: {0}' -f $Path)
    }

    return [System.IO.File]::ReadAllText($Path)
}

function Assert-FileExists {
    param(
        [Parameter(Mandatory = $true)][string]$Label,
        [Parameter(Mandatory = $true)][string]$Path
    )

    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Expected {0} file missing: {1}' -f $Label, $Path)
    }
}

function Assert-FileMissing {
    param(
        [Parameter(Mandatory = $true)][string]$Label,
        [Parameter(Mandatory = $true)][string]$Path
    )

    if (Test-Path -Path $Path -PathType Leaf) {
        throw ('Unexpected legacy {0} file still exists: {1}' -f $Label, $Path)
    }
}

function Assert-ImportPath {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label,
        [Parameter(Mandatory = $true)][string]$ImportedName,
        [Parameter(Mandatory = $true)][string]$ExpectedPath
    )

    $text = Get-Text -Path $Path
    $escapedName = [System.Text.RegularExpressions.Regex]::Escape($ImportedName)
    $pattern = '(?m)^\s*import\s+(.+?\b' + $escapedName + '\b.+?)\s+from\s+[''\"]([^''\"]+)[''\"]\s*;\s*$'
    $match = [System.Text.RegularExpressions.Regex]::Match($text, $pattern)

    if (-not $match.Success) {
        throw ('Expected import missing for {0}: {1}' -f $Label, $ImportedName)
    }

    $actualPath = $match.Groups[2].Value
    if ($actualPath -ne $ExpectedPath) {
        throw ('Unexpected import path for {0}. Expected={1} Actual={2}' -f $Label, $ExpectedPath, $actualPath)
    }
}

function Assert-AppImportIfUsed {
    param(
        [Parameter(Mandatory = $true)][string]$AppPath,
        [Parameter(Mandatory = $true)][string]$ImportedName,
        [Parameter(Mandatory = $true)][string]$ExpectedPath
    )

    $text = Get-Text -Path $AppPath
    $symbolUsed = $text.Contains(('<{0} ' -f $ImportedName)) -or $text.Contains(('<{0}/>' -f $ImportedName)) -or $text.Contains(('<{0} />' -f $ImportedName))
    $escapedName = [System.Text.RegularExpressions.Regex]::Escape($ImportedName)
    $pattern = '(?m)^\s*import\s+\{\s*' + $escapedName + '\s*\}\s+from\s+[''\"]([^''\"]+)[''\"]\s*;\s*$'
    $match = [System.Text.RegularExpressions.Regex]::Match($text, $pattern)

    if (-not $symbolUsed -and -not $match.Success) {
        return
    }

    if ($symbolUsed -and -not $match.Success) {
        throw ('App.tsx uses {0}, but import is missing.' -f $ImportedName)
    }

    if ($match.Success) {
        $actualPath = $match.Groups[1].Value
        if ($actualPath -ne $ExpectedPath) {
            throw ('Unexpected App.tsx import path for {0}. Expected={1} Actual={2}' -f $ImportedName, $ExpectedPath, $actualPath)
        }
    }
}

function Assert-NoUnsafeGeneratedScriptPatterns {
    param([Parameter(Mandatory = $true)][string]$ToolRoot)

    $scripts = @(Get-ChildItem -Path $ToolRoot -Filter '*.ps1' -File -Recurse)
    foreach ($script in $scripts) {
        $text = [System.IO.File]::ReadAllText($script.FullName)
        if ($text -match '\$[A-Za-z_][A-Za-z0-9_]*:') {
            throw ('Unsafe PowerShell interpolation pattern found in {0}' -f $script.FullName)
        }
        if ($text.Contains('@(' + [Environment]::NewLine + '    @(')) {
            throw ('Nested validation array pattern found in {0}' -f $script.FullName)
        }
        if ($text.Contains('src' + [char]9 + 'ypes') -or $text.Contains('src' + [char]7 + 'pi')) {
            throw ('Corrupted escape sequence found in {0}' -f $script.FullName)
        }
    }
}

$repoRoot = Get-RepoRoot
$webSrc = Join-Path -Path $repoRoot -ChildPath 'src/Admin/Migration.Admin.Web/src'
$featureRoot = Join-Path -Path $webSrc -ChildPath 'features/connectors/configuration'

$pageTarget = Join-Path -Path $featureRoot -ChildPath 'pages/ConnectorConfiguration.tsx'
$apiTarget = Join-Path -Path $featureRoot -ChildPath 'api/connectorConfigurationApi.ts'
$typeTarget = Join-Path -Path $featureRoot -ChildPath 'types/connectorConfiguration.ts'
$appPath = Join-Path -Path $webSrc -ChildPath 'App.tsx'

Assert-FileExists -Label 'Connector Configuration page' -Path $pageTarget
Assert-FileExists -Label 'Connector Configuration API' -Path $apiTarget
Assert-FileExists -Label 'Connector Configuration types' -Path $typeTarget

Assert-FileMissing -Label 'Connector Configuration page' -Path (Join-Path -Path $webSrc -ChildPath 'pages/ConnectorConfiguration.tsx')
Assert-FileMissing -Label 'Connector Configuration API' -Path (Join-Path -Path $webSrc -ChildPath 'api/connectorConfigurationApi.ts')
Assert-FileMissing -Label 'Connector Configuration types' -Path (Join-Path -Path $webSrc -ChildPath 'types/connectorConfiguration.ts')

Assert-ImportPath -Path $pageTarget -Label 'Connector Configuration page API import' -ImportedName 'connectorConfigurationApi' -ExpectedPath '../api/connectorConfigurationApi'
Assert-ImportPath -Path $pageTarget -Label 'Connector Configuration page Card import' -ImportedName 'Card' -ExpectedPath '../../../../components/Card'
Assert-ImportPath -Path $pageTarget -Label 'Connector Configuration page LoadingError import' -ImportedName 'LoadingError' -ExpectedPath '../../../../components/LoadingError'
Assert-ImportPath -Path $apiTarget -Label 'Connector Configuration API client import' -ImportedName 'adminApiClient' -ExpectedPath '../../../../api/core/adminApiClient'
Assert-AppImportIfUsed -AppPath $appPath -ImportedName 'ConnectorConfiguration' -ExpectedPath './features/connectors/configuration/pages/ConnectorConfiguration'

Assert-NoUnsafeGeneratedScriptPatterns -ToolRoot (Join-Path -Path $repoRoot -ChildPath 'tools/p10/P10.2AO')

Write-Host 'P10.2AO Admin Web Connector Configuration feature move validation passed.'
