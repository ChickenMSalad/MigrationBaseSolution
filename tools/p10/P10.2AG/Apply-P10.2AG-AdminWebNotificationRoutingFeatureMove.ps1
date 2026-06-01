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

function Assert-FileState {
    param(
        [Parameter(Mandatory = $true)][string] $Source,
        [Parameter(Mandatory = $true)][string] $Destination,
        [Parameter(Mandatory = $true)][string] $Label
    )

    $sourceExists = Test-Path -Path $Source -PathType Leaf
    $destinationExists = Test-Path -Path $Destination -PathType Leaf

    if (-not $sourceExists -and -not $destinationExists) {
        throw "Required $Label file was not found at either source or destination. Source: $Source Destination: $Destination"
    }
}

function Move-FileIfNeeded {
    param(
        [Parameter(Mandatory = $true)][string] $Source,
        [Parameter(Mandatory = $true)][string] $Destination,
        [Parameter(Mandatory = $true)][string] $Label
    )

    if (Test-Path -Path $Destination -PathType Leaf) {
        Write-Host "Already moved: $Destination"
        return
    }

    if (-not (Test-Path -Path $Source -PathType Leaf)) {
        throw "Required $Label source file was not found: $Source"
    }

    $destinationDirectory = Split-Path -Parent $Destination
    if (-not (Test-Path -Path $destinationDirectory -PathType Container)) {
        New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
    }

    Move-Item -Path $Source -Destination $Destination
    Write-Host "Moved: $Source -> $Destination"
}

function Update-TextFile {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $OldText,
        [Parameter(Mandatory = $true)][string] $NewText,
        [Parameter(Mandatory = $true)][string] $Label
    )

    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw "Cannot update missing $Label file: $Path"
    }

    $content = Get-Content -Path $Path -Raw
    if ($content.Contains($NewText)) {
        Write-Host "Already updated $Label: $Path"
        return
    }

    if (-not $content.Contains($OldText)) {
        throw "Expected text was not found while updating $Label in $Path"
    }

    $content = $content.Replace($OldText, $NewText)
    Set-Content -Path $Path -Value $content -NoNewline
    Write-Host "Updated $Label: $Path"
}

$repoRoot = Get-RepoRoot
$adminWebSrc = Join-RepoPath -Root $repoRoot -Segments @('src','Admin','Migration.Admin.Web','src')

$pageSource = Join-RepoPath -Root $adminWebSrc -Segments @('pages','NotificationRouting.tsx')
$apiSource = Join-RepoPath -Root $adminWebSrc -Segments @('api','notificationRoutingApi.ts')
$typeSource = Join-RepoPath -Root $adminWebSrc -Segments @('types','notificationRouting.ts')

$featureRoot = Join-RepoPath -Root $adminWebSrc -Segments @('features','governance','notificationRouting')
$pageDestination = Join-RepoPath -Root $featureRoot -Segments @('pages','NotificationRouting.tsx')
$apiDestination = Join-RepoPath -Root $featureRoot -Segments @('api','notificationRoutingApi.ts')
$typeDestination = Join-RepoPath -Root $featureRoot -Segments @('types','notificationRouting.ts')
$appPath = Join-RepoPath -Root $adminWebSrc -Segments @('App.tsx')

Assert-FileState -Source $pageSource -Destination $pageDestination -Label 'NotificationRouting page'
Assert-FileState -Source $apiSource -Destination $apiDestination -Label 'NotificationRouting API'
Assert-FileState -Source $typeSource -Destination $typeDestination -Label 'NotificationRouting types'

Move-FileIfNeeded -Source $pageSource -Destination $pageDestination -Label 'NotificationRouting page'
Move-FileIfNeeded -Source $apiSource -Destination $apiDestination -Label 'NotificationRouting API'
Move-FileIfNeeded -Source $typeSource -Destination $typeDestination -Label 'NotificationRouting types'

Update-TextFile -Path $pageDestination -OldText "../api/notificationRoutingApi" -NewText "../api/notificationRoutingApi" -Label 'NotificationRouting page API import'
Update-TextFile -Path $pageDestination -OldText "../types/notificationRouting" -NewText "../types/notificationRouting" -Label 'NotificationRouting page types import'
Update-TextFile -Path $apiDestination -OldText "./core/adminApiClient" -NewText "../../../../api/core/adminApiClient" -Label 'NotificationRouting API core client import'
Update-TextFile -Path $apiDestination -OldText "../types/notificationRouting" -NewText "../types/notificationRouting" -Label 'NotificationRouting API types import'
Update-TextFile -Path $appPath -OldText "./pages/NotificationRouting" -NewText "./features/governance/notificationRouting/pages/NotificationRouting" -Label 'App NotificationRouting import'

Write-Host 'P10.2AG Admin Web Notification Routing feature move applied.'
