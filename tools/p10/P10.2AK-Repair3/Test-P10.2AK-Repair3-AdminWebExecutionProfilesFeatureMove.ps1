Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $current = (Get-Location).Path
    while ($null -ne $current -and $current.Length -gt 0) {
        if (Test-Path -Path (Join-Path -Path $current -ChildPath 'src/Admin/Migration.Admin.Web') -PathType Container) {
            return $current
        }
        $parent = Split-Path -Path $current -Parent
        if ($parent -eq $current) { break }
        $current = $parent
    }
    throw 'Unable to locate repo root containing src/Admin/Migration.Admin.Web.'
}

function Assert-FileExists {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$Label
    )
    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Expected file missing for {0}: {1}' -f $Label, $Path)
    }
}

function Assert-FileMissing {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$Label
    )
    if (Test-Path -Path $Path -PathType Leaf) {
        throw ('Legacy file should have been moved for {0}: {1}' -f $Label, $Path)
    }
}

function Assert-Contains {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$Text,
        [Parameter(Mandatory=$true)][string]$Label
    )
    Assert-FileExists -Path $Path -Label $Label
    $content = [System.IO.File]::ReadAllText($Path)
    if (-not $content.Contains($Text)) {
        throw ('Expected text missing for {0}: {1}' -f $Label, $Text)
    }
}

function Assert-NotContains {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$Text,
        [Parameter(Mandatory=$true)][string]$Label
    )
    Assert-FileExists -Path $Path -Label $Label
    $content = [System.IO.File]::ReadAllText($Path)
    if ($content.Contains($Text)) {
        throw ('Unexpected text found for {0}: {1}' -f $Label, $Text)
    }
}

function Test-PathHasSegment {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$Segment
    )
    $parts = $Path -split [System.Text.RegularExpressions.Regex]::Escape([System.IO.Path]::DirectorySeparatorChar)
    return @($parts) -contains $Segment
}

$repoRoot = Get-RepoRoot
$adminSrc = Join-Path -Path $repoRoot -ChildPath 'src/Admin/Migration.Admin.Web/src'
$toolRoot = Join-Path -Path $repoRoot -ChildPath 'tools/p10/P10.2AK-Repair3'
$featureRoot = Join-Path -Path $adminSrc -ChildPath 'features/operations/executionProfiles'
$pagePath = Join-Path -Path $featureRoot -ChildPath 'pages/ExecutionProfiles.tsx'
$apiPath = Join-Path -Path $featureRoot -ChildPath 'api/executionProfilesApi.ts'
$typePath = Join-Path -Path $featureRoot -ChildPath 'types/executionProfiles.ts'

Assert-FileExists -Path $pagePath -Label 'Execution Profiles feature page'
Assert-FileExists -Path $apiPath -Label 'Execution Profiles feature API'
Assert-FileExists -Path $typePath -Label 'Execution Profiles feature types'

Assert-FileMissing -Path (Join-Path -Path $adminSrc -ChildPath 'pages/ExecutionProfiles.tsx') -Label 'Execution Profiles legacy page'
Assert-FileMissing -Path (Join-Path -Path $adminSrc -ChildPath 'api/executionProfilesApi.ts') -Label 'Execution Profiles legacy API'
Assert-FileMissing -Path (Join-Path -Path $adminSrc -ChildPath 'types/executionProfiles.ts') -Label 'Execution Profiles legacy types'

Assert-Contains -Path $pagePath -Text '../../../../components/Card' -Label 'Execution Profiles page Card import'
Assert-Contains -Path $pagePath -Text '../../../../components/LoadingError' -Label 'Execution Profiles page LoadingError import'
Assert-Contains -Path $pagePath -Text '../api/executionProfilesApi' -Label 'Execution Profiles page API import'
Assert-Contains -Path $pagePath -Text '../types/executionProfiles' -Label 'Execution Profiles page types import'
Assert-Contains -Path $apiPath -Text '../../../../api/core/adminApiClient' -Label 'Execution Profiles API admin client import'
Assert-Contains -Path $apiPath -Text '../types/executionProfiles' -Label 'Execution Profiles API types import'

Assert-NotContains -Path $pagePath -Text '../components/Card' -Label 'Execution Profiles page legacy Card import'
Assert-NotContains -Path $pagePath -Text '../components/LoadingError' -Label 'Execution Profiles page legacy LoadingError import'
Assert-NotContains -Path $apiPath -Text './core/adminApiClient' -Label 'Execution Profiles API legacy admin client import'

$scriptFiles = Get-ChildItem -Path $toolRoot -Filter '*.ps1' -File -Recurse
foreach ($scriptFile in $scriptFiles) {
    if (Test-PathHasSegment -Path $scriptFile.FullName -Segment 'bin') { continue }
    if (Test-PathHasSegment -Path $scriptFile.FullName -Segment 'obj') { continue }
    $scriptContent = [System.IO.File]::ReadAllText($scriptFile.FullName)
    if ($scriptContent.Contains('$Label:') -or $scriptContent.Contains('$Path:') -or $scriptContent.Contains('$Source:')) {
        throw ('Unsafe variable-colon interpolation found in {0}' -f $scriptFile.FullName)
    }
}

Write-Host 'P10.2AK Repair3 validation passed.'
