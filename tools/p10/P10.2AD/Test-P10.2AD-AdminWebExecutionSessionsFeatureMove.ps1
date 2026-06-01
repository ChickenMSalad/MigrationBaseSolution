Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-ScriptRootSafe {
    if ($PSScriptRoot -and $PSScriptRoot.Length -gt 0) {
        return $PSScriptRoot
    }

    $invocationPath = $MyInvocation.MyCommand.Path
    if ($invocationPath -and $invocationPath.Length -gt 0) {
        return Split-Path -Parent $invocationPath
    }

    return (Get-Location).Path
}

function Find-RepoRoot {
    param(
        [Parameter(Mandatory = $true)]
        [string] $StartPath
    )

    $current = [System.IO.DirectoryInfo]::new($StartPath)
    while ($null -ne $current) {
        $adminWebPath = Join-Path -Path $current.FullName -ChildPath 'src/Admin/Migration.Admin.Web'
        if (Test-Path -Path $adminWebPath -PathType Container) {
            return $current.FullName
        }

        $current = $current.Parent
    }

    throw 'Unable to locate repository root containing src/Admin/Migration.Admin.Web.'
}

function Join-RepoPath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Root,

        [Parameter(Mandatory = $true)]
        [string[]] $Segments
    )

    $path = $Root
    foreach ($segment in $Segments) {
        $path = Join-Path -Path $path -ChildPath $segment
    }

    return $path
}

function Assert-LeafExists {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path
    )

    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw "Expected file was not found: $Path"
    }
}

function Assert-LeafMissing {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path
    )

    if (Test-Path -Path $Path -PathType Leaf) {
        throw "File should have been moved away: $Path"
    }
}

function Assert-FileContains {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path,

        [Parameter(Mandatory = $true)]
        [string] $Text
    )

    Assert-LeafExists -Path $Path
    $content = Get-Content -Path $Path -Raw
    if (-not $content.Contains($Text)) {
        throw "Expected text was not found in $Path"
    }
}

function Assert-FileDoesNotContain {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path,

        [Parameter(Mandatory = $true)]
        [string] $Text
    )

    Assert-LeafExists -Path $Path
    $content = Get-Content -Path $Path -Raw
    if ($content.Contains($Text)) {
        throw "Unexpected text was found in $Path"
    }
}

$scriptRoot = Get-ScriptRootSafe
$repoRoot = Find-RepoRoot -StartPath $scriptRoot
$adminSrc = Join-RepoPath -Root $repoRoot -Segments @('src', 'Admin', 'Migration.Admin.Web', 'src')
$featureRoot = Join-RepoPath -Root $adminSrc -Segments @('features', 'operations', 'executionSessions')

$expectedFiles = @(
    [pscustomobject]@{ Path = (Join-RepoPath -Root $featureRoot -Segments @('pages', 'ExecutionSessions.tsx')) },
    [pscustomobject]@{ Path = (Join-RepoPath -Root $featureRoot -Segments @('api', 'executionSessionsApi.ts')) },
    [pscustomobject]@{ Path = (Join-RepoPath -Root $featureRoot -Segments @('types', 'executionSessions.ts')) }
)

foreach ($expectedFile in $expectedFiles) {
    Assert-LeafExists -Path $expectedFile.Path
}

$movedSources = @(
    [pscustomobject]@{ Path = (Join-RepoPath -Root $adminSrc -Segments @('pages', 'ExecutionSessions.tsx')) },
    [pscustomobject]@{ Path = (Join-RepoPath -Root $adminSrc -Segments @('api', 'executionSessionsApi.ts')) },
    [pscustomobject]@{ Path = (Join-RepoPath -Root $adminSrc -Segments @('types', 'executionSessions.ts')) }
)

foreach ($movedSource in $movedSources) {
    Assert-LeafMissing -Path $movedSource.Path
}

$appPath = Join-Path -Path $adminSrc -ChildPath 'App.tsx'
Assert-FileContains -Path $appPath -Text 'import { ExecutionSessions } from "./features/operations/executionSessions/pages/ExecutionSessions";'
Assert-FileDoesNotContain -Path $appPath -Text 'import { ExecutionSessions } from "./pages/ExecutionSessions";'

$pagePath = Join-RepoPath -Root $featureRoot -Segments @('pages', 'ExecutionSessions.tsx')
Assert-FileContains -Path $pagePath -Text 'import { executionSessionsApi } from "../api/executionSessionsApi";'
Assert-FileContains -Path $pagePath -Text 'import type { ExecutionSessionRecord } from "../types/executionSessions";'

$apiPath = Join-RepoPath -Root $featureRoot -Segments @('api', 'executionSessionsApi.ts')
Assert-FileContains -Path $apiPath -Text 'from "../types/executionSessions"'

Write-Host 'P10.2AD Admin Web Execution Sessions feature move validation passed.'
