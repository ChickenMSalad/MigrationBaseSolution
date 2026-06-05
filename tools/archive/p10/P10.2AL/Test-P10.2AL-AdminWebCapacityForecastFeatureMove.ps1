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

function Assert-FileExists {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label
    )

    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Expected file missing for {0}: {1}' -f $Label, $Path)
    }
}

function Assert-FileMissing {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label
    )

    if (Test-Path -Path $Path -PathType Leaf) {
        throw ('Legacy file still exists for {0}: {1}' -f $Label, $Path)
    }
}

function Assert-ImportLine {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Pattern,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $content = Get-Content -Path $Path -Raw
    if ($content -notmatch $Pattern) {
        throw ('Expected import line missing for {0}: {1}' -f $Label, $Pattern)
    }
}

$repoRoot = Get-RepoRoot
$adminSrc = Join-RepoPath -Root $repoRoot -Segments @('src','Admin','Migration.Admin.Web','src')
$featureRoot = Join-RepoPath -Root $adminSrc -Segments @('features','platform','capacityForecast')

$page = Join-RepoPath -Root $featureRoot -Segments @('pages','CapacityForecast.tsx')
$api = Join-RepoPath -Root $featureRoot -Segments @('api','capacityForecastApi.ts')
$types = Join-RepoPath -Root $featureRoot -Segments @('types','capacityForecast.ts')

Assert-FileExists -Path $page -Label 'Capacity Forecast page'
Assert-FileExists -Path $api -Label 'Capacity Forecast API'
Assert-FileExists -Path $types -Label 'Capacity Forecast types'

Assert-FileMissing -Path (Join-RepoPath -Root $adminSrc -Segments @('pages','CapacityForecast.tsx')) -Label 'Capacity Forecast page'
Assert-FileMissing -Path (Join-RepoPath -Root $adminSrc -Segments @('api','capacityForecastApi.ts')) -Label 'Capacity Forecast API'
Assert-FileMissing -Path (Join-RepoPath -Root $adminSrc -Segments @('types','capacityForecast.ts')) -Label 'Capacity Forecast types'

Assert-ImportLine -Path $page -Pattern 'import\s+\{\s*getCapacityForecast\s*\}\s+from\s+"\.\./api/capacityForecastApi"' -Label 'Capacity Forecast page API import'
Assert-ImportLine -Path $page -Pattern 'import\s+type\s+\{\s*CapacityForecastSummary\s*\}\s+from\s+"\.\./types/capacityForecast"' -Label 'Capacity Forecast page type import'
Assert-ImportLine -Path $api -Pattern 'import\s+\{\s*adminApiClient\s*\}\s+from\s+"\.\./\.\./\.\./\.\./api/core/adminApiClient"' -Label 'Capacity Forecast API core client import'
Assert-ImportLine -Path $api -Pattern 'import\s+type\s+\{\s*CapacityForecastSummary\s*\}\s+from\s+"\.\./types/capacityForecast"' -Label 'Capacity Forecast API type import'

Write-Host 'P10.2AL Capacity Forecast feature move validation passed.'
