Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $scriptRoot = $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
        $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    }

    $current = Resolve-Path -Path $scriptRoot
    while ($null -ne $current) {
        $candidate = Join-Path -Path $current.Path -ChildPath 'MigrationBaseSolution.sln'
        if (Test-Path -Path $candidate -PathType Leaf) {
            return $current.Path
        }

        $parent = Split-Path -Path $current.Path -Parent
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $current.Path) {
            break
        }

        $current = Resolve-Path -Path $parent
    }

    throw 'Could not locate repository root containing MigrationBaseSolution.sln.'
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

function Read-TextFile {
    param([Parameter(Mandatory = $true)][string] $Path)
    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Required file was not found: {0}' -f $Path)
    }

    return [System.IO.File]::ReadAllText($Path)
}

function Assert-LeafExists {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Label
    )

    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Missing {0}: {1}' -f $Label, $Path)
    }

    Write-Host ('Verified {0}: {1}' -f $Label, $Path)
}

function Assert-LeafMissing {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Label
    )

    if (Test-Path -Path $Path -PathType Leaf) {
        throw ('Unexpected remaining flat {0}: {1}' -f $Label, $Path)
    }

    Write-Host ('Verified flat {0} removed: {1}' -f $Label, $Path)
}

function Assert-Contains {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Needle,
        [Parameter(Mandatory = $true)][string] $Label
    )

    $content = Read-TextFile -Path $Path
    if ($content.IndexOf($Needle, [System.StringComparison]::Ordinal) -lt 0) {
        throw ('Expected {0} in {1}. Missing text: {2}' -f $Label, $Path, $Needle)
    }

    Write-Host ('Verified {0}' -f $Label)
}

function Assert-NotContains {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Needle,
        [Parameter(Mandatory = $true)][string] $Label
    )

    $content = Read-TextFile -Path $Path
    if ($content.IndexOf($Needle, [System.StringComparison]::Ordinal) -ge 0) {
        throw ('Unexpected {0} in {1}. Text: {2}' -f $Label, $Path, $Needle)
    }

    Write-Host ('Verified absence of {0}' -f $Label)
}

$repoRoot = Get-RepoRoot
$webSrc = Join-RepoPath -Root $repoRoot -Segments @('src','Admin','Migration.Admin.Web','src')

$pageSource = Join-RepoPath -Root $webSrc -Segments @('pages','AuditTrail.tsx')
$apiSource = Join-RepoPath -Root $webSrc -Segments @('api','auditTrailApi.ts')
$typeSource = Join-RepoPath -Root $webSrc -Segments @('types','auditTrail.ts')
$pageTarget = Join-RepoPath -Root $webSrc -Segments @('features','governance','auditTrail','pages','AuditTrail.tsx')
$apiTarget = Join-RepoPath -Root $webSrc -Segments @('features','governance','auditTrail','api','auditTrailApi.ts')
$typeTarget = Join-RepoPath -Root $webSrc -Segments @('features','governance','auditTrail','types','auditTrail.ts')
$appPath = Join-RepoPath -Root $webSrc -Segments @('App.tsx')

Assert-LeafExists -Path $pageTarget -Label 'Audit Trail feature page'
Assert-LeafExists -Path $apiTarget -Label 'Audit Trail feature API'
Assert-LeafExists -Path $typeTarget -Label 'Audit Trail feature types'
Assert-LeafMissing -Path $pageSource -Label 'Audit Trail page'
Assert-LeafMissing -Path $apiSource -Label 'Audit Trail API'
Assert-LeafMissing -Path $typeSource -Label 'Audit Trail types'
Assert-Contains -Path $apiTarget -Needle "../../../../api/core/adminApiClient" -Label 'feature API admin client relative import'
Assert-Contains -Path $pageTarget -Needle "../api/auditTrailApi" -Label 'feature page API import'
Assert-Contains -Path $pageTarget -Needle "../types/auditTrail" -Label 'feature page type import'
Assert-Contains -Path $appPath -Needle "./features/governance/auditTrail/pages/AuditTrail" -Label 'App.tsx feature import'
Assert-NotContains -Path $appPath -Needle "./pages/AuditTrail" -Label 'old App.tsx Audit Trail import'

Write-Host 'P10.2AH validation passed.'
