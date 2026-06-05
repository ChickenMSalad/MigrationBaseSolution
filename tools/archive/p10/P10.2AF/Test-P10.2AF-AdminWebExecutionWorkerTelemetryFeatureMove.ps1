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

function Assert-FileExists {
    param([Parameter(Mandatory = $true)][string] $Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Expected file was not found: $Path"
    }
}

function Assert-FileMissing {
    param([Parameter(Mandatory = $true)][string] $Path)

    if (Test-Path -LiteralPath $Path -PathType Leaf) {
        throw "File should have been moved away but still exists: $Path"
    }
}

function Assert-FileContains {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Text
    )

    Assert-FileExists -Path $Path
    $content = Get-Content -LiteralPath $Path -Raw
    if (-not $content.Contains($Text)) {
        throw "Expected text was not found in $Path : $Text"
    }
}

function Assert-FileDoesNotContain {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Text
    )

    Assert-FileExists -Path $Path
    $content = Get-Content -LiteralPath $Path -Raw
    if ($content.Contains($Text)) {
        throw "Unexpected text was found in $Path : $Text"
    }
}

$repoRoot = Get-RepoRoot
$adminSrc = Join-Parts -Root $repoRoot -Parts @('src','Admin','Migration.Admin.Web','src')
$featureRoot = Join-Parts -Root $adminSrc -Parts @('features','operations','executionWorkerTelemetry')

$pageTarget = Join-Parts -Root $featureRoot -Parts @('pages','ExecutionWorkerTelemetry.tsx')
$apiTarget = Join-Parts -Root $featureRoot -Parts @('api','executionWorkerTelemetryApi.ts')
$typeTarget = Join-Parts -Root $featureRoot -Parts @('types','executionWorkerTelemetry.ts')
$appPath = Join-Parts -Root $adminSrc -Parts @('App.tsx')

Assert-FileExists -Path $pageTarget
Assert-FileExists -Path $apiTarget
Assert-FileExists -Path $typeTarget

Assert-FileMissing -Path (Join-Parts -Root $adminSrc -Parts @('pages','ExecutionWorkerTelemetry.tsx'))
Assert-FileMissing -Path (Join-Parts -Root $adminSrc -Parts @('api','executionWorkerTelemetryApi.ts'))
Assert-FileMissing -Path (Join-Parts -Root $adminSrc -Parts @('types','executionWorkerTelemetry.ts'))

Assert-FileContains -Path $appPath -Text 'from "./features/operations/executionWorkerTelemetry/pages/ExecutionWorkerTelemetry"'
Assert-FileDoesNotContain -Path $appPath -Text 'from "./pages/ExecutionWorkerTelemetry"'
Assert-FileContains -Path $apiTarget -Text 'from "../../../../api/core/adminApiClient"'
Assert-FileContains -Path $pageTarget -Text 'from "../api/executionWorkerTelemetryApi"'
Assert-FileContains -Path $pageTarget -Text 'from "../types/executionWorkerTelemetry"'

Write-Host 'P10.2AF Admin Web Execution Worker Telemetry feature move validation passed.'
