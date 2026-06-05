Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $current = (Get-Location).Path
    while (-not [string]::IsNullOrWhiteSpace($current)) {
        $candidate = [System.IO.Path]::Combine($current, 'src', 'Admin', 'Migration.Admin.Web', 'src')
        if (Test-Path -Path $candidate -PathType Container) {
            return $current
        }

        $parent = [System.IO.Directory]::GetParent($current)
        if ($null -eq $parent) {
            break
        }

        $current = $parent.FullName
    }

    throw 'Unable to locate repository root. Run this script from inside MigrationBaseSolution.'
}

function Assert-LeafExists {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label
    )

    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Expected file missing for {0}: {1}' -f $Label, $Path)
    }
}

function Assert-LeafMissing {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label
    )

    if (Test-Path -Path $Path -PathType Leaf) {
        throw ('Legacy file still exists for {0}: {1}' -f $Label, $Path)
    }
}

function Assert-ImportLine {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Pattern,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $content = Get-Content -Path $Path -Raw
    if ($content -notmatch $Pattern) {
        throw ('Expected import line missing for {0}: {1}' -f $Label, $Pattern)
    }
}

function Assert-NoImportLine {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Pattern,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $content = Get-Content -Path $Path -Raw
    if ($content -match $Pattern) {
        throw ('Unexpected legacy import line found for {0}: {1}' -f $Label, $Pattern)
    }
}

$repoRoot = Get-RepoRoot
$adminSrc = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src')
$featureRoot = [System.IO.Path]::Combine($adminSrc, 'features', 'platform', 'costAnalytics')

$pageTarget = [System.IO.Path]::Combine($featureRoot, 'pages', 'CostAnalytics.tsx')
$apiTarget = [System.IO.Path]::Combine($featureRoot, 'api', 'costAnalyticsApi.ts')
$typeTarget = [System.IO.Path]::Combine($featureRoot, 'types', 'costAnalytics.ts')

Assert-LeafExists -Path $pageTarget -Label 'Cost Analytics page'
Assert-LeafExists -Path $apiTarget -Label 'Cost Analytics API'
Assert-LeafExists -Path $typeTarget -Label 'Cost Analytics types'

Assert-LeafMissing -Path ([System.IO.Path]::Combine($adminSrc, 'pages', 'CostAnalytics.tsx')) -Label 'flat Cost Analytics page'
Assert-LeafMissing -Path ([System.IO.Path]::Combine($adminSrc, 'api', 'costAnalyticsApi.ts')) -Label 'flat Cost Analytics API'
Assert-LeafMissing -Path ([System.IO.Path]::Combine($adminSrc, 'types', 'costAnalytics.ts')) -Label 'flat Cost Analytics types'

Assert-ImportLine -Path $apiTarget -Pattern 'import\s+\{\s*adminApiClient\s*\}\s+from\s+"\.\./\.\./\.\./\.\./api/core/adminApiClient"' -Label 'Cost Analytics API core client import'
Assert-NoImportLine -Path $apiTarget -Pattern 'import\s+\{\s*adminApiClient\s*\}\s+from\s+"\./core/adminApiClient"' -Label 'Cost Analytics API legacy core client import'

$appPath = [System.IO.Path]::Combine($adminSrc, 'App.tsx')
if (Test-Path -Path $appPath -PathType Leaf) {
    $appContent = Get-Content -Path $appPath -Raw
    if ($appContent -match 'CostAnalytics') {
        Assert-ImportLine -Path $appPath -Pattern 'import\s+\{\s*CostAnalytics\s*\}\s+from\s+"\./features/platform/costAnalytics/pages/CostAnalytics"' -Label 'App.tsx Cost Analytics feature import'
        Assert-NoImportLine -Path $appPath -Pattern 'import\s+\{\s*CostAnalytics\s*\}\s+from\s+"\./pages/CostAnalytics"' -Label 'App.tsx Cost Analytics legacy import'
    }
}

Write-Host 'P10.2AM Admin Web Cost Analytics feature move validation passed.'
