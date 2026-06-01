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
        throw ('Required source file was not found: {0}' -f $Source)
    }

    $destinationDirectory = Split-Path -Path $Destination -Parent
    Ensure-Directory -Path $destinationDirectory
    Move-Item -Path $Source -Destination $Destination
    Write-Host ('Moved {0}: {1} -> {2}' -f $Label, $Source, $Destination)
}

function Update-TextFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$OldText,
        [Parameter(Mandatory = $true)][string]$NewText,
        [Parameter(Mandatory = $true)][string]$Label
    )

    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Required file was not found: {0}' -f $Path)
    }

    $content = Get-Content -Path $Path -Raw
    if ($content.Contains($NewText)) {
        Write-Host ('Already updated {0}: {1}' -f $Label, $Path)
        return
    }

    if (-not $content.Contains($OldText)) {
        throw ('Could not find expected text for {0} in {1}' -f $Label, $Path)
    }

    $updated = $content.Replace($OldText, $NewText)
    Set-Content -Path $Path -Value $updated -Encoding UTF8
    Write-Host ('Updated {0}: {1}' -f $Label, $Path)
}

$repoRoot = Get-RepoRoot
$adminSrc = Join-Many -Root $repoRoot -Segments @('src', 'Admin', 'Migration.Admin.Web', 'src')
$appPath = Join-Path -Path $adminSrc -ChildPath 'App.tsx'
$featureRoot = Join-Many -Root $adminSrc -Segments @('features', 'governance', 'notificationRouting')

$moveItems = @(
    [pscustomobject]@{
        Label = 'NotificationRouting page'
        Source = Join-Many -Root $adminSrc -Segments @('pages', 'NotificationRouting.tsx')
        Destination = Join-Many -Root $featureRoot -Segments @('pages', 'NotificationRouting.tsx')
    },
    [pscustomobject]@{
        Label = 'NotificationRouting API'
        Source = Join-Many -Root $adminSrc -Segments @('api', 'notificationRoutingApi.ts')
        Destination = Join-Many -Root $featureRoot -Segments @('api', 'notificationRoutingApi.ts')
    },
    [pscustomobject]@{
        Label = 'NotificationRouting types'
        Source = Join-Many -Root $adminSrc -Segments @('types', 'notificationRouting.ts')
        Destination = Join-Many -Root $featureRoot -Segments @('types', 'notificationRouting.ts')
    }
)

foreach ($item in $moveItems) {
    $sourceExists = Test-Path -Path $item.Source -PathType Leaf
    $destinationExists = Test-Path -Path $item.Destination -PathType Leaf
    if (-not $sourceExists -and -not $destinationExists) {
        throw ('Required file was not found at source or destination for {0}. Source: {1}. Destination: {2}' -f $item.Label, $item.Source, $item.Destination)
    }
}

foreach ($item in $moveItems) {
    Move-FeatureFile -Source $item.Source -Destination $item.Destination -Label $item.Label
}

Update-TextFile -Path $appPath -OldText './pages/NotificationRouting' -NewText './features/governance/notificationRouting/pages/NotificationRouting' -Label 'App NotificationRouting import'

Write-Host 'P10.2AG repair applied successfully.'
