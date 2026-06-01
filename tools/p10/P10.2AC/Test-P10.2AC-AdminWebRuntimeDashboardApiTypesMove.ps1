[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-ScriptDirectory {
    if ($PSScriptRoot) {
        return $PSScriptRoot
    }

    $invocation = $MyInvocation
    if ($null -ne $invocation -and $null -ne $invocation.MyCommand -and -not [string]::IsNullOrWhiteSpace($invocation.MyCommand.Path)) {
        return Split-Path -Parent $invocation.MyCommand.Path
    }

    return (Get-Location).Path
}

function Join-RepoPath {
    param(
        [Parameter(Mandatory = $true)][string] $Root,
        [Parameter(Mandatory = $true)][string[]] $Segments
    )

    $current = $Root
    foreach ($segment in $Segments) {
        $current = Join-Path -Path $current -ChildPath $segment
    }

    return $current
}

function Find-RepoRoot {
    param([Parameter(Mandatory = $true)][string] $StartPath)

    $current = (Resolve-Path -Path $StartPath).Path
    while ($true) {
        $candidate = Join-RepoPath -Root $current -Segments @('src','Admin','Migration.Admin.Web','package.json')
        if (Test-Path -Path $candidate -PathType Leaf) {
            return $current
        }

        $parent = Split-Path -Parent $current
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $current) {
            throw 'Unable to locate repository root. Run this script from inside MigrationBaseSolution.'
        }

        $current = $parent
    }
}

function Assert-LeafExists {
    param([Parameter(Mandatory = $true)][string] $Path)

    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw "Expected file was not found: $Path"
    }
}

function Assert-LeafMissing {
    param([Parameter(Mandatory = $true)][string] $Path)

    if (Test-Path -Path $Path -PathType Leaf) {
        throw "Old file should not remain after feature move: $Path"
    }
}

function Assert-Contains {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Text
    )

    Assert-LeafExists -Path $Path
    $content = Get-Content -Path $Path -Raw
    if (-not $content.Contains($Text)) {
        throw "Expected text not found in $Path : $Text"
    }
}

function Assert-NotContains {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Text
    )

    Assert-LeafExists -Path $Path
    $content = Get-Content -Path $Path -Raw
    if ($content.Contains($Text)) {
        throw "Unexpected text found in $Path : $Text"
    }
}

$scriptDirectory = Get-ScriptDirectory
$repoRoot = Find-RepoRoot -StartPath $scriptDirectory
$webSrc = Join-RepoPath -Root $repoRoot -Segments @('src','Admin','Migration.Admin.Web','src')
$featureRoot = Join-RepoPath -Root $webSrc -Segments @('features','operations','runtimeDashboard')
$pagesDirectory = Join-Path -Path $featureRoot -ChildPath 'pages'
$apiDirectory = Join-Path -Path $featureRoot -ChildPath 'api'
$typesDirectory = Join-Path -Path $featureRoot -ChildPath 'types'

$expectedFiles = @(
    [pscustomobject]@{ Path = (Join-Path -Path $pagesDirectory -ChildPath 'RuntimeDashboard.tsx') },
    [pscustomobject]@{ Path = (Join-Path -Path $pagesDirectory -ChildPath 'RuntimeRunDetail.tsx') },
    [pscustomobject]@{ Path = (Join-Path -Path $apiDirectory -ChildPath 'runtimeDashboardApi.ts') },
    [pscustomobject]@{ Path = (Join-Path -Path $typesDirectory -ChildPath 'runtimeDashboard.ts') }
)

foreach ($file in $expectedFiles) {
    Assert-LeafExists -Path $file.Path
}

Assert-LeafMissing -Path (Join-RepoPath -Root $webSrc -Segments @('api','runtimeDashboardApi.ts'))
Assert-LeafMissing -Path (Join-RepoPath -Root $webSrc -Segments @('types','runtimeDashboard.ts'))

$runtimeDashboardPage = Join-Path -Path $pagesDirectory -ChildPath 'RuntimeDashboard.tsx'
$runtimeRunDetailPage = Join-Path -Path $pagesDirectory -ChildPath 'RuntimeRunDetail.tsx'
$runtimeDashboardApi = Join-Path -Path $apiDirectory -ChildPath 'runtimeDashboardApi.ts'
$appFile = Join-Path -Path $webSrc -ChildPath 'App.tsx'

$pageImportChecks = @(
    [pscustomobject]@{ Path = $runtimeDashboardPage; Text = 'from "../api/runtimeDashboardApi"' },
    [pscustomobject]@{ Path = $runtimeDashboardPage; Text = 'from "../types/runtimeDashboard"' },
    [pscustomobject]@{ Path = $runtimeDashboardPage; Text = 'from "../../../../components/Card"' },
    [pscustomobject]@{ Path = $runtimeDashboardPage; Text = 'from "../../../../components/LoadingError"' },
    [pscustomobject]@{ Path = $runtimeRunDetailPage; Text = 'from "../api/runtimeDashboardApi"' },
    [pscustomobject]@{ Path = $runtimeRunDetailPage; Text = 'from "../types/runtimeDashboard"' },
    [pscustomobject]@{ Path = $runtimeRunDetailPage; Text = 'from "../../../../components/Card"' },
    [pscustomobject]@{ Path = $runtimeRunDetailPage; Text = 'from "../../../../components/LoadingError"' }
)

foreach ($check in $pageImportChecks) {
    Assert-Contains -Path $check.Path -Text $check.Text
}

$forbiddenPageImports = @(
    [pscustomobject]@{ Path = $runtimeDashboardPage; Text = 'from "../components/Card"' },
    [pscustomobject]@{ Path = $runtimeDashboardPage; Text = 'from "../components/LoadingError"' },
    [pscustomobject]@{ Path = $runtimeRunDetailPage; Text = 'from "../components/Card"' },
    [pscustomobject]@{ Path = $runtimeRunDetailPage; Text = 'from "../components/LoadingError"' }
)

foreach ($check in $forbiddenPageImports) {
    Assert-NotContains -Path $check.Path -Text $check.Text
}

Assert-Contains -Path $runtimeDashboardApi -Text 'from "../types/runtimeDashboard"'
Assert-Contains -Path $appFile -Text 'from "./features/operations/runtimeDashboard/pages/RuntimeDashboard"'
Assert-Contains -Path $appFile -Text 'from "./features/operations/runtimeDashboard/pages/RuntimeRunDetail"'
Assert-NotContains -Path $appFile -Text 'from "./pages/RuntimeDashboard"'
Assert-NotContains -Path $appFile -Text 'from "./pages/RuntimeRunDetail"'

Write-Host 'P10.2AC validation passed.'
