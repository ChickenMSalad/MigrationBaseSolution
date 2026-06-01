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

function Write-TextFileIfChanged {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Content,
        [Parameter(Mandatory = $true)][string] $Label
    )

    $existing = Read-TextFile -Path $Path
    if ($existing -eq $Content) {
        Write-Host ('Already updated {0}: {1}' -f $Label, $Path)
        return
    }

    [System.IO.File]::WriteAllText($Path, $Content, [System.Text.UTF8Encoding]::new($false))
    Write-Host ('Updated {0}: {1}' -f $Label, $Path)
}

function Move-LeafFile {
    param(
        [Parameter(Mandatory = $true)][string] $Source,
        [Parameter(Mandatory = $true)][string] $Target,
        [Parameter(Mandatory = $true)][string] $Label
    )

    $targetExists = Test-Path -Path $Target -PathType Leaf
    $sourceExists = Test-Path -Path $Source -PathType Leaf

    if ($targetExists -and -not $sourceExists) {
        Write-Host ('Already moved {0}: {1}' -f $Label, $Target)
        return
    }

    if ($targetExists -and $sourceExists) {
        throw ('Both source and target exist for {0}. Resolve before applying. Source={1} Target={2}' -f $Label, $Source, $Target)
    }

    if (-not $sourceExists) {
        throw ('Required source file was not found for {0}: {1}' -f $Label, $Source)
    }

    $targetDirectory = Split-Path -Path $Target -Parent
    if (-not (Test-Path -Path $targetDirectory -PathType Container)) {
        New-Item -Path $targetDirectory -ItemType Directory -Force | Out-Null
    }

    Move-Item -Path $Source -Destination $Target
    Write-Host ('Moved {0}: {1}' -f $Label, $Target)
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

$items = @(
    [pscustomobject]@{ Label = 'Audit Trail page'; Source = $pageSource; Target = $pageTarget },
    [pscustomobject]@{ Label = 'Audit Trail API'; Source = $apiSource; Target = $apiTarget },
    [pscustomobject]@{ Label = 'Audit Trail types'; Source = $typeSource; Target = $typeTarget }
)

foreach ($item in $items) {
    $sourceExists = Test-Path -Path $item.Source -PathType Leaf
    $targetExists = Test-Path -Path $item.Target -PathType Leaf
    if (-not $sourceExists -and -not $targetExists) {
        throw ('Neither source nor target exists for {0}. Source={1} Target={2}' -f $item.Label, $item.Source, $item.Target)
    }
    if ($sourceExists -and $targetExists) {
        throw ('Both source and target exist for {0}. Source={1} Target={2}' -f $item.Label, $item.Source, $item.Target)
    }
}

foreach ($item in $items) {
    Move-LeafFile -Source $item.Source -Target $item.Target -Label $item.Label
}

$pageContent = Read-TextFile -Path $pageTarget
$pageContent = $pageContent.Replace("../api/auditTrailApi", "../api/auditTrailApi")
$pageContent = $pageContent.Replace("../types/auditTrail", "../types/auditTrail")
Write-TextFileIfChanged -Path $pageTarget -Content $pageContent -Label 'Audit Trail page imports'

$apiContent = Read-TextFile -Path $apiTarget
$apiContent = $apiContent.Replace("'./core/adminApiClient'", "'../../../../api/core/adminApiClient'")
$apiContent = $apiContent.Replace('"./core/adminApiClient"', '"../../../../api/core/adminApiClient"')
$apiContent = $apiContent.Replace("'../types/auditTrail'", "'../types/auditTrail'")
$apiContent = $apiContent.Replace('"../types/auditTrail"', '"../types/auditTrail"')
Write-TextFileIfChanged -Path $apiTarget -Content $apiContent -Label 'Audit Trail API imports'

$appContent = Read-TextFile -Path $appPath
$appContent = $appContent.Replace("'./pages/AuditTrail'", "'./features/governance/auditTrail/pages/AuditTrail'")
$appContent = $appContent.Replace('"./pages/AuditTrail"', '"./features/governance/auditTrail/pages/AuditTrail"')
Write-TextFileIfChanged -Path $appPath -Content $appContent -Label 'App.tsx Audit Trail import'

Write-Host 'P10.2AH apply completed.'
