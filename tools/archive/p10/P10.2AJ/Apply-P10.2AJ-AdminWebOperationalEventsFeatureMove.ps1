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

function Assert-Directory {
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
    Assert-Directory -Path $destinationDirectory
    Move-Item -LiteralPath $Source -Destination $Destination
    Write-Host ('Moved {0}: {1}' -f $Label, $Destination)
}

function Update-FileText {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $OldText,
        [Parameter(Mandatory = $true)][string] $NewText,
        [Parameter(Mandatory = $true)][string] $Label
    )

    Assert-Leaf -Path $Path -Label $Label
    $content = Get-Content -LiteralPath $Path -Raw
    if ($content.Contains($NewText)) {
        Write-Host ('Already updated {0}: {1}' -f $Label, $Path)
        return
    }

    if (-not $content.Contains($OldText)) {
        throw ('Unable to update {0}; expected text was not found in {1}' -f $Label, $Path)
    }

    $content = $content.Replace($OldText, $NewText)
    Set-Content -LiteralPath $Path -Value $content -Encoding UTF8
    Write-Host ('Updated {0}: {1}' -f $Label, $Path)
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

$items = @(
    [pscustomobject]@{ Label = 'Operational Events page'; Source = $pageSource; Destination = $pageDestination },
    [pscustomobject]@{ Label = 'Operational Events API'; Source = $apiSource; Destination = $apiDestination },
    [pscustomobject]@{ Label = 'Operational Events types'; Source = $typeSource; Destination = $typeDestination }
)

foreach ($item in $items) {
    $destinationExists = Test-Path -LiteralPath $item.Destination -PathType Leaf
    $sourceExists = Test-Path -LiteralPath $item.Source -PathType Leaf
    if (-not $destinationExists -and -not $sourceExists) {
        throw ('Required {0} source or destination file was not found. Source: {1} Destination: {2}' -f $item.Label, $item.Source, $item.Destination)
    }
}

foreach ($item in $items) {
    Move-LeafIfNeeded -Source $item.Source -Destination $item.Destination -Label $item.Label
}

Update-FileText -Path $pageDestination -OldText '"../api/operationalEventsApi"' -NewText '"../api/operationalEventsApi"' -Label 'Operational Events page API import no-op check'
Update-FileText -Path $pageDestination -OldText '"../types/operationalEvents"' -NewText '"../types/operationalEvents"' -Label 'Operational Events page type import no-op check'

$pageContent = Get-Content -LiteralPath $pageDestination -Raw
$pageContent = $pageContent.Replace('"../api/operationalEventsApi"', '"../api/operationalEventsApi"')
$pageContent = $pageContent.Replace('"../types/operationalEvents"', '"../types/operationalEvents"')
$pageContent = $pageContent.Replace('"../../api/operationalEventsApi"', '"../api/operationalEventsApi"')
$pageContent = $pageContent.Replace('"../../types/operationalEvents"', '"../types/operationalEvents"')
$pageContent = $pageContent.Replace('"../api/operationalEvents"', '"../api/operationalEvents"')
Set-Content -LiteralPath $pageDestination -Value $pageContent -Encoding UTF8

$apiContent = Get-Content -LiteralPath $apiDestination -Raw
$apiContent = $apiContent.Replace('"../types/operationalEvents"', '"../types/operationalEvents"')
$apiContent = $apiContent.Replace('"../../types/operationalEvents"', '"../types/operationalEvents"')
$apiContent = $apiContent.Replace('"./client"', '"../../../../api/client"')
$apiContent = $apiContent.Replace('"../client"', '"../../../../api/client"')
Set-Content -LiteralPath $apiDestination -Value $apiContent -Encoding UTF8

Update-FileText -Path $appFile -OldText 'import { OperationalEvents } from "./pages/OperationalEvents";' -NewText 'import { OperationalEvents } from "./features/operations/operationalEvents/pages/OperationalEvents";' -Label 'App.tsx Operational Events import'

Write-Host 'P10.2AJ Operational Events feature move applied.'
