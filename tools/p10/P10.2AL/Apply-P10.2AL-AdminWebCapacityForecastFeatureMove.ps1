Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $scriptRoot = $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
        $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    }

    $candidate = [System.IO.Path]::GetFullPath((Join-Path $scriptRoot '..\..\..'))
    if (-not (Test-Path -Path (Join-Path $candidate 'src') -PathType Container)) {
        throw ('Unable to resolve repository root from script path: {0}' -f $scriptRoot)
    }

    return $candidate
}

function Join-RepoPath {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string[]]$Segments
    )

    $path = $Root
    foreach ($segment in $Segments) {
        $path = Join-Path $path $segment
    }

    return $path
}

function Ensure-Directory {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -Path $Path -PathType Container)) {
        New-Item -Path $Path -ItemType Directory -Force | Out-Null
    }
}

function Move-FeatureFile {
    param(
        [Parameter(Mandatory = $true)][string]$Source,
        [Parameter(Mandatory = $true)][string]$Destination,
        [Parameter(Mandatory = $true)][string]$Label
    )

    if (Test-Path -Path $Destination -PathType Leaf) {
        Write-Host ('Already moved {0}: {1}' -f $Label, $Destination)
        return
    }

    if (-not (Test-Path -Path $Source -PathType Leaf)) {
        throw ('Required source file was not found for {0}: {1}' -f $Label, $Source)
    }

    $destinationDirectory = Split-Path -Parent $Destination
    Ensure-Directory -Path $destinationDirectory
    Move-Item -Path $Source -Destination $Destination
    Write-Host ('Moved {0}: {1}' -f $Label, $Destination)
}

function Replace-TextIfPresent {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$OldText,
        [Parameter(Mandatory = $true)][string]$NewText,
        [Parameter(Mandatory = $true)][string]$Label
    )

    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Cannot update missing file for {0}: {1}' -f $Label, $Path)
    }

    $content = Get-Content -Path $Path -Raw
    if ($content.Contains($NewText)) {
        Write-Host ('Already updated {0}: {1}' -f $Label, $Path)
        return
    }

    if (-not $content.Contains($OldText)) {
        Write-Host ('No update needed for {0}: expected legacy text not present.' -f $Label)
        return
    }

    $content = $content.Replace($OldText, $NewText)
    Set-Content -Path $Path -Value $content -NoNewline
    Write-Host ('Updated {0}: {1}' -f $Label, $Path)
}

$repoRoot = Get-RepoRoot
$adminSrc = Join-RepoPath -Root $repoRoot -Segments @('src','Admin','Migration.Admin.Web','src')
$featureRoot = Join-RepoPath -Root $adminSrc -Segments @('features','platform','capacityForecast')

$pageSource = Join-RepoPath -Root $adminSrc -Segments @('pages','CapacityForecast.tsx')
$apiSource = Join-RepoPath -Root $adminSrc -Segments @('api','capacityForecastApi.ts')
$typesSource = Join-RepoPath -Root $adminSrc -Segments @('types','capacityForecast.ts')

$pageDestination = Join-RepoPath -Root $featureRoot -Segments @('pages','CapacityForecast.tsx')
$apiDestination = Join-RepoPath -Root $featureRoot -Segments @('api','capacityForecastApi.ts')
$typesDestination = Join-RepoPath -Root $featureRoot -Segments @('types','capacityForecast.ts')

$requiredMoves = @(
    [pscustomobject]@{ Label = 'Capacity Forecast page'; Source = $pageSource; Destination = $pageDestination },
    [pscustomobject]@{ Label = 'Capacity Forecast API'; Source = $apiSource; Destination = $apiDestination },
    [pscustomobject]@{ Label = 'Capacity Forecast types'; Source = $typesSource; Destination = $typesDestination }
)

foreach ($move in $requiredMoves) {
    $sourceExists = Test-Path -Path $move.Source -PathType Leaf
    $destinationExists = Test-Path -Path $move.Destination -PathType Leaf
    if (-not $sourceExists -and -not $destinationExists) {
        throw ('Neither source nor destination exists for {0}. Source: {1} Destination: {2}' -f $move.Label, $move.Source, $move.Destination)
    }
}

foreach ($move in $requiredMoves) {
    Move-FeatureFile -Source $move.Source -Destination $move.Destination -Label $move.Label
}

Replace-TextIfPresent -Path $apiDestination -OldText 'from "./core/adminApiClient"' -NewText 'from "../../../../api/core/adminApiClient"' -Label 'Capacity Forecast API core client import'

Write-Host 'P10.2AL Capacity Forecast feature move completed.'
