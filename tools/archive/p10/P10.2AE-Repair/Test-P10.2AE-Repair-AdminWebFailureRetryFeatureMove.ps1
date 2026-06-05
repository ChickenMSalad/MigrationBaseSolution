Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Find-RepoRoot {
    $current = $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($current)) {
        $current = (Get-Location).Path
    }

    while (-not [string]::IsNullOrWhiteSpace($current)) {
        $marker = Join-Path -Path $current -ChildPath 'Directory.Packages.props'
        $src = Join-Path -Path $current -ChildPath 'src'
        if ((Test-Path -Path $marker -PathType Leaf) -and (Test-Path -Path $src -PathType Container)) {
            return $current
        }

        $parent = Split-Path -Path $current -Parent
        if ($parent -eq $current) {
            break
        }

        $current = $parent
    }

    throw 'Could not locate repository root from script location.'
}

function Join-Under {
    param(
        [Parameter(Mandatory = $true)][string]$Base,
        [Parameter(Mandatory = $true)][string[]]$Parts
    )

    $path = $Base
    foreach ($part in $Parts) {
        $path = Join-Path -Path $path -ChildPath $part
    }

    return $path
}

function Assert-FileExists {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw "Expected file was not found: $Path"
    }
}

function Assert-FileMissing {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (Test-Path -Path $Path -PathType Leaf) {
        throw "File should have been moved but still exists: $Path"
    }
}

function Assert-FileContains {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Text
    )

    Assert-FileExists -Path $Path
    $content = Get-Content -Path $Path -Raw
    if (-not $content.Contains($Text)) {
        throw "Expected text was not found in $Path : $Text"
    }
}

function Assert-FileNotContains {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Text
    )

    Assert-FileExists -Path $Path
    $content = Get-Content -Path $Path -Raw
    if ($content.Contains($Text)) {
        throw "Unexpected text was found in $Path : $Text"
    }
}

$repoRoot = Find-RepoRoot
$adminSrc = Join-Under -Base $repoRoot -Parts @('src', 'Admin', 'Migration.Admin.Web', 'src')
$featureRoot = Join-Under -Base $adminSrc -Parts @('features', 'operations', 'failureRetry')

$expectedFiles = @(
    [pscustomobject]@{ Path = Join-Under -Base $featureRoot -Parts @('pages', 'FailureRetry.tsx') },
    [pscustomobject]@{ Path = Join-Under -Base $featureRoot -Parts @('api', 'failureRetryApi.ts') },
    [pscustomobject]@{ Path = Join-Under -Base $featureRoot -Parts @('types', 'failureRetry.ts') }
)

foreach ($expected in $expectedFiles) {
    Assert-FileExists -Path $expected.Path
}

$oldFiles = @(
    [pscustomobject]@{ Path = Join-Under -Base $adminSrc -Parts @('pages', 'FailureRetry.tsx') },
    [pscustomobject]@{ Path = Join-Under -Base $adminSrc -Parts @('api', 'failureRetryApi.ts') },
    [pscustomobject]@{ Path = Join-Under -Base $adminSrc -Parts @('types', 'failureRetry.ts') }
)

foreach ($old in $oldFiles) {
    Assert-FileMissing -Path $old.Path
}

$pagePath = Join-Under -Base $featureRoot -Parts @('pages', 'FailureRetry.tsx')
Assert-FileContains -Path $pagePath -Text 'from "../api/failureRetryApi"'
Assert-FileContains -Path $pagePath -Text 'from "../types/failureRetry"'

$appPath = Join-Under -Base $adminSrc -Parts @('App.tsx')
Assert-FileContains -Path $appPath -Text 'from "./features/operations/failureRetry/pages/FailureRetry"'
Assert-FileNotContains -Path $appPath -Text 'from "./pages/FailureRetry"'

Write-Host 'P10.2AE repair validation passed.'
