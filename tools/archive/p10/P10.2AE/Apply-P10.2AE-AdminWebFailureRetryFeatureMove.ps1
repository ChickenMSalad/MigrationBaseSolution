Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $scriptRoot = $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
        $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    }

    $current = Resolve-Path -LiteralPath $scriptRoot
    while ($null -ne $current) {
        $candidate = Join-Path -Path $current.Path -ChildPath 'src'
        $adminCandidate = Join-Path -Path $candidate -ChildPath 'Admin'
        if (Test-Path -LiteralPath $adminCandidate -PathType Container) {
            return $current.Path
        }

        $parent = Split-Path -Parent $current.Path
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $current.Path) {
            break
        }

        $current = Resolve-Path -LiteralPath $parent
    }

    throw 'Unable to locate repository root. Run this script from inside the MigrationBaseSolution repository.'
}

function Join-RepoPath {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string[]]$Segments
    )

    $result = $Root
    foreach ($segment in $Segments) {
        $result = Join-Path -Path $result -ChildPath $segment
    }

    return $result
}

function Move-FileIfNeeded {
    param(
        [Parameter(Mandatory = $true)][string]$Source,
        [Parameter(Mandatory = $true)][string]$Destination
    )

    if (Test-Path -LiteralPath $Destination -PathType Leaf) {
        if (Test-Path -LiteralPath $Source -PathType Leaf) {
            throw "Both source and destination exist. Refusing to overwrite destination: $Destination"
        }

        Write-Host "Already moved: $Destination"
        return
    }

    if (-not (Test-Path -LiteralPath $Source -PathType Leaf)) {
        throw "Required source file was not found: $Source"
    }

    $destinationDirectory = Split-Path -Parent $Destination
    if (-not (Test-Path -LiteralPath $destinationDirectory -PathType Container)) {
        New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
    }

    Move-Item -LiteralPath $Source -Destination $Destination
    Write-Host "Moved: $Source -> $Destination"
}

function Update-TextFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][scriptblock]$Updater
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Required file was not found: $Path"
    }

    $before = Get-Content -LiteralPath $Path -Raw
    $after = & $Updater $before

    if ($null -eq $after) {
        throw "Updater returned null for $Path"
    }

    if ($after -ne $before) {
        Set-Content -LiteralPath $Path -Value $after -Encoding UTF8
        Write-Host "Updated: $Path"
    }
    else {
        Write-Host "No changes needed: $Path"
    }
}

$repoRoot = Get-RepoRoot
$adminSrc = Join-RepoPath -Root $repoRoot -Segments @('src','Admin','Migration.Admin.Web','src')
$featureRoot = Join-RepoPath -Root $adminSrc -Segments @('features','operations','failureRetry')

$pageSource = Join-RepoPath -Root $adminSrc -Segments @('pages','FailureRetry.tsx')
$pageDestination = Join-RepoPath -Root $featureRoot -Segments @('pages','FailureRetry.tsx')
$apiSource = Join-RepoPath -Root $adminSrc -Segments @('api','failureRetry.ts')
$apiDestination = Join-RepoPath -Root $featureRoot -Segments @('api','failureRetry.ts')
$typeSource = Join-RepoPath -Root $adminSrc -Segments @('types','failureRetry.ts')
$typeDestination = Join-RepoPath -Root $featureRoot -Segments @('types','failureRetry.ts')
$appPath = Join-RepoPath -Root $adminSrc -Segments @('App.tsx')

Move-FileIfNeeded -Source $pageSource -Destination $pageDestination
Move-FileIfNeeded -Source $apiSource -Destination $apiDestination
Move-FileIfNeeded -Source $typeSource -Destination $typeDestination

Update-TextFile -Path $pageDestination -Updater {
    param([string]$text)
    $updated = $text
    $updated = $updated.Replace('../api/failureRetry', '../api/failureRetry')
    $updated = $updated.Replace('../types/failureRetry', '../types/failureRetry')
    $updated = $updated.Replace('../lib/apiClient', '../../../../lib/apiClient')
    $updated = $updated.Replace('../lib/http', '../../../../lib/http')
    $updated = $updated.Replace('../components/', '../../../../components/')
    return $updated
}

Update-TextFile -Path $apiDestination -Updater {
    param([string]$text)
    $updated = $text
    $updated = $updated.Replace('../types/failureRetry', '../types/failureRetry')
    $updated = $updated.Replace('../lib/apiClient', '../../../../lib/apiClient')
    $updated = $updated.Replace('../lib/http', '../../../../lib/http')
    return $updated
}

Update-TextFile -Path $appPath -Updater {
    param([string]$text)
    $updated = $text
    $updated = $updated.Replace('./pages/FailureRetry', './features/operations/failureRetry/pages/FailureRetry')
    return $updated
}

Write-Host 'P10.2AE apply completed.'
