param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $scriptRoot = $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
        $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    }

    $current = Resolve-Path -Path $scriptRoot
    while ($null -ne $current) {
        $candidate = Join-Path -Path $current.Path -ChildPath 'MigrationBaseSolution.sln'
        if (Test-Path -Path $candidate -PathType Leaf) {
            return $current.Path
        }

        $parent = Split-Path -Parent $current.Path
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $current.Path) {
            break
        }

        $current = Resolve-Path -Path $parent
    }

    throw 'Unable to locate repo root containing MigrationBaseSolution.sln.'
}

function Join-RepoPath {
    param(
        [Parameter(Mandatory = $true)][string] $Root,
        [Parameter(Mandatory = $true)][string[]] $Segments
    )

    $result = $Root
    foreach ($segment in $Segments) {
        $result = [System.IO.Path]::Combine($result, $segment)
    }

    return $result
}

function Ensure-Directory {
    param([Parameter(Mandatory = $true)][string] $Path)

    if (-not (Test-Path -Path $Path -PathType Container)) {
        New-Item -Path $Path -ItemType Directory -Force | Out-Null
    }
}

function Move-RequiredFile {
    param(
        [Parameter(Mandatory = $true)][string] $Source,
        [Parameter(Mandatory = $true)][string] $Target,
        [Parameter(Mandatory = $true)][string] $Label
    )

    if (Test-Path -Path $Target -PathType Leaf) {
        Write-Host ('Already moved {0}: {1}' -f $Label, $Target)
        return
    }

    if (-not (Test-Path -Path $Source -PathType Leaf)) {
        throw ('Required source file was not found for {0}: {1}' -f $Label, $Source)
    }

    Ensure-Directory -Path (Split-Path -Parent $Target)
    Move-Item -Path $Source -Destination $Target
    Write-Host ('Moved {0}: {1}' -f $Label, $Target)
}

function Replace-TextInFile {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Pattern,
        [Parameter(Mandatory = $true)][string] $Replacement,
        [Parameter(Mandatory = $true)][string] $Label
    )

    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Required file was not found for {0}: {1}' -f $Label, $Path)
    }

    $content = Get-Content -Path $Path -Raw
    $updated = [System.Text.RegularExpressions.Regex]::Replace($content, $Pattern, $Replacement)
    if ($updated -eq $content) {
        if ($content -like ('*' + $Replacement + '*')) {
            Write-Host ('Already updated {0}: {1}' -f $Label, $Path)
            return
        }

        throw ('Unable to update {0}; expected text was not found in {1}' -f $Label, $Path)
    }

    Set-Content -Path $Path -Value $updated -NoNewline
    Write-Host ('Updated {0}: {1}' -f $Label, $Path)
}

$repoRoot = Get-RepoRoot
$adminSrc = Join-RepoPath -Root $repoRoot -Segments @('src', 'Admin', 'Migration.Admin.Web', 'src')
$featureRoot = Join-RepoPath -Root $adminSrc -Segments @('features', 'operations', 'executionProfiles')

$moves = @(
    [pscustomobject]@{
        Label = 'Execution Profiles page'
        Source = Join-RepoPath -Root $adminSrc -Segments @('pages', 'ExecutionProfiles.tsx')
        Target = Join-RepoPath -Root $featureRoot -Segments @('pages', 'ExecutionProfiles.tsx')
    },
    [pscustomobject]@{
        Label = 'Execution Profiles API'
        Source = Join-RepoPath -Root $adminSrc -Segments @('api', 'executionProfilesApi.ts')
        Target = Join-RepoPath -Root $featureRoot -Segments @('api', 'executionProfilesApi.ts')
    },
    [pscustomobject]@{
        Label = 'Execution Profiles types'
        Source = Join-RepoPath -Root $adminSrc -Segments @('types', 'executionProfiles.ts')
        Target = Join-RepoPath -Root $featureRoot -Segments @('types', 'executionProfiles.ts')
    }
)

foreach ($move in $moves) {
    if (-not (Test-Path -Path $move.Target -PathType Leaf) -and -not (Test-Path -Path $move.Source -PathType Leaf)) {
        throw ('Cannot continue; neither source nor target exists for {0}. Source: {1}. Target: {2}' -f $move.Label, $move.Source, $move.Target)
    }
}

foreach ($move in $moves) {
    Move-RequiredFile -Source $move.Source -Target $move.Target -Label $move.Label
}

$pagePath = Join-RepoPath -Root $featureRoot -Segments @('pages', 'ExecutionProfiles.tsx')
$apiPath = Join-RepoPath -Root $featureRoot -Segments @('api', 'executionProfilesApi.ts')
$appPath = Join-RepoPath -Root $adminSrc -Segments @('App.tsx')

Replace-TextInFile -Path $pagePath -Pattern '\.\./components/Card' -Replacement '../../../../components/Card' -Label 'Execution Profiles page Card import'
Replace-TextInFile -Path $pagePath -Pattern '\.\./components/LoadingError' -Replacement '../../../../components/LoadingError' -Label 'Execution Profiles page LoadingError import'
Replace-TextInFile -Path $apiPath -Pattern '\./core/adminApiClient' -Replacement '../../../../api/core/adminApiClient' -Label 'Execution Profiles API core client import'
Replace-TextInFile -Path $appPath -Pattern '\.\./pages/ExecutionProfiles' -Replacement './features/operations/executionProfiles/pages/ExecutionProfiles' -Label 'App.tsx Execution Profiles import'

Write-Host 'P10.2AK Admin Web Execution Profiles feature move completed.'
