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

function Ensure-Directory {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -Path $Path -PathType Container)) {
        New-Item -Path $Path -ItemType Directory -Force | Out-Null
    }
}

function Move-CanonicalFile {
    param(
        [Parameter(Mandatory = $true)][string]$Source,
        [Parameter(Mandatory = $true)][string]$Destination
    )

    $destinationDirectory = Split-Path -Path $Destination -Parent
    Ensure-Directory -Path $destinationDirectory

    $sourceExists = Test-Path -Path $Source -PathType Leaf
    $destinationExists = Test-Path -Path $Destination -PathType Leaf

    if ($sourceExists -and $destinationExists) {
        $sourceText = Get-Content -Path $Source -Raw
        $destinationText = Get-Content -Path $Destination -Raw
        if ($sourceText -ne $destinationText) {
            throw "Both source and destination exist with different content. Source: $Source Destination: $Destination"
        }

        Remove-Item -Path $Source -Force
        Write-Host "Removed duplicate source already present at destination: $Source"
        return
    }

    if ($sourceExists) {
        Move-Item -Path $Source -Destination $Destination
        Write-Host "Moved: $Source -> $Destination"
        return
    }

    if ($destinationExists) {
        Write-Host "Already moved: $Destination"
        return
    }

    throw "Neither source nor destination exists. Source: $Source Destination: $Destination"
}

function Replace-InFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$OldValue,
        [Parameter(Mandatory = $true)][string]$NewValue
    )

    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw "Required file was not found: $Path"
    }

    $content = Get-Content -Path $Path -Raw
    if ($content.Contains($OldValue)) {
        $content = $content.Replace($OldValue, $NewValue)
        Set-Content -Path $Path -Value $content -Encoding UTF8
        Write-Host "Updated: $Path"
        return
    }

    if ($content.Contains($NewValue)) {
        Write-Host "Already updated: $Path"
        return
    }

    throw "Could not find old or new import text in: $Path"
}

$repoRoot = Find-RepoRoot
$adminSrc = Join-Under -Base $repoRoot -Parts @('src', 'Admin', 'Migration.Admin.Web', 'src')
if (-not (Test-Path -Path $adminSrc -PathType Container)) {
    throw "Admin Web source folder was not found: $adminSrc"
}

$featureRoot = Join-Under -Base $adminSrc -Parts @('features', 'operations', 'failureRetry')

$moves = @(
    [pscustomobject]@{
        Name = 'Failure Retry page'
        Source = Join-Under -Base $adminSrc -Parts @('pages', 'FailureRetry.tsx')
        Destination = Join-Under -Base $featureRoot -Parts @('pages', 'FailureRetry.tsx')
    },
    [pscustomobject]@{
        Name = 'Failure Retry API'
        Source = Join-Under -Base $adminSrc -Parts @('api', 'failureRetryApi.ts')
        Destination = Join-Under -Base $featureRoot -Parts @('api', 'failureRetryApi.ts')
    },
    [pscustomobject]@{
        Name = 'Failure Retry types'
        Source = Join-Under -Base $adminSrc -Parts @('types', 'failureRetry.ts')
        Destination = Join-Under -Base $featureRoot -Parts @('types', 'failureRetry.ts')
    }
)

foreach ($move in $moves) {
    $sourceExists = Test-Path -Path $move.Source -PathType Leaf
    $destinationExists = Test-Path -Path $move.Destination -PathType Leaf
    if ((-not $sourceExists) -and (-not $destinationExists)) {
        throw ("Required file was not found in either source or destination for {0}. Source: {1} Destination: {2}" -f $move.Name, $move.Source, $move.Destination)
    }
}

foreach ($move in $moves) {
    Move-CanonicalFile -Source $move.Source -Destination $move.Destination
}

$pagePath = Join-Under -Base $featureRoot -Parts @('pages', 'FailureRetry.tsx')
Replace-InFile -Path $pagePath -OldValue 'from "../api/failureRetryApi"' -NewValue 'from "../api/failureRetryApi"'
Replace-InFile -Path $pagePath -OldValue 'from "../types/failureRetry"' -NewValue 'from "../types/failureRetry"'

$appPath = Join-Under -Base $adminSrc -Parts @('App.tsx')
Replace-InFile -Path $appPath -OldValue 'from "./pages/FailureRetry"' -NewValue 'from "./features/operations/failureRetry/pages/FailureRetry"'

Write-Host 'P10.2AE repair applied successfully.'
