Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $scriptRoot = $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
        $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    }

    $candidate = Resolve-Path -Path (Join-Path $scriptRoot '..\..\..')
    return $candidate.Path
}

function Join-RepoPath {
    param(
        [Parameter(Mandatory = $true)][string] $Root,
        [Parameter(Mandatory = $true)][string[]] $Segments
    )

    $path = $Root
    foreach ($segment in $Segments) {
        $path = [System.IO.Path]::Combine($path, $segment)
    }

    return $path
}

function Assert-LeafExists {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Label
    )

    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw "Missing $Label file: $Path"
    }
}

function Assert-LeafMissing {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Label
    )

    if (Test-Path -Path $Path -PathType Leaf) {
        throw "Unexpected old $Label file remains: $Path"
    }
}

function Assert-FileContains {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Text,
        [Parameter(Mandatory = $true)][string] $Label
    )

    Assert-LeafExists -Path $Path -Label $Label
    $content = Get-Content -Path $Path -Raw
    if (-not $content.Contains($Text)) {
        throw "Expected text was not found in $Label file $Path"
    }
}

$repoRoot = Get-RepoRoot
$adminWebSrc = Join-RepoPath -Root $repoRoot -Segments @('src','Admin','Migration.Admin.Web','src')
$featureRoot = Join-RepoPath -Root $adminWebSrc -Segments @('features','governance','notificationRouting')

$pageDestination = Join-RepoPath -Root $featureRoot -Segments @('pages','NotificationRouting.tsx')
$apiDestination = Join-RepoPath -Root $featureRoot -Segments @('api','notificationRoutingApi.ts')
$typeDestination = Join-RepoPath -Root $featureRoot -Segments @('types','notificationRouting.ts')
$appPath = Join-RepoPath -Root $adminWebSrc -Segments @('App.tsx')

Assert-LeafExists -Path $pageDestination -Label 'NotificationRouting page'
Assert-LeafExists -Path $apiDestination -Label 'NotificationRouting API'
Assert-LeafExists -Path $typeDestination -Label 'NotificationRouting types'

Assert-LeafMissing -Path (Join-RepoPath -Root $adminWebSrc -Segments @('pages','NotificationRouting.tsx')) -Label 'NotificationRouting page'
Assert-LeafMissing -Path (Join-RepoPath -Root $adminWebSrc -Segments @('api','notificationRoutingApi.ts')) -Label 'NotificationRouting API'
Assert-LeafMissing -Path (Join-RepoPath -Root $adminWebSrc -Segments @('types','notificationRouting.ts')) -Label 'NotificationRouting types'

Assert-FileContains -Path $pageDestination -Text "../api/notificationRoutingApi" -Label 'NotificationRouting page'
Assert-FileContains -Path $pageDestination -Text "../types/notificationRouting" -Label 'NotificationRouting page'
Assert-FileContains -Path $apiDestination -Text "../../../../api/core/adminApiClient" -Label 'NotificationRouting API'
Assert-FileContains -Path $apiDestination -Text "../types/notificationRouting" -Label 'NotificationRouting API'
Assert-FileContains -Path $appPath -Text "./features/governance/notificationRouting/pages/NotificationRouting" -Label 'App'

Write-Host 'P10.2AG Admin Web Notification Routing feature move validation passed.'
