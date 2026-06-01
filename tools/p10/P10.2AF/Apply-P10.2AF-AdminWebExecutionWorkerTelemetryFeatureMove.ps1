Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $scriptRoot = $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
        $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    }

    $candidate = Resolve-Path -LiteralPath (Join-Path $scriptRoot '..\..\..')
    return $candidate.ProviderPath
}

function Join-Parts {
    param(
        [Parameter(Mandatory = $true)][string] $Root,
        [Parameter(Mandatory = $true)][string[]] $Parts
    )

    $current = $Root
    foreach ($part in $Parts) {
        $current = Join-Path -Path $current -ChildPath $part
    }

    return $current
}

function Ensure-Directory {
    param([Parameter(Mandatory = $true)][string] $Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
    }
}

function Move-RequiredFile {
    param(
        [Parameter(Mandatory = $true)][string] $Source,
        [Parameter(Mandatory = $true)][string] $Destination
    )

    if (Test-Path -LiteralPath $Destination -PathType Leaf) {
        Write-Host "Already present: $Destination"
        return
    }

    if (-not (Test-Path -LiteralPath $Source -PathType Leaf)) {
        throw "Required source file was not found: $Source"
    }

    $destinationDirectory = Split-Path -Parent $Destination
    Ensure-Directory -Path $destinationDirectory
    Move-Item -LiteralPath $Source -Destination $Destination
    Write-Host "Moved: $Source -> $Destination"
}

function Replace-InFile {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $OldValue,
        [Parameter(Mandatory = $true)][string] $NewValue
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Required file was not found: $Path"
    }

    $content = Get-Content -LiteralPath $Path -Raw
    if ($content.Contains($OldValue)) {
        $content = $content.Replace($OldValue, $NewValue)
        Set-Content -LiteralPath $Path -Value $content -NoNewline
        Write-Host "Updated: $Path"
    }
}

$repoRoot = Get-RepoRoot
$adminSrc = Join-Parts -Root $repoRoot -Parts @('src','Admin','Migration.Admin.Web','src')

$pageSource = Join-Parts -Root $adminSrc -Parts @('pages','ExecutionWorkerTelemetry.tsx')
$apiSource = Join-Parts -Root $adminSrc -Parts @('api','executionWorkerTelemetryApi.ts')
$typeSource = Join-Parts -Root $adminSrc -Parts @('types','executionWorkerTelemetry.ts')

$featureRoot = Join-Parts -Root $adminSrc -Parts @('features','operations','executionWorkerTelemetry')
$pageTarget = Join-Parts -Root $featureRoot -Parts @('pages','ExecutionWorkerTelemetry.tsx')
$apiTarget = Join-Parts -Root $featureRoot -Parts @('api','executionWorkerTelemetryApi.ts')
$typeTarget = Join-Parts -Root $featureRoot -Parts @('types','executionWorkerTelemetry.ts')
$appPath = Join-Parts -Root $adminSrc -Parts @('App.tsx')

$requiredBeforeMove = @(
    [pscustomobject]@{ Source = $pageSource; Target = $pageTarget },
    [pscustomobject]@{ Source = $apiSource; Target = $apiTarget },
    [pscustomobject]@{ Source = $typeSource; Target = $typeTarget }
)

foreach ($item in $requiredBeforeMove) {
    $sourceExists = Test-Path -LiteralPath $item.Source -PathType Leaf
    $targetExists = Test-Path -LiteralPath $item.Target -PathType Leaf
    if ((-not $sourceExists) -and (-not $targetExists)) {
        throw "Neither source nor target exists for required file: $($item.Source)"
    }
}

Ensure-Directory -Path (Join-Parts -Root $featureRoot -Parts @('pages'))
Ensure-Directory -Path (Join-Parts -Root $featureRoot -Parts @('api'))
Ensure-Directory -Path (Join-Parts -Root $featureRoot -Parts @('types'))

Move-RequiredFile -Source $pageSource -Destination $pageTarget
Move-RequiredFile -Source $apiSource -Destination $apiTarget
Move-RequiredFile -Source $typeSource -Destination $typeTarget

Replace-InFile -Path $apiTarget -OldValue 'from "./core/adminApiClient"' -NewValue 'from "../../../../api/core/adminApiClient"'
Replace-InFile -Path $appPath -OldValue 'from "./pages/ExecutionWorkerTelemetry"' -NewValue 'from "./features/operations/executionWorkerTelemetry/pages/ExecutionWorkerTelemetry"'

Write-Host 'P10.2AF Admin Web Execution Worker Telemetry feature move applied.'
