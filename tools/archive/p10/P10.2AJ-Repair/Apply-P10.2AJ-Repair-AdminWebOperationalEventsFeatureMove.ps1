Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $scriptRoot = $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
        $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    }

    $root = Resolve-Path (Join-Path $scriptRoot '..\..\..')
    return $root.Path
}

function Join-RepoPath {
    param(
        [Parameter(Mandatory = $true)][string] $Root,
        [Parameter(Mandatory = $true)][string[]] $Segments
    )

    $result = $Root
    foreach ($segment in $Segments) {
        $result = [System.IO.Path]::Combine($result, $segment)
    }

    return $result
}

function Assert-Leaf {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Label
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw ('Required {0} file was not found: {1}' -f $Label, $Path)
    }
}

function Ensure-Directory {
    param([Parameter(Mandatory = $true)][string] $Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        New-Item -ItemType Directory -Path $Path | Out-Null
    }
}

function Move-LeafIfNeeded {
    param(
        [Parameter(Mandatory = $true)][string] $Source,
        [Parameter(Mandatory = $true)][string] $Destination,
        [Parameter(Mandatory = $true)][string] $Label
    )

    if (Test-Path -LiteralPath $Destination -PathType Leaf) {
        Write-Host ('Already moved {0}: {1}' -f $Label, $Destination)
        return
    }

    Assert-Leaf -Path $Source -Label $Label
    $destinationDirectory = Split-Path -Parent $Destination
    Ensure-Directory -Path $destinationDirectory
    Move-Item -LiteralPath $Source -Destination $Destination
    Write-Host ('Moved {0}: {1}' -f $Label, $Destination)
}

function Replace-TextIfPresent {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $OldText,
        [Parameter(Mandatory = $true)][string] $NewText,
        [Parameter(Mandatory = $true)][string] $Label
    )

    Assert-Leaf -Path $Path -Label $Label
    $content = Get-Content -LiteralPath $Path -Raw
    if ($content.Contains($OldText)) {
        $content = $content.Replace($OldText, $NewText)
        Set-Content -LiteralPath $Path -Value $content -Encoding UTF8
        Write-Host ('Updated {0}: {1}' -f $Label, $Path)
        return
    }

    Write-Host ('No update needed for {0}: {1}' -f $Label, $Path)
}

function Ensure-AppImport {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $OldImport,
        [Parameter(Mandatory = $true)][string] $NewImport,
        [Parameter(Mandatory = $true)][string] $AnchorImport
    )

    Assert-Leaf -Path $Path -Label 'App.tsx'
    $content = Get-Content -LiteralPath $Path -Raw

    if ($content.Contains($NewImport)) {
        Write-Host ('Already updated App.tsx Operational Events import: {0}' -f $Path)
        return
    }

    if ($content.Contains($OldImport)) {
        $content = $content.Replace($OldImport, $NewImport)
        Set-Content -LiteralPath $Path -Value $content -Encoding UTF8
        Write-Host ('Replaced App.tsx Operational Events import: {0}' -f $Path)
        return
    }

    if ($content.Contains($AnchorImport)) {
        $replacement = $AnchorImport + ' ' + $NewImport
        $content = $content.Replace($AnchorImport, $replacement)
        Set-Content -LiteralPath $Path -Value $content -Encoding UTF8
        Write-Host ('Inserted App.tsx Operational Events import after anchor: {0}' -f $Path)
        return
    }

    throw ('Unable to update App.tsx Operational Events import; neither the old import nor the Command Center anchor was found in {0}' -f $Path)
}

$repoRoot = Get-RepoRoot
$adminSrc = Join-RepoPath -Root $repoRoot -Segments @('src','Admin','Migration.Admin.Web','src')
$appFile = Join-RepoPath -Root $adminSrc -Segments @('App.tsx')

$pageSource = Join-RepoPath -Root $adminSrc -Segments @('pages','OperationalEvents.tsx')
$apiSource = Join-RepoPath -Root $adminSrc -Segments @('api','operationalEventsApi.ts')
$typeSource = Join-RepoPath -Root $adminSrc -Segments @('types','operationalEvents.ts')

$featureRoot = Join-RepoPath -Root $adminSrc -Segments @('features','operations','operationalEvents')
$pageDestination = Join-RepoPath -Root $featureRoot -Segments @('pages','OperationalEvents.tsx')
$apiDestination = Join-RepoPath -Root $featureRoot -Segments @('api','operationalEventsApi.ts')
$typeDestination = Join-RepoPath -Root $featureRoot -Segments @('types','operationalEvents.ts')

Assert-Leaf -Path $appFile -Label 'App.tsx'

$moveItems = @(
    [pscustomobject]@{ Label = 'Operational Events page'; Source = $pageSource; Destination = $pageDestination },
    [pscustomobject]@{ Label = 'Operational Events API'; Source = $apiSource; Destination = $apiDestination },
    [pscustomobject]@{ Label = 'Operational Events types'; Source = $typeSource; Destination = $typeDestination }
)

foreach ($item in $moveItems) {
    $sourceExists = Test-Path -LiteralPath $item.Source -PathType Leaf
    $destinationExists = Test-Path -LiteralPath $item.Destination -PathType Leaf
    if (-not $sourceExists -and -not $destinationExists) {
        throw ('Required {0} source or destination file was not found. Source: {1} Destination: {2}' -f $item.Label, $item.Source, $item.Destination)
    }
}

foreach ($item in $moveItems) {
    Move-LeafIfNeeded -Source $item.Source -Destination $item.Destination -Label $item.Label
}

Replace-TextIfPresent -Path $pageDestination -OldText '"../../api/operationalEventsApi"' -NewText '"../api/operationalEventsApi"' -Label 'Operational Events page API import'
Replace-TextIfPresent -Path $pageDestination -OldText '"../../types/operationalEvents"' -NewText '"../types/operationalEvents"' -Label 'Operational Events page type import'
Replace-TextIfPresent -Path $apiDestination -OldText '"../../types/operationalEvents"' -NewText '"../types/operationalEvents"' -Label 'Operational Events API type import'
Replace-TextIfPresent -Path $apiDestination -OldText '"./client"' -NewText '"../../../../api/client"' -Label 'Operational Events API client import'
Replace-TextIfPresent -Path $apiDestination -OldText '"../client"' -NewText '"../../../../api/client"' -Label 'Operational Events API parent client import'

$oldImport = 'import { OperationalEvents } from "./pages/OperationalEvents";'
$newImport = 'import { OperationalEvents } from "./features/operations/operationalEvents/pages/OperationalEvents";'
$anchorImport = 'import { CommandCenter } from "./features/operations/commandCenter/pages/CommandCenter";'
Ensure-AppImport -Path $appFile -OldImport $oldImport -NewImport $newImport -AnchorImport $anchorImport

$appContent = Get-Content -LiteralPath $appFile -Raw
if (-not $appContent.Contains($newImport)) {
    throw ('App.tsx does not contain the Operational Events feature import after repair: {0}' -f $appFile)
}

Write-Host 'P10.2AJ repair applied.'
