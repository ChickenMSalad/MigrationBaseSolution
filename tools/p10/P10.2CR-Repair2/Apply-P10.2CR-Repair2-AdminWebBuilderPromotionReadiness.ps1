Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $current = $PSScriptRoot
    while ($true) {
        if ([string]::IsNullOrWhiteSpace($current)) {
            throw 'Unable to locate repository root from script path.'
        }

        $solutionPath = Join-Path $current 'MigrationBaseSolution.sln'
        $srcPath = Join-Path $current 'src'
        if ((Test-Path -LiteralPath $solutionPath) -and (Test-Path -LiteralPath $srcPath)) {
            return $current
        }

        $parent = Split-Path -Parent $current
        if ($parent -eq $current) {
            throw 'Unable to locate repository root from script path.'
        }

        $current = $parent
    }
}

function Get-RelativePathText {
    param(
        [string]$BasePath,
        [string]$TargetPath
    )

    $baseFull = [System.IO.Path]::GetFullPath($BasePath)
    $targetFull = [System.IO.Path]::GetFullPath($TargetPath)
    $baseUri = New-Object System.Uri(($baseFull.TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar))
    $targetUri = New-Object System.Uri($targetFull)
    $relativeUri = $baseUri.MakeRelativeUri($targetUri)
    $relative = [System.Uri]::UnescapeDataString($relativeUri.ToString())
    return ($relative -replace '/', [System.IO.Path]::DirectorySeparatorChar)
}

$repoRoot = Get-RepoRoot
$adminRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$canonicalSourceRoot = Join-Path $adminRoot 'src'
$canonicalFeaturesRoot = Join-Path $canonicalSourceRoot 'features'
$canonicalPagesRoot = Join-Path $canonicalSourceRoot 'pages'
$referenceFeaturesRoot = Join-Path $adminRoot 'reference\apps-migration-admin-ui\src\features'
$referencePagesRoot = Join-Path $adminRoot 'reference\apps-migration-admin-ui\src\pages'
$legacyFeaturesRoot = Join-Path $repoRoot 'apps\migration-admin-ui\src\features'
$legacyPagesRoot = Join-Path $repoRoot 'apps\migration-admin-ui\src\pages'
$docsRoot = Join-Path $repoRoot 'docs\P10'
$reportPath = Join-Path $docsRoot 'P10.2CR-Repair2-AdminWebBuilderPromotionReadiness.md'

if (-not (Test-Path -LiteralPath $adminRoot)) {
    throw ('Admin Web root not found: {0}' -f $adminRoot)
}

if (-not (Test-Path -LiteralPath $docsRoot)) {
    New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null
}

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2CR Repair2 - Admin Web Builder Promotion Readiness')
[void]$report.Add('')
[void]$report.Add(('Generated UTC: `{0}`' -f ([DateTime]::UtcNow.ToString('o'))))
[void]$report.Add(('Repo root: `{0}`' -f $repoRoot))
[void]$report.Add(('Admin Web root: `{0}`' -f $adminRoot))
[void]$report.Add('')
[void]$report.Add('## Purpose')
[void]$report.Add('')
[void]$report.Add('Inventory Manifest, Taxonomy, and Mapping builder candidates across canonical Admin Web source, Admin Web reference source, and legacy apps source without guessing or mutating routes.')
[void]$report.Add('')
[void]$report.Add('## Candidate Roots')
[void]$report.Add('')
[void]$report.Add('| Scope | Root | Exists |')
[void]$report.Add('|---|---:|---:|')

$rootItems = New-Object 'System.Collections.Generic.List[object]'
[void]$rootItems.Add([pscustomobject]@{ Scope = 'Canonical features'; Path = $canonicalFeaturesRoot })
[void]$rootItems.Add([pscustomobject]@{ Scope = 'Canonical pages'; Path = $canonicalPagesRoot })
[void]$rootItems.Add([pscustomobject]@{ Scope = 'Reference features'; Path = $referenceFeaturesRoot })
[void]$rootItems.Add([pscustomobject]@{ Scope = 'Reference pages'; Path = $referencePagesRoot })
[void]$rootItems.Add([pscustomobject]@{ Scope = 'Legacy apps features'; Path = $legacyFeaturesRoot })
[void]$rootItems.Add([pscustomobject]@{ Scope = 'Legacy apps pages'; Path = $legacyPagesRoot })

foreach ($rootItem in $rootItems) {
    $existsText = 'No'
    if (Test-Path -LiteralPath $rootItem.Path) {
        $existsText = 'Yes'
    }
    [void]$report.Add(('| {0} | `{1}` | {2} |' -f $rootItem.Scope, $rootItem.Path, $existsText))
}

$terms = New-Object 'System.Collections.Generic.List[string]'
[void]$terms.Add('manifest')
[void]$terms.Add('taxonomy')
[void]$terms.Add('mapping')

$candidates = New-Object 'System.Collections.Generic.List[object]'

foreach ($rootItem in $rootItems) {
    if (-not (Test-Path -LiteralPath $rootItem.Path)) {
        continue
    }

    $files = Get-ChildItem -LiteralPath $rootItem.Path -Recurse -File -ErrorAction Stop |
        Where-Object {
            $ext = [System.IO.Path]::GetExtension($_.Name)
            ($ext -eq '.tsx' -or $ext -eq '.ts')
        }

    foreach ($file in $files) {
        $lowerName = $file.Name.ToLowerInvariant()
        $lowerPath = $file.FullName.ToLowerInvariant()
        $matchedTerm = $null
        foreach ($term in $terms) {
            if ($lowerName.Contains($term) -or $lowerPath.Contains($term)) {
                $matchedTerm = $term
                break
            }
        }

        if ($null -ne $matchedTerm) {
            $relativePath = Get-RelativePathText -BasePath $repoRoot -TargetPath $file.FullName
            [void]$candidates.Add([pscustomobject]@{
                Builder = $matchedTerm
                Scope = $rootItem.Scope
                RelativePath = $relativePath
                Extension = [System.IO.Path]::GetExtension($file.Name)
            })
        }
    }
}

[void]$report.Add('')
[void]$report.Add('## Builder Candidate Inventory')
[void]$report.Add('')
[void]$report.Add('| Builder | Scope | File |')
[void]$report.Add('|---|---|---|')

if ($candidates.Count -eq 0) {
    [void]$report.Add('| none | none | none |')
} else {
    foreach ($candidate in $candidates) {
        [void]$report.Add(('| {0} | {1} | `{2}` |' -f $candidate.Builder, $candidate.Scope, $candidate.RelativePath))
    }
}

[void]$report.Add('')
[void]$report.Add('## Promotion Readiness')
[void]$report.Add('')
[void]$report.Add('| Builder | Canonical TSX Count | Reference TSX Count | Legacy TSX Count | Recommendation |')
[void]$report.Add('|---|---:|---:|---:|---|')

foreach ($term in $terms) {
    $canonicalCount = 0
    $referenceCount = 0
    $legacyCount = 0

    foreach ($candidate in $candidates) {
        if ($candidate.Builder -ne $term) {
            continue
        }
        if ($candidate.Extension -ne '.tsx') {
            continue
        }
        if ($candidate.Scope.StartsWith('Canonical')) {
            $canonicalCount = $canonicalCount + 1
        } elseif ($candidate.Scope.StartsWith('Reference')) {
            $referenceCount = $referenceCount + 1
        } elseif ($candidate.Scope.StartsWith('Legacy')) {
            $legacyCount = $legacyCount + 1
        }
    }

    $recommendation = 'No candidate found; do not guess.'
    if ($canonicalCount -gt 0) {
        $recommendation = 'Canonical candidate exists; route/nav reachability can be validated or restored.'
    } elseif ($referenceCount -eq 1 -or $legacyCount -eq 1) {
        $recommendation = 'Single non-canonical candidate exists; eligible for guarded promotion in a later set.'
    } elseif ($referenceCount -gt 1 -or $legacyCount -gt 1) {
        $recommendation = 'Multiple non-canonical candidates; requires manual selection or narrower promotion rule.'
    }

    [void]$report.Add(('| {0} | {1} | {2} | {3} | {4} |' -f $term, $canonicalCount, $referenceCount, $legacyCount, $recommendation))
}

[void]$report.Add('')
[void]$report.Add('## Notes')
[void]$report.Add('')
[void]$report.Add('- This set is intentionally report-only.')
[void]$report.Add('- It does not promote files, modify App.tsx, or modify navigation.')
[void]$report.Add('- The next set should act only on single unambiguous candidates from this report.')

Set-Content -LiteralPath $reportPath -Value $report.ToArray() -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2CR Repair2 Admin Web builder promotion readiness applied.'
