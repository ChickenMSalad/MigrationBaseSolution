Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $current = $PSScriptRoot
    while ($null -ne $current -and $current -ne '') {
        $candidate = Join-Path -Path $current -ChildPath 'MigrationBaseSolution.sln'
        if (Test-Path -Path $candidate -PathType Leaf) {
            return $current
        }

        $parent = Split-Path -Path $current -Parent
        if ($parent -eq $current) {
            break
        }

        $current = $parent
    }

    throw 'Could not locate repository root by finding MigrationBaseSolution.sln.'
}

function Join-Many {
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

function Assert-FileExists {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label
    )

    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Missing expected file for {0}: {1}' -f $Label, $Path)
    }

    Write-Host ('Verified {0}: {1}' -f $Label, $Path)
}

function Assert-FileMissing {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label
    )

    if (Test-Path -Path $Path -PathType Leaf) {
        throw ('Unexpected legacy file still exists for {0}: {1}' -f $Label, $Path)
    }

    Write-Host ('Verified legacy file removed for {0}: {1}' -f $Label, $Path)
}

function Assert-Contains {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Text,
        [Parameter(Mandatory = $true)][string]$Label
    )

    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Missing file for content assertion: {0}' -f $Path)
    }

    $content = Get-Content -Path $Path -Raw
    if (-not $content.Contains($Text)) {
        throw ('Missing expected text for {0}: {1}' -f $Label, $Text)
    }

    Write-Host ('Verified content for {0}: {1}' -f $Label, $Path)
}

function Assert-NotContains {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Text,
        [Parameter(Mandatory = $true)][string]$Label
    )

    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Missing file for content assertion: {0}' -f $Path)
    }

    $content = Get-Content -Path $Path -Raw
    if ($content.Contains($Text)) {
        throw ('Unexpected legacy text remains for {0}: {1}' -f $Label, $Text)
    }

    Write-Host ('Verified legacy text absent for {0}: {1}' -f $Label, $Path)
}

$repoRoot = Get-RepoRoot
$adminSrc = Join-Many -Root $repoRoot -Segments @('src', 'Admin', 'Migration.Admin.Web', 'src')
$appPath = Join-Path -Path $adminSrc -ChildPath 'App.tsx'
$featureRoot = Join-Many -Root $adminSrc -Segments @('features', 'governance', 'notificationRouting')

$targetFiles = @(
    [pscustomobject]@{
        Label = 'NotificationRouting page'
        Path = Join-Many -Root $featureRoot -Segments @('pages', 'NotificationRouting.tsx')
    },
    [pscustomobject]@{
        Label = 'NotificationRouting API'
        Path = Join-Many -Root $featureRoot -Segments @('api', 'notificationRoutingApi.ts')
    },
    [pscustomobject]@{
        Label = 'NotificationRouting types'
        Path = Join-Many -Root $featureRoot -Segments @('types', 'notificationRouting.ts')
    }
)

$legacyFiles = @(
    [pscustomobject]@{
        Label = 'NotificationRouting page'
        Path = Join-Many -Root $adminSrc -Segments @('pages', 'NotificationRouting.tsx')
    },
    [pscustomobject]@{
        Label = 'NotificationRouting API'
        Path = Join-Many -Root $adminSrc -Segments @('api', 'notificationRoutingApi.ts')
    },
    [pscustomobject]@{
        Label = 'NotificationRouting types'
        Path = Join-Many -Root $adminSrc -Segments @('types', 'notificationRouting.ts')
    }
)

foreach ($item in $targetFiles) {
    Assert-FileExists -Path $item.Path -Label $item.Label
}

foreach ($item in $legacyFiles) {
    Assert-FileMissing -Path $item.Path -Label $item.Label
}

Assert-Contains -Path $appPath -Text './features/governance/notificationRouting/pages/NotificationRouting' -Label 'App NotificationRouting feature import'
Assert-NotContains -Path $appPath -Text './pages/NotificationRouting' -Label 'App NotificationRouting legacy import'

Write-Host 'P10.2AG repair validation passed.'
