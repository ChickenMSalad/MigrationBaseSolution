Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $current = $PSScriptRoot
    while ($null -ne $current -and $current.Length -gt 0) {
        $candidate = Join-Path -Path $current -ChildPath 'src'
        if (Test-Path -Path $candidate -PathType Container) {
            $adminWeb = Join-Path -Path $current -ChildPath 'src/Admin/Migration.Admin.Web'
            if (Test-Path -Path $adminWeb -PathType Container) {
                return $current
            }
        }
        $parent = Split-Path -Path $current -Parent
        if ($parent -eq $current) { break }
        $current = $parent
    }
    throw 'Could not locate repository root containing src/Admin/Migration.Admin.Web.'
}

function Ensure-Directory {
    param([Parameter(Mandatory = $true)][string] $Path)
    if (-not (Test-Path -Path $Path -PathType Container)) {
        New-Item -Path $Path -ItemType Directory -Force | Out-Null
    }
}

function Get-TextFile {
    param([Parameter(Mandatory = $true)][string] $Path)
    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Required file was not found: {0}' -f $Path)
    }
    return [System.IO.File]::ReadAllText($Path)
}

function Set-TextFile {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Content
    )
    [System.IO.File]::WriteAllText($Path, $Content, [System.Text.UTF8Encoding]::new($false))
}

function Move-FeatureFile {
    param(
        [Parameter(Mandatory = $true)][string] $Source,
        [Parameter(Mandatory = $true)][string] $Destination,
        [Parameter(Mandatory = $true)][string] $Name
    )

    $destinationDirectory = Split-Path -Path $Destination -Parent
    Ensure-Directory -Path $destinationDirectory

    if (Test-Path -Path $Source -PathType Leaf) {
        if (Test-Path -Path $Destination -PathType Leaf) {
            throw ('Both source and destination exist for {0}. Resolve manually before applying this set. Source={1} Destination={2}' -f $Name, $Source, $Destination)
        }
        Move-Item -Path $Source -Destination $Destination
        Write-Host ('Moved {0}' -f $Name)
        return
    }

    if (Test-Path -Path $Destination -PathType Leaf) {
        Write-Host ('Already moved {0}' -f $Name)
        return
    }

    throw ('Neither source nor destination exists for {0}. Source={1} Destination={2}' -f $Name, $Source, $Destination)
}

function Replace-InFile {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $OldText,
        [Parameter(Mandatory = $true)][string] $NewText,
        [Parameter(Mandatory = $true)][string] $Label
    )

    $content = Get-TextFile -Path $Path
    if ($content.Contains($NewText)) {
        Write-Host ('Already updated {0}' -f $Label)
        return
    }
    if (-not $content.Contains($OldText)) {
        throw ('Could not find expected text for {0} in {1}' -f $Label, $Path)
    }
    $content = $content.Replace($OldText, $NewText)
    Set-TextFile -Path $Path -Content $content
    Write-Host ('Updated {0}' -f $Label)
}

$repoRoot = Get-RepoRoot
$adminSrc = Join-Path -Path $repoRoot -ChildPath 'src/Admin/Migration.Admin.Web/src'

$appPath = Join-Path -Path $adminSrc -ChildPath 'App.tsx'
$pageSource = Join-Path -Path $adminSrc -ChildPath 'pages/CommandCenter.tsx'
$apiSource = Join-Path -Path $adminSrc -ChildPath 'api/commandCenterApi.ts'
$typeSource = Join-Path -Path $adminSrc -ChildPath 'types/commandCenter.ts'

$featureRoot = Join-Path -Path $adminSrc -ChildPath 'features/operations/commandCenter'
$pageDestination = Join-Path -Path $featureRoot -ChildPath 'pages/CommandCenter.tsx'
$apiDestination = Join-Path -Path $featureRoot -ChildPath 'api/commandCenterApi.ts'
$typeDestination = Join-Path -Path $featureRoot -ChildPath 'types/commandCenter.ts'

$items = @(
    [pscustomobject]@{ Name = 'Command Center page'; Source = $pageSource; Destination = $pageDestination },
    [pscustomobject]@{ Name = 'Command Center API'; Source = $apiSource; Destination = $apiDestination },
    [pscustomobject]@{ Name = 'Command Center types'; Source = $typeSource; Destination = $typeDestination }
)

if (-not (Test-Path -Path $appPath -PathType Leaf)) {
    throw ('App.tsx was not found: {0}' -f $appPath)
}

foreach ($item in $items) {
    $sourceExists = Test-Path -Path $item.Source -PathType Leaf
    $destinationExists = Test-Path -Path $item.Destination -PathType Leaf
    if ($sourceExists -and $destinationExists) {
        throw ('Both source and destination exist for {0}. Source={1} Destination={2}' -f $item.Name, $item.Source, $item.Destination)
    }
    if (-not $sourceExists -and -not $destinationExists) {
        throw ('Required source or destination file was not found for {0}. Source={1} Destination={2}' -f $item.Name, $item.Source, $item.Destination)
    }
}

foreach ($item in $items) {
    Move-FeatureFile -Source $item.Source -Destination $item.Destination -Name $item.Name
}

Replace-InFile -Path $appPath -OldText 'import { CommandCenter } from "./pages/CommandCenter";' -NewText 'import { CommandCenter } from "./features/operations/commandCenter/pages/CommandCenter";' -Label 'App Command Center import'

Replace-InFile -Path $pageDestination -OldText "from '../api/commandCenterApi'" -NewText "from '../api/commandCenterApi'" -Label 'Command Center page API import'
Replace-InFile -Path $pageDestination -OldText "from '../types/commandCenter'" -NewText "from '../types/commandCenter'" -Label 'Command Center page type import'
Replace-InFile -Path $apiDestination -OldText "from './core/adminApiClient'" -NewText "from '../../../../api/core/adminApiClient'" -Label 'Command Center API core import'
Replace-InFile -Path $apiDestination -OldText "from '../types/commandCenter'" -NewText "from '../types/commandCenter'" -Label 'Command Center API type import'

Write-Host 'P10.2AI Command Center feature move applied.'
