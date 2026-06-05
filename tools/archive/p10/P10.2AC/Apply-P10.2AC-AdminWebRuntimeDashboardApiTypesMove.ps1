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

function Ensure-Directory {
    param([Parameter(Mandatory = $true)][string] $Path)

    if (-not (Test-Path -Path $Path -PathType Container)) {
        New-Item -Path $Path -ItemType Directory -Force | Out-Null
    }
}

function Move-LeafIfNeeded {
    param(
        [Parameter(Mandatory = $true)][string] $Source,
        [Parameter(Mandatory = $true)][string] $Destination
    )

    if (Test-Path -Path $Destination -PathType Leaf) {
        if (Test-Path -Path $Source -PathType Leaf) {
            $sourceText = Get-Content -Path $Source -Raw
            $destinationText = Get-Content -Path $Destination -Raw
            if ($sourceText -ne $destinationText) {
                throw "Destination exists with different content: $Destination"
            }
            Remove-Item -Path $Source -Force
        }
        return
    }

    if (-not (Test-Path -Path $Source -PathType Leaf)) {
        throw "Required source file was not found: $Source"
    }

    $destinationDirectory = Split-Path -Parent $Destination
    Ensure-Directory -Path $destinationDirectory
    Move-Item -Path $Source -Destination $Destination
}

function Replace-InFile {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Search,
        [Parameter(Mandatory = $true)][string] $Replacement
    )

    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw "File not found: $Path"
    }

    $text = Get-Content -Path $Path -Raw
    if ($text.Contains($Search)) {
        $updated = $text.Replace($Search, $Replacement)
        if ($updated -ne $text) {
            Set-Content -Path $Path -Value $updated -NoNewline -Encoding UTF8
        }
    }
}

$scriptDirectory = Get-ScriptDirectory
$repoRoot = Find-RepoRoot -StartPath $scriptDirectory
$webSrc = Join-RepoPath -Root $repoRoot -Segments @('src','Admin','Migration.Admin.Web','src')
$featureRoot = Join-RepoPath -Root $webSrc -Segments @('features','operations','runtimeDashboard')
$pagesDirectory = Join-Path -Path $featureRoot -ChildPath 'pages'
$apiDirectory = Join-Path -Path $featureRoot -ChildPath 'api'
$typesDirectory = Join-Path -Path $featureRoot -ChildPath 'types'

$requiredDirectories = @(
    [pscustomobject]@{ Path = $webSrc },
    [pscustomobject]@{ Path = $featureRoot },
    [pscustomobject]@{ Path = $pagesDirectory }
)

foreach ($entry in $requiredDirectories) {
    if (-not (Test-Path -Path $entry.Path -PathType Container)) {
        throw "Required directory was not found: $($entry.Path)"
    }
}

Ensure-Directory -Path $apiDirectory
Ensure-Directory -Path $typesDirectory

Move-LeafIfNeeded -Source (Join-RepoPath -Root $webSrc -Segments @('api','runtimeDashboardApi.ts')) -Destination (Join-Path -Path $apiDirectory -ChildPath 'runtimeDashboardApi.ts')
Move-LeafIfNeeded -Source (Join-RepoPath -Root $webSrc -Segments @('types','runtimeDashboard.ts')) -Destination (Join-Path -Path $typesDirectory -ChildPath 'runtimeDashboard.ts')

$runtimeDashboardPage = Join-Path -Path $pagesDirectory -ChildPath 'RuntimeDashboard.tsx'
$runtimeRunDetailPage = Join-Path -Path $pagesDirectory -ChildPath 'RuntimeRunDetail.tsx'
$runtimeDashboardApi = Join-Path -Path $apiDirectory -ChildPath 'runtimeDashboardApi.ts'
$appFile = Join-Path -Path $webSrc -ChildPath 'App.tsx'

$pageFiles = @(
    [pscustomobject]@{ Path = $runtimeDashboardPage },
    [pscustomobject]@{ Path = $runtimeRunDetailPage }
)

foreach ($page in $pageFiles) {
    Replace-InFile -Path $page.Path -Search 'from "../api/runtimeDashboardApi"' -Replacement 'from "../api/runtimeDashboardApi"'
    Replace-InFile -Path $page.Path -Search 'from "../types/runtimeDashboard"' -Replacement 'from "../types/runtimeDashboard"'
    Replace-InFile -Path $page.Path -Search 'from "../components/Card"' -Replacement 'from "../../../../components/Card"'
    Replace-InFile -Path $page.Path -Search 'from "../components/LoadingError"' -Replacement 'from "../../../../components/LoadingError"'
}

Replace-InFile -Path $runtimeDashboardApi -Search 'from "../types/runtimeDashboard"' -Replacement 'from "../types/runtimeDashboard"'
Replace-InFile -Path $runtimeDashboardApi -Search "from '../types/runtimeDashboard'" -Replacement "from '../types/runtimeDashboard'"

Replace-InFile -Path $appFile -Search 'from "./pages/RuntimeDashboard"' -Replacement 'from "./features/operations/runtimeDashboard/pages/RuntimeDashboard"'
Replace-InFile -Path $appFile -Search 'from "./pages/RuntimeRunDetail"' -Replacement 'from "./features/operations/runtimeDashboard/pages/RuntimeRunDetail"'

Write-Host 'P10.2AC applied: runtime dashboard API/types moved into the canonical feature folder and imports repaired.'
