Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $current = (Get-Location).Path
    while (-not [string]::IsNullOrWhiteSpace($current)) {
        $candidate = [System.IO.Path]::Combine($current, 'src', 'Admin', 'Migration.Admin.Web', 'src')
        if (Test-Path -Path $candidate -PathType Container) {
            return $current
        }

        $parent = [System.IO.Directory]::GetParent($current)
        if ($null -eq $parent) {
            break
        }

        $current = $parent.FullName
    }

    throw 'Unable to locate repository root. Run this script from inside MigrationBaseSolution.'
}

function Ensure-Directory {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -Path $Path -PathType Container)) {
        New-Item -Path $Path -ItemType Directory -Force | Out-Null
    }
}

function Move-LeafFile {
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

    Ensure-Directory -Path ([System.IO.Path]::GetDirectoryName($Destination))
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

    $updated = $content.Replace($OldText, $NewText)
    Set-Content -Path $Path -Value $updated -NoNewline
    Write-Host ('Updated {0}: {1}' -f $Label, $Path)
}

$repoRoot = Get-RepoRoot
$adminSrc = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src')

$featureRoot = [System.IO.Path]::Combine($adminSrc, 'features', 'platform', 'costAnalytics')
$pageTarget = [System.IO.Path]::Combine($featureRoot, 'pages', 'CostAnalytics.tsx')
$apiTarget = [System.IO.Path]::Combine($featureRoot, 'api', 'costAnalyticsApi.ts')
$typeTarget = [System.IO.Path]::Combine($featureRoot, 'types', 'costAnalytics.ts')

$pageSource = [System.IO.Path]::Combine($adminSrc, 'pages', 'CostAnalytics.tsx')
$apiSource = [System.IO.Path]::Combine($adminSrc, 'api', 'costAnalyticsApi.ts')
$typeSource = [System.IO.Path]::Combine($adminSrc, 'types', 'costAnalytics.ts')

$requiredMoves = @(
    [pscustomobject]@{ Source = $pageSource; Destination = $pageTarget; Label = 'Cost Analytics page' },
    [pscustomobject]@{ Source = $apiSource; Destination = $apiTarget; Label = 'Cost Analytics API' },
    [pscustomobject]@{ Source = $typeSource; Destination = $typeTarget; Label = 'Cost Analytics types' }
)

foreach ($move in $requiredMoves) {
    if ((-not (Test-Path -Path $move.Destination -PathType Leaf)) -and (-not (Test-Path -Path $move.Source -PathType Leaf))) {
        throw ('Required source file was not found before moving {0}: {1}' -f $move.Label, $move.Source)
    }
}

foreach ($move in $requiredMoves) {
    Move-LeafFile -Source $move.Source -Destination $move.Destination -Label $move.Label
}

Replace-TextIfPresent -Path $apiTarget -OldText 'from "./core/adminApiClient"' -NewText 'from "../../../../api/core/adminApiClient"' -Label 'Cost Analytics API core client import'

$appPath = [System.IO.Path]::Combine($adminSrc, 'App.tsx')
if (Test-Path -Path $appPath -PathType Leaf) {
    Replace-TextIfPresent -Path $appPath -OldText 'from "./pages/CostAnalytics"' -NewText 'from "./features/platform/costAnalytics/pages/CostAnalytics"' -Label 'App.tsx Cost Analytics import'
}

Write-Host 'P10.2AM Admin Web Cost Analytics feature move completed.'
