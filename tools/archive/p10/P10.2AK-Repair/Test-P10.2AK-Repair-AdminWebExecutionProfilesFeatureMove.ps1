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

function Assert-LeafExists {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Label
    )

    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Missing {0}: {1}' -f $Label, $Path)
    }
}

function Assert-LeafAbsent {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Label
    )

    if (Test-Path -Path $Path -PathType Leaf) {
        throw ('Legacy {0} should not remain: {1}' -f $Label, $Path)
    }
}

function Assert-ContainsText {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Text,
        [Parameter(Mandatory = $true)][string] $Label
    )

    Assert-LeafExists -Path $Path -Label $Label
    $content = Get-Content -Path $Path -Raw
    if (-not $content.Contains($Text)) {
        throw ('Expected text not found for {0}: {1}' -f $Label, $Text)
    }
}

function Assert-NotContainsText {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Text,
        [Parameter(Mandatory = $true)][string] $Label
    )

    Assert-LeafExists -Path $Path -Label $Label
    $content = Get-Content -Path $Path -Raw
    if ($content.Contains($Text)) {
        throw ('Unexpected legacy text found for {0}: {1}' -f $Label, $Text)
    }
}

function Test-PathHasBlockedSegment {
    param([Parameter(Mandatory = $true)][string] $Path)
    $segments = $Path -split '[\\/]+'
    foreach ($segment in $segments) {
        if ($segment -eq 'bin' -or $segment -eq 'obj') {
            return $true
        }
    }
    return $false
}

$repoRoot = Get-RepoRoot
$adminSrc = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src')
$featureRoot = [System.IO.Path]::Combine($adminSrc, 'features', 'operations', 'executionProfiles')
$pagePath = [System.IO.Path]::Combine($featureRoot, 'pages', 'ExecutionProfiles.tsx')
$apiPath = [System.IO.Path]::Combine($featureRoot, 'api', 'executionProfilesApi.ts')
$typePath = [System.IO.Path]::Combine($featureRoot, 'types', 'executionProfiles.ts')
$appPath = [System.IO.Path]::Combine($adminSrc, 'App.tsx')

$legacyPagePath = [System.IO.Path]::Combine($adminSrc, 'pages', 'ExecutionProfiles.tsx')
$legacyApiPath = [System.IO.Path]::Combine($adminSrc, 'api', 'executionProfilesApi.ts')
$legacyTypePath = [System.IO.Path]::Combine($adminSrc, 'types', 'executionProfiles.ts')

Assert-LeafExists -Path $pagePath -Label 'Execution Profiles feature page'
Assert-LeafExists -Path $apiPath -Label 'Execution Profiles feature API'
Assert-LeafExists -Path $typePath -Label 'Execution Profiles feature types'
Assert-LeafAbsent -Path $legacyPagePath -Label 'Execution Profiles page'
Assert-LeafAbsent -Path $legacyApiPath -Label 'Execution Profiles API'
Assert-LeafAbsent -Path $legacyTypePath -Label 'Execution Profiles types'

Assert-ContainsText -Path $appPath -Text 'import { ExecutionProfiles } from "./features/operations/executionProfiles/pages/ExecutionProfiles";' -Label 'App.tsx feature import'
Assert-NotContainsText -Path $appPath -Text 'import { ExecutionProfiles } from "./pages/ExecutionProfiles";' -Label 'App.tsx legacy import'

Assert-ContainsText -Path $pagePath -Text '../../../../components/Card' -Label 'Execution Profiles page Card import'
Assert-ContainsText -Path $pagePath -Text '../../../../components/LoadingError' -Label 'Execution Profiles page LoadingError import'
Assert-ContainsText -Path $apiPath -Text '../../../../lib/apiClient' -Label 'Execution Profiles API core client import'

$scriptFolder = [System.IO.Path]::Combine($repoRoot, 'tools', 'p10', 'P10.2AK-Repair')
$scripts = @(Get-ChildItem -Path $scriptFolder -Filter '*.ps1' -File | Where-Object { -not (Test-PathHasBlockedSegment -Path $_.FullName) })
foreach ($script in $scripts) {
    $scriptContent = Get-Content -Path $script.FullName -Raw
    $labelColon = '$' + 'Label' + ':'
    $pathColon = '$' + 'Path' + ':'
    if ($scriptContent.Contains($labelColon) -or $scriptContent.Contains($pathColon)) {
        throw ('Unsafe colon interpolation pattern found in {0}' -f $script.FullName)
    }
}

Write-Host 'P10.2AK repair validation passed.'
