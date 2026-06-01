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

function Assert-Leaf {
    param([Parameter(Mandatory = $true)][string] $Path)
    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Expected file was not found: {0}' -f $Path)
    }
}

function Assert-MissingLeaf {
    param([Parameter(Mandatory = $true)][string] $Path)
    if (Test-Path -Path $Path -PathType Leaf) {
        throw ('File should have been moved but still exists: {0}' -f $Path)
    }
}

function Assert-Contains {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Expected,
        [Parameter(Mandatory = $true)][string] $Label
    )
    Assert-Leaf -Path $Path
    $content = [System.IO.File]::ReadAllText($Path)
    if (-not $content.Contains($Expected)) {
        throw ('Expected text was not found for {0} in {1}' -f $Label, $Path)
    }
}

function Assert-NotContains {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Unexpected,
        [Parameter(Mandatory = $true)][string] $Label
    )
    Assert-Leaf -Path $Path
    $content = [System.IO.File]::ReadAllText($Path)
    if ($content.Contains($Unexpected)) {
        throw ('Unexpected text was found for {0} in {1}' -f $Label, $Path)
    }
}

function Assert-NoPowerShellInterpolationHazard {
    param([Parameter(Mandatory = $true)][string] $Path)
    Assert-Leaf -Path $Path
    $pattern = [string]::Concat('\', '$', '[A-Za-z_][A-Za-z0-9_]*:')
    $lines = [System.IO.File]::ReadAllLines($Path)
    for ($index = 0; $index -lt $lines.Length; $index++) {
        if ($lines[$index] -match $pattern) {
            throw ('PowerShell variable-colon interpolation hazard in {0} at line {1}' -f $Path, ($index + 1))
        }
    }
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

Assert-MissingLeaf -Path $pageSource
Assert-MissingLeaf -Path $apiSource
Assert-MissingLeaf -Path $typeSource

Assert-Leaf -Path $pageDestination
Assert-Leaf -Path $apiDestination
Assert-Leaf -Path $typeDestination

Assert-Contains -Path $appPath -Expected 'import { CommandCenter } from "./features/operations/commandCenter/pages/CommandCenter";' -Label 'App Command Center feature import'
Assert-NotContains -Path $appPath -Unexpected 'import { CommandCenter } from "./pages/CommandCenter";' -Label 'App old Command Center import'

Assert-Contains -Path $pageDestination -Expected "from '../api/commandCenterApi'" -Label 'Command Center page API import'
Assert-Contains -Path $pageDestination -Expected "from '../types/commandCenter'" -Label 'Command Center page type import'
Assert-Contains -Path $apiDestination -Expected "from '../../../../api/core/adminApiClient'" -Label 'Command Center API core import'
Assert-Contains -Path $apiDestination -Expected "from '../types/commandCenter'" -Label 'Command Center API type import'

$applyScript = Join-Path -Path $repoRoot -ChildPath 'tools/p10/P10.2AI/Apply-P10.2AI-AdminWebCommandCenterFeatureMove.ps1'
Assert-NoPowerShellInterpolationHazard -Path $applyScript

Write-Host 'P10.2AI Command Center feature move validation passed.'
