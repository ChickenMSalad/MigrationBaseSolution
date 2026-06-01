Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    $current = $PSScriptRoot
    while ($null -ne $current -and $current.Length -gt 0) {
        $solutionPath = [System.IO.Path]::Combine($current, 'MigrationBaseSolution.sln')
        if (Test-Path -Path $solutionPath -PathType Leaf) {
            return $current
        }

        $parent = [System.IO.Directory]::GetParent($current)
        if ($null -eq $parent) {
            break
        }

        $current = $parent.FullName
    }

    throw 'Could not locate repository root containing MigrationBaseSolution.sln.'
}

function Join-RepoPath {
    param(
        [Parameter(Mandatory = $true)] [string] $Root,
        [Parameter(Mandatory = $true)] [string[]] $Segments
    )

    $combined = $Root
    foreach ($segment in $Segments) {
        $combined = [System.IO.Path]::Combine($combined, $segment)
    }

    return $combined
}

function Read-TextFile {
    param([Parameter(Mandatory = $true)] [string] $Path)

    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw "Required file was not found: $Path"
    }

    return Get-Content -Path $Path -Raw
}

function Write-TextFileNoBom {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $Content
    )

    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $Content, $utf8NoBom)
}

function Move-FeatureFile {
    param(
        [Parameter(Mandatory = $true)] [string] $SourcePath,
        [Parameter(Mandatory = $true)] [string] $DestinationPath
    )

    if (-not (Test-Path -Path $SourcePath -PathType Leaf)) {
        throw "Source file was not found: $SourcePath"
    }

    $destinationDirectory = [System.IO.Path]::GetDirectoryName($DestinationPath)
    if (-not (Test-Path -Path $destinationDirectory -PathType Container)) {
        New-Item -Path $destinationDirectory -ItemType Directory -Force | Out-Null
    }

    if (Test-Path -Path $DestinationPath -PathType Leaf) {
        $sourceContent = Read-TextFile -Path $SourcePath
        $destinationContent = Read-TextFile -Path $DestinationPath
        if ($sourceContent -ne $destinationContent) {
            throw "Destination already exists with different content: $DestinationPath"
        }

        Remove-Item -Path $SourcePath -Force
        return
    }

    Move-Item -Path $SourcePath -Destination $DestinationPath
}

$repoRoot = Get-RepositoryRoot
$adminWebSrc = Join-RepoPath -Root $repoRoot -Segments @('src', 'Admin', 'Migration.Admin.Web', 'src')
$appPath = Join-RepoPath -Root $adminWebSrc -Segments @('App.tsx')
$featureRoot = Join-RepoPath -Root $adminWebSrc -Segments @('features', 'operations', 'runtimeDashboard')
$featurePages = Join-RepoPath -Root $featureRoot -Segments @('pages')

$plannedMoves = @(
    [pscustomobject]@{
        Source = Join-RepoPath -Root $adminWebSrc -Segments @('pages', 'RuntimeDashboard.tsx')
        Destination = Join-RepoPath -Root $featurePages -Segments @('RuntimeDashboard.tsx')
    },
    [pscustomobject]@{
        Source = Join-RepoPath -Root $adminWebSrc -Segments @('pages', 'RuntimeRunDetail.tsx')
        Destination = Join-RepoPath -Root $featurePages -Segments @('RuntimeRunDetail.tsx')
    }
)

foreach ($move in $plannedMoves) {
    Move-FeatureFile -SourcePath $move.Source -DestinationPath $move.Destination
}

$appContent = Read-TextFile -Path $appPath
$replacementPairs = @(
    [pscustomobject]@{
        Old = 'import { RuntimeDashboard } from "./pages/RuntimeDashboard";'
        New = 'import { RuntimeDashboard } from "./features/operations/runtimeDashboard/pages/RuntimeDashboard";'
    },
    [pscustomobject]@{
        Old = 'import { RuntimeRunDetail } from "./pages/RuntimeRunDetail";'
        New = 'import { RuntimeRunDetail } from "./features/operations/runtimeDashboard/pages/RuntimeRunDetail";'
    }
)

foreach ($pair in $replacementPairs) {
    if ($appContent.Contains($pair.New)) {
        continue
    }

    if (-not $appContent.Contains($pair.Old)) {
        throw "Expected import was not found in App.tsx: $($pair.Old)"
    }

    $appContent = $appContent.Replace($pair.Old, $pair.New)
}

Write-TextFileNoBom -Path $appPath -Content $appContent

$readmePath = Join-RepoPath -Root $featureRoot -Segments @('README.md')
$readmeContent = @'
# Runtime Dashboard Feature

Canonical Admin Web feature home for runtime dashboard pages.

Moved during P10.2AB from the flat `src/pages` area as part of the Admin UI consolidation effort. The deployable UI remains `src/Admin/Migration.Admin.Web`; `/apps/migration-admin-ui` remains reference-only until it is retired.
'@
Write-TextFileNoBom -Path $readmePath -Content $readmeContent

Write-Host 'P10.2AB applied successfully.'
