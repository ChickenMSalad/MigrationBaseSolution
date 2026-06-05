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

function Assert-FileExists {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Label
    )

    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Missing expected file for {0}: {1}' -f $Label, $Path)
    }
}

function Assert-FileMissing {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Label
    )

    if (Test-Path -Path $Path -PathType Leaf) {
        throw ('Unexpected old flat file still exists for {0}: {1}' -f $Label, $Path)
    }
}

function Assert-Contains {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Text,
        [Parameter(Mandatory = $true)][string] $Label
    )

    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Cannot inspect missing file for {0}: {1}' -f $Label, $Path)
    }

    $content = Get-Content -Path $Path -Raw
    if (-not $content.Contains($Text)) {
        throw ('Expected text was not found for {0} in {1}' -f $Label, $Path)
    }
}

function Assert-DoesNotContainRegex {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Pattern,
        [Parameter(Mandatory = $true)][string] $Label
    )

    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Cannot inspect missing file for {0}: {1}' -f $Label, $Path)
    }

    $content = Get-Content -Path $Path -Raw
    if ($content -match $Pattern) {
        throw ('Unsafe script pattern found for {0} in {1}' -f $Label, $Path)
    }
}

$repoRoot = Get-RepoRoot
$adminSrc = Join-RepoPath -Root $repoRoot -Segments @('src', 'Admin', 'Migration.Admin.Web', 'src')
$featureRoot = Join-RepoPath -Root $adminSrc -Segments @('features', 'operations', 'executionProfiles')

$pagePath = Join-RepoPath -Root $featureRoot -Segments @('pages', 'ExecutionProfiles.tsx')
$apiPath = Join-RepoPath -Root $featureRoot -Segments @('api', 'executionProfilesApi.ts')
$typesPath = Join-RepoPath -Root $featureRoot -Segments @('types', 'executionProfiles.ts')
$appPath = Join-RepoPath -Root $adminSrc -Segments @('App.tsx')
$applyPath = Join-RepoPath -Root $repoRoot -Segments @('tools', 'p10', 'P10.2AK', 'Apply-P10.2AK-AdminWebExecutionProfilesFeatureMove.ps1')

Assert-FileExists -Path $pagePath -Label 'Execution Profiles page'
Assert-FileExists -Path $apiPath -Label 'Execution Profiles API'
Assert-FileExists -Path $typesPath -Label 'Execution Profiles types'

Assert-FileMissing -Path (Join-RepoPath -Root $adminSrc -Segments @('pages', 'ExecutionProfiles.tsx')) -Label 'Execution Profiles page'
Assert-FileMissing -Path (Join-RepoPath -Root $adminSrc -Segments @('api', 'executionProfilesApi.ts')) -Label 'Execution Profiles API'
Assert-FileMissing -Path (Join-RepoPath -Root $adminSrc -Segments @('types', 'executionProfiles.ts')) -Label 'Execution Profiles types'

Assert-Contains -Path $pagePath -Text '../../../../components/Card' -Label 'Execution Profiles page Card import'
Assert-Contains -Path $pagePath -Text '../../../../components/LoadingError' -Label 'Execution Profiles page LoadingError import'
Assert-Contains -Path $pagePath -Text '../api/executionProfilesApi' -Label 'Execution Profiles page API import'
Assert-Contains -Path $pagePath -Text '../types/executionProfiles' -Label 'Execution Profiles page type import'
Assert-Contains -Path $apiPath -Text '../../../../api/core/adminApiClient' -Label 'Execution Profiles API core import'
Assert-Contains -Path $apiPath -Text '../types/executionProfiles' -Label 'Execution Profiles API type import'
Assert-Contains -Path $appPath -Text './features/operations/executionProfiles/pages/ExecutionProfiles' -Label 'App.tsx Execution Profiles import'

Assert-DoesNotContainRegex -Path $applyPath -Pattern '\$[A-Za-z_][A-Za-z0-9_]*:' -Label 'colon variable interpolation'
Assert-DoesNotContainRegex -Path $applyPath -Pattern '@\(\s*@\(' -Label 'nested array literal'
Assert-DoesNotContainRegex -Path $applyPath -Pattern 'src\s+ypes|src\s+pi' -Label 'corrupted path escape sequence'

Write-Host 'P10.2AK Admin Web Execution Profiles feature move validation passed.'
