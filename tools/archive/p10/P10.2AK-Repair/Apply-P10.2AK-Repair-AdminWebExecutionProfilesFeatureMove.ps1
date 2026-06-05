Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $current = (Get-Location).Path
    while (-not [string]::IsNullOrWhiteSpace($current)) {
        $candidate = [System.IO.Path]::Combine($current, 'src', 'Admin', 'Migration.Admin.Web', 'src', 'App.tsx')
        if (Test-Path -Path $candidate -PathType Leaf) {
            return $current
        }

        $parent = [System.IO.Directory]::GetParent($current)
        if ($null -eq $parent) {
            break
        }

        $current = $parent.FullName
    }

    throw 'Unable to locate repository root. Run this script from inside MigrationBaseSolutionRepo.'
}

function Ensure-Directory {
    param([Parameter(Mandatory = $true)][string] $Path)
    if (-not (Test-Path -Path $Path -PathType Container)) {
        New-Item -Path $Path -ItemType Directory -Force | Out-Null
    }
}

function Move-LeafIdempotent {
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
        throw ('Required source file for {0} was not found at source or destination.' -f $Label)
    }

    $destinationDirectory = [System.IO.Path]::GetDirectoryName($Destination)
    Ensure-Directory -Path $destinationDirectory
    Move-Item -Path $Source -Destination $Destination
    Write-Host ('Moved {0}: {1}' -f $Label, $Destination)
}

function Replace-TextIfPresent {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $OldText,
        [Parameter(Mandatory = $true)][string] $NewText,
        [Parameter(Mandatory = $true)][string] $Label
    )

    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('File not found for update: {0}' -f $Path)
    }

    $content = Get-Content -Path $Path -Raw
    if ($content.Contains($NewText)) {
        Write-Host ('Already updated {0}: {1}' -f $Label, $Path)
        return
    }

    if (-not $content.Contains($OldText)) {
        Write-Host ('No update needed for {0}: expected legacy text not present.' -f $Label)
        return
    }

    $updated = $content.Replace($OldText, $NewText)
    Set-Content -Path $Path -Value $updated -NoNewline
    Write-Host ('Updated {0}: {1}' -f $Label, $Path)
}

function Ensure-AppImport {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $LegacyImport,
        [Parameter(Mandatory = $true)][string] $FeatureImport
    )

    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('App.tsx was not found: {0}' -f $Path)
    }

    $content = Get-Content -Path $Path -Raw
    if ($content.Contains($FeatureImport)) {
        Write-Host ('Already updated App.tsx Execution Profiles import: {0}' -f $Path)
        return
    }

    if ($content.Contains($LegacyImport)) {
        $updated = $content.Replace($LegacyImport, $FeatureImport)
        Set-Content -Path $Path -Value $updated -NoNewline
        Write-Host ('Updated App.tsx Execution Profiles import: {0}' -f $Path)
        return
    }

    if ($content -notmatch '\bExecutionProfiles\b') {
        throw 'App.tsx does not reference ExecutionProfiles; refusing to add an unused import.'
    }

    $anchor = 'import { CommandCenter } from "./features/operations/commandCenter/pages/CommandCenter";'
    if ($content.Contains($anchor)) {
        $replacement = $anchor + ' ' + $FeatureImport
        $updatedWithAnchor = $content.Replace($anchor, $replacement)
        Set-Content -Path $Path -Value $updatedWithAnchor -NoNewline
        Write-Host ('Inserted App.tsx Execution Profiles import after Command Center import: {0}' -f $Path)
        return
    }

    $exportMarker = 'export default function App()'
    if ($content.Contains($exportMarker)) {
        $replacementExport = $FeatureImport + ' ' + $exportMarker
        $updatedBeforeExport = $content.Replace($exportMarker, $replacementExport)
        Set-Content -Path $Path -Value $updatedBeforeExport -NoNewline
        Write-Host ('Inserted App.tsx Execution Profiles import before export: {0}' -f $Path)
        return
    }

    throw 'Unable to insert Execution Profiles import into App.tsx; no safe insertion anchor was found.'
}

$repoRoot = Get-RepoRoot
$adminSrc = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src')
$featureRoot = [System.IO.Path]::Combine($adminSrc, 'features', 'operations', 'executionProfiles')
$pageDirectory = [System.IO.Path]::Combine($featureRoot, 'pages')
$apiDirectory = [System.IO.Path]::Combine($featureRoot, 'api')
$typeDirectory = [System.IO.Path]::Combine($featureRoot, 'types')

Ensure-Directory -Path $pageDirectory
Ensure-Directory -Path $apiDirectory
Ensure-Directory -Path $typeDirectory

$pageSource = [System.IO.Path]::Combine($adminSrc, 'pages', 'ExecutionProfiles.tsx')
$pageDestination = [System.IO.Path]::Combine($pageDirectory, 'ExecutionProfiles.tsx')
$apiSource = [System.IO.Path]::Combine($adminSrc, 'api', 'executionProfilesApi.ts')
$apiDestination = [System.IO.Path]::Combine($apiDirectory, 'executionProfilesApi.ts')
$typeSource = [System.IO.Path]::Combine($adminSrc, 'types', 'executionProfiles.ts')
$typeDestination = [System.IO.Path]::Combine($typeDirectory, 'executionProfiles.ts')
$appPath = [System.IO.Path]::Combine($adminSrc, 'App.tsx')

Move-LeafIdempotent -Source $pageSource -Destination $pageDestination -Label 'Execution Profiles page'
Move-LeafIdempotent -Source $apiSource -Destination $apiDestination -Label 'Execution Profiles API'
Move-LeafIdempotent -Source $typeSource -Destination $typeDestination -Label 'Execution Profiles types'

Replace-TextIfPresent -Path $pageDestination -OldText '../components/Card' -NewText '../../../../components/Card' -Label 'Execution Profiles page Card import'
Replace-TextIfPresent -Path $pageDestination -OldText '../components/LoadingError' -NewText '../../../../components/LoadingError' -Label 'Execution Profiles page LoadingError import'
Replace-TextIfPresent -Path $apiDestination -OldText '../lib/apiClient' -NewText '../../../../lib/apiClient' -Label 'Execution Profiles API core client import'

$legacyImport = 'import { ExecutionProfiles } from "./pages/ExecutionProfiles";'
$featureImport = 'import { ExecutionProfiles } from "./features/operations/executionProfiles/pages/ExecutionProfiles";'
Ensure-AppImport -Path $appPath -LegacyImport $legacyImport -FeatureImport $featureImport

Write-Host 'P10.2AK repair apply completed.'
