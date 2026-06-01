Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $scriptRoot = $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
        $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    }

    $root = Resolve-Path (Join-Path $scriptRoot '..\..\..')
    return $root.Path
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

function Assert-Leaf {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Label
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw ('Missing required {0}: {1}' -f $Label, $Path)
    }
}

function Assert-TextContains {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Expected,
        [Parameter(Mandatory = $true)][string] $Label
    )

    Assert-Leaf -Path $Path -Label $Label
    $content = Get-Content -LiteralPath $Path -Raw
    if (-not $content.Contains($Expected)) {
        throw ('Expected text was not found for {0}: {1}' -f $Label, $Expected)
    }
}

function Assert-TextNotContains {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Unexpected,
        [Parameter(Mandatory = $true)][string] $Label
    )

    Assert-Leaf -Path $Path -Label $Label
    $content = Get-Content -LiteralPath $Path -Raw
    if ($content.Contains($Unexpected)) {
        throw ('Unexpected text was found for {0}: {1}' -f $Label, $Unexpected)
    }
}

function Test-PathDoesNotHaveSegment {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Segment
    )

    $parts = $Path -split [System.Text.RegularExpressions.Regex]::Escape([System.IO.Path]::DirectorySeparatorChar)
    foreach ($part in $parts) {
        if ($part -eq $Segment) {
            return $false
        }
    }

    return $true
}

$repoRoot = Get-RepoRoot
$adminSrc = Join-RepoPath -Root $repoRoot -Segments @('src','Admin','Migration.Admin.Web','src')
$appFile = Join-RepoPath -Root $adminSrc -Segments @('App.tsx')
$featureRoot = Join-RepoPath -Root $adminSrc -Segments @('features','operations','operationalEvents')

$expectedFiles = @(
    [pscustomobject]@{ Label = 'Operational Events page'; Path = (Join-RepoPath -Root $featureRoot -Segments @('pages','OperationalEvents.tsx')) },
    [pscustomobject]@{ Label = 'Operational Events API'; Path = (Join-RepoPath -Root $featureRoot -Segments @('api','operationalEventsApi.ts')) },
    [pscustomobject]@{ Label = 'Operational Events types'; Path = (Join-RepoPath -Root $featureRoot -Segments @('types','operationalEvents.ts')) }
)

foreach ($file in $expectedFiles) {
    Assert-Leaf -Path $file.Path -Label $file.Label
}

Assert-TextContains -Path $appFile -Expected 'import { OperationalEvents } from "./features/operations/operationalEvents/pages/OperationalEvents";' -Label 'App.tsx feature import'
Assert-TextNotContains -Path $appFile -Unexpected 'import { OperationalEvents } from "./pages/OperationalEvents";' -Label 'App.tsx old import'

$scriptRoot = $PSScriptRoot
$psScripts = Get-ChildItem -LiteralPath $scriptRoot -Filter '*.ps1' -File
foreach ($script in $psScripts) {
    $text = Get-Content -LiteralPath $script.FullName -Raw
    $unsafeVariableColon = '$' + 'Label:'
    if ($text.Contains($unsafeVariableColon)) {
        throw ('Unsafe variable-colon interpolation found in script: {0}' -f $script.FullName)
    }
    if ($text.Contains("@(`r`n    @(")) {
        throw ('Nested array validation pattern found in script: {0}' -f $script.FullName)
    }
    if ($text.Contains('src\t') -or $text.Contains('src\a')) {
        throw ('Potential corrupted escape sequence found in script: {0}' -f $script.FullName)
    }
}

Write-Host 'P10.2AJ validation passed.'
