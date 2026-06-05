Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $scriptRoot = $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
        $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    }

    $current = Resolve-Path -LiteralPath $scriptRoot
    while ($null -ne $current) {
        $candidate = Join-Path -Path $current.Path -ChildPath 'src'
        $gitCandidate = Join-Path -Path $current.Path -ChildPath '.git'
        if ((Test-Path -LiteralPath $candidate -PathType Container) -and (Test-Path -LiteralPath $gitCandidate -PathType Container)) {
            return $current.Path
        }

        $parent = Split-Path -Parent $current.Path
        if ([string]::IsNullOrWhiteSpace($parent) -or ($parent -eq $current.Path)) {
            break
        }
        $current = Resolve-Path -LiteralPath $parent
    }

    throw 'Unable to locate repository root from script path.'
}

function Ensure-Directory {
    param([Parameter(Mandatory=$true)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
    }
}

function Move-FileIfNeeded {
    param(
        [Parameter(Mandatory=$true)][string]$Label,
        [Parameter(Mandatory=$true)][string]$Source,
        [Parameter(Mandatory=$true)][string]$Destination
    )

    if (Test-Path -LiteralPath $Destination -PathType Leaf) {
        Write-Host ('Already moved {0}: {1}' -f $Label, $Destination)
        return
    }

    if (-not (Test-Path -LiteralPath $Source -PathType Leaf)) {
        throw ('Missing {0}. Neither destination nor source exists. Destination: {1} Source: {2}' -f $Label, $Destination, $Source)
    }

    Ensure-Directory -Path (Split-Path -Parent $Destination)
    Move-Item -LiteralPath $Source -Destination $Destination
    Write-Host ('Moved {0}: {1}' -f $Label, $Destination)
}

function Normalize-ImportSource {
    param(
        [Parameter(Mandatory=$true)][string]$Label,
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$ModuleSuffixPattern,
        [Parameter(Mandatory=$true)][string]$ReplacementSource
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw ('File not found for {0}: {1}' -f $Label, $Path)
    }

    $content = Get-Content -LiteralPath $Path -Raw
    $pattern = '(from\s+["''])([^"'']*' + $ModuleSuffixPattern + ')(["''])'
    $replacement = '$1' + $ReplacementSource + '$3'
    $updated = [System.Text.RegularExpressions.Regex]::Replace($content, $pattern, $replacement)

    if ($updated -ne $content) {
        Set-Content -LiteralPath $Path -Value $updated -NoNewline
        Write-Host ('Normalized {0}: {1}' -f $Label, $Path)
    }
    else {
        Write-Host ('No import normalization needed for {0}: {1}' -f $Label, $Path)
    }
}

function Normalize-AppImport {
    param(
        [Parameter(Mandatory=$true)][string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw ('App.tsx was not found: {0}' -f $Path)
    }

    $content = Get-Content -LiteralPath $Path -Raw
    $target = './features/connectors/configuration/pages/ConnectorConfiguration'
    $importPattern = '(import\s+\{\s*ConnectorConfiguration\s*\}\s+from\s+["''])([^"'']+)(["''];?)'

    if ([System.Text.RegularExpressions.Regex]::IsMatch($content, $importPattern)) {
        $updated = [System.Text.RegularExpressions.Regex]::Replace($content, $importPattern, ('$1' + $target + '$3'))
        if ($updated -ne $content) {
            Set-Content -LiteralPath $Path -Value $updated -NoNewline
            Write-Host ('Normalized App.tsx ConnectorConfiguration import: {0}' -f $Path)
        }
        else {
            Write-Host ('App.tsx ConnectorConfiguration import already normalized: {0}' -f $Path)
        }
        return
    }

    if ($content -match '<ConnectorConfiguration\s*/>') {
        $anchorPattern = '(import\s+\{\s*Connectors\s*\}\s+from\s+["''][^"'']+["''];?)'
        if (-not [System.Text.RegularExpressions.Regex]::IsMatch($content, $anchorPattern)) {
            throw 'App.tsx uses ConnectorConfiguration but no safe import insertion anchor was found.'
        }

        $lineEnding = "`r`n"
        if ($content -notmatch "`r`n") { $lineEnding = "`n" }
        $newImport = 'import { ConnectorConfiguration } from "' + $target + '";'
        $updatedContent = [System.Text.RegularExpressions.Regex]::Replace($content, $anchorPattern, ('$1' + $lineEnding + $newImport), 1)
        Set-Content -LiteralPath $Path -Value $updatedContent -NoNewline
        Write-Host ('Inserted App.tsx ConnectorConfiguration import: {0}' -f $Path)
        return
    }

    Write-Host 'App.tsx does not reference ConnectorConfiguration; no App import change required.'
}

$repoRoot = Get-RepoRoot
$adminSrc = Join-Path -Path $repoRoot -ChildPath (Join-Path -Path 'src' -ChildPath (Join-Path -Path 'Admin' -ChildPath (Join-Path -Path 'Migration.Admin.Web' -ChildPath 'src')))
$featureRoot = Join-Path -Path $adminSrc -ChildPath (Join-Path -Path 'features' -ChildPath (Join-Path -Path 'connectors' -ChildPath 'configuration'))

$pageSource = Join-Path -Path $adminSrc -ChildPath (Join-Path -Path 'pages' -ChildPath 'ConnectorConfiguration.tsx')
$apiSource = Join-Path -Path $adminSrc -ChildPath (Join-Path -Path 'api' -ChildPath 'connectorConfigurationApi.ts')
$typesSource = Join-Path -Path $adminSrc -ChildPath (Join-Path -Path 'types' -ChildPath 'connectorConfiguration.ts')

$pageDestination = Join-Path -Path $featureRoot -ChildPath (Join-Path -Path 'pages' -ChildPath 'ConnectorConfiguration.tsx')
$apiDestination = Join-Path -Path $featureRoot -ChildPath (Join-Path -Path 'api' -ChildPath 'connectorConfigurationApi.ts')
$typesDestination = Join-Path -Path $featureRoot -ChildPath (Join-Path -Path 'types' -ChildPath 'connectorConfiguration.ts')
$appPath = Join-Path -Path $adminSrc -ChildPath 'App.tsx'

Move-FileIfNeeded -Label 'Connector Configuration page' -Source $pageSource -Destination $pageDestination
Move-FileIfNeeded -Label 'Connector Configuration API' -Source $apiSource -Destination $apiDestination
Move-FileIfNeeded -Label 'Connector Configuration types' -Source $typesSource -Destination $typesDestination

Normalize-ImportSource -Label 'Connector Configuration page API import' -Path $pageDestination -ModuleSuffixPattern 'connectorConfigurationApi' -ReplacementSource '../api/connectorConfigurationApi'
Normalize-ImportSource -Label 'Connector Configuration page types import' -Path $pageDestination -ModuleSuffixPattern 'connectorConfiguration' -ReplacementSource '../types/connectorConfiguration'
Normalize-ImportSource -Label 'Connector Configuration page Card import' -Path $pageDestination -ModuleSuffixPattern 'components/Card' -ReplacementSource '../../../../components/Card'
Normalize-ImportSource -Label 'Connector Configuration page LoadingError import' -Path $pageDestination -ModuleSuffixPattern 'components/LoadingError' -ReplacementSource '../../../../components/LoadingError'
Normalize-ImportSource -Label 'Connector Configuration API types import' -Path $apiDestination -ModuleSuffixPattern 'connectorConfiguration' -ReplacementSource '../types/connectorConfiguration'
Normalize-AppImport -Path $appPath

Write-Host 'P10.2AO Repair4 Admin Web Connector Configuration feature move applied.'
