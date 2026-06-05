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

function Assert-LeafExists {
    param([Parameter(Mandatory = $true)] [string] $Path)

    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw "Expected file does not exist: $Path"
    }
}

function Assert-LeafMissing {
    param([Parameter(Mandatory = $true)] [string] $Path)

    if (Test-Path -Path $Path -PathType Leaf) {
        throw "File should have been moved away: $Path"
    }
}

function Assert-Contains {
    param(
        [Parameter(Mandatory = $true)] [string] $Content,
        [Parameter(Mandatory = $true)] [string] $Expected
    )

    if (-not $Content.Contains($Expected)) {
        throw "Expected text was not found: $Expected"
    }
}

function Assert-DoesNotContain {
    param(
        [Parameter(Mandatory = $true)] [string] $Content,
        [Parameter(Mandatory = $true)] [string] $Unexpected
    )

    if ($Content.Contains($Unexpected)) {
        throw "Unexpected text was found: $Unexpected"
    }
}

$repoRoot = Get-RepositoryRoot
$adminWebSrc = Join-RepoPath -Root $repoRoot -Segments @('src', 'Admin', 'Migration.Admin.Web', 'src')
$appPath = Join-RepoPath -Root $adminWebSrc -Segments @('App.tsx')
$featureRoot = Join-RepoPath -Root $adminWebSrc -Segments @('features', 'operations', 'runtimeDashboard')
$featurePages = Join-RepoPath -Root $featureRoot -Segments @('pages')

$expectedFiles = @(
    [pscustomobject]@{ Path = Join-RepoPath -Root $featurePages -Segments @('RuntimeDashboard.tsx') },
    [pscustomobject]@{ Path = Join-RepoPath -Root $featurePages -Segments @('RuntimeRunDetail.tsx') },
    [pscustomobject]@{ Path = Join-RepoPath -Root $featureRoot -Segments @('README.md') }
)

foreach ($file in $expectedFiles) {
    Assert-LeafExists -Path $file.Path
}

$unexpectedFiles = @(
    [pscustomobject]@{ Path = Join-RepoPath -Root $adminWebSrc -Segments @('pages', 'RuntimeDashboard.tsx') },
    [pscustomobject]@{ Path = Join-RepoPath -Root $adminWebSrc -Segments @('pages', 'RuntimeRunDetail.tsx') }
)

foreach ($file in $unexpectedFiles) {
    Assert-LeafMissing -Path $file.Path
}

$appContent = Read-TextFile -Path $appPath
Assert-Contains -Content $appContent -Expected 'import { RuntimeDashboard } from "./features/operations/runtimeDashboard/pages/RuntimeDashboard";'
Assert-Contains -Content $appContent -Expected 'import { RuntimeRunDetail } from "./features/operations/runtimeDashboard/pages/RuntimeRunDetail";'
Assert-DoesNotContain -Content $appContent -Unexpected 'import { RuntimeDashboard } from "./pages/RuntimeDashboard";'
Assert-DoesNotContain -Content $appContent -Unexpected 'import { RuntimeRunDetail } from "./pages/RuntimeRunDetail";'

$appsReferencePath = Join-RepoPath -Root $repoRoot -Segments @('apps', 'migration-admin-ui')
if (-not (Test-Path -Path $appsReferencePath -PathType Container)) {
    Write-Host 'Reference-only apps/migration-admin-ui folder was not present in this checkout. No action required.'
}

Write-Host 'P10.2AB validation passed.'
