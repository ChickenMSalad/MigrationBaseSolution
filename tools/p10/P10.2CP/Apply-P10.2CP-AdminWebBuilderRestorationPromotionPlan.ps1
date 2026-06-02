Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $current = (Get-Location).Path
    while ($true) {
        if (Test-Path (Join-Path $current '.git')) { return $current }
        $parent = Split-Path -Parent $current
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $current) {
            throw 'Unable to locate repository root. Run from inside the MigrationBaseSolution repository.'
        }
        $current = $parent
    }
}

$repoRoot = Get-RepoRoot
$adminRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$sourceRoot = Join-Path $adminRoot 'src'
$referenceRoot = Join-Path $adminRoot 'reference\apps-migration-admin-ui\src'
$legacyAppsRoot = Join-Path $repoRoot 'apps\migration-admin-ui\src'
$docsRoot = Join-Path $repoRoot 'docs\P10'
$artifactsRoot = Join-Path $repoRoot 'artifacts\p10\P10.2CP'

if (-not (Test-Path $adminRoot)) { throw ('Admin Web root not found: {0}' -f $adminRoot) }
if (-not (Test-Path $sourceRoot)) { throw ('Admin Web src root not found: {0}' -f $sourceRoot) }

New-Item -ItemType Directory -Force -Path $docsRoot | Out-Null
New-Item -ItemType Directory -Force -Path $artifactsRoot | Out-Null

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2CP - Admin Web Builder Restoration Promotion Plan')
[void]$report.Add('')
[void]$report.Add(('Generated UTC: `{0}`' -f ([DateTime]::UtcNow.ToString('o'))))
[void]$report.Add(('Repository root: `{0}`' -f $repoRoot))
[void]$report.Add('')
[void]$report.Add('## Scope')
[void]$report.Add('')
[void]$report.Add('- Manifest Builder')
[void]$report.Add('- Taxonomy Builder')
[void]$report.Add('- Mapping Builder')
[void]$report.Add('')
[void]$report.Add('This is report-only. No source files were changed.')
[void]$report.Add('')

$roots = New-Object 'System.Collections.Generic.List[object]'
[void]$roots.Add([pscustomobject]@{ Name = 'canonical-admin-src'; Path = $sourceRoot })
if (Test-Path $referenceRoot) { [void]$roots.Add([pscustomobject]@{ Name = 'admin-reference'; Path = $referenceRoot }) }
if (Test-Path $legacyAppsRoot) { [void]$roots.Add([pscustomobject]@{ Name = 'legacy-apps'; Path = $legacyAppsRoot }) }

$builderSpecs = @(
    [pscustomobject]@{ Name = 'Manifest Builder'; Slug = 'manifest-builder'; Terms = @('manifestbuilder','manifest-builder','manifest builder','manifest') },
    [pscustomobject]@{ Name = 'Taxonomy Builder'; Slug = 'taxonomy-builder'; Terms = @('taxonomybuilder','taxonomy-builder','taxonomy builder','taxonomy') },
    [pscustomobject]@{ Name = 'Mapping Builder'; Slug = 'mapping-builder'; Terms = @('mappingbuilder','mapping-builder','mapping builder','mapping') }
)

$allRows = New-Object 'System.Collections.Generic.List[object]'

foreach ($spec in $builderSpecs) {
    [void]$report.Add(('## {0}' -f $spec.Name))
    [void]$report.Add('')

    $rows = New-Object 'System.Collections.Generic.List[object]'

    foreach ($root in $roots) {
        $files = @(Get-ChildItem -Path $root.Path -Recurse -File -Include *.ts,*.tsx,*.js,*.jsx,*.json,*.md -ErrorAction SilentlyContinue)
        foreach ($file in $files) {
            $relative = $file.FullName.Substring($root.Path.Length).TrimStart('\','/')
            $haystack = ($relative + ' ' + $file.BaseName).ToLowerInvariant()
            $matched = $false
            foreach ($term in $spec.Terms) {
                if ($haystack.Contains($term.ToLowerInvariant())) { $matched = $true }
            }
            if ($matched) {
                $row = [pscustomobject]@{
                    Builder = $spec.Name
                    Slug = $spec.Slug
                    Root = $root.Name
                    RelativePath = $relative
                    FullPath = $file.FullName
                }
                [void]$rows.Add($row)
                [void]$allRows.Add($row)
            }
        }
    }

    if ($rows.Count -eq 0) {
        [void]$report.Add('No local file/path candidates found by name for this builder.')
        [void]$report.Add('')
    }
    else {
        [void]$report.Add('| Root | Relative path |')
        [void]$report.Add('| --- | --- |')
        foreach ($row in $rows) {
            [void]$report.Add(('| {0} | `{1}` |' -f $row.Root, $row.RelativePath))
        }
        [void]$report.Add('')
    }

    $canonicalRows = @($rows | Where-Object { $_.Root -eq 'canonical-admin-src' })
    $referenceRows = @($rows | Where-Object { $_.Root -ne 'canonical-admin-src' })
    if ($canonicalRows.Length -gt 0) {
        [void]$report.Add('Promotion posture: canonical candidate exists. Next set can safely evaluate route/nav restoration against the canonical file.')
    }
    elseif ($referenceRows.Length -gt 0) {
        [void]$report.Add('Promotion posture: reference-only candidate exists. Next set should copy/promote from reference into a canonical builder feature folder, then build-gate it.')
    }
    else {
        [void]$report.Add('Promotion posture: no candidate found. Next set should inspect backend/API/docs before creating new UI surface.')
    }
    [void]$report.Add('')
}

$appPath = Join-Path $sourceRoot 'App.tsx'
$layoutPath = Join-Path $sourceRoot 'components\Layout.tsx'
[void]$report.Add('## Route and navigation checks')
[void]$report.Add('')

foreach ($target in @($appPath, $layoutPath)) {
    if (Test-Path $target) {
        [void]$report.Add(('### `{0}`' -f ($target.Substring($repoRoot.Length).TrimStart('\','/'))))
        $content = Get-Content -Raw -Path $target
        foreach ($spec in $builderSpecs) {
            $termsFound = New-Object 'System.Collections.Generic.List[string]'
            foreach ($term in $spec.Terms) {
                if ($content.ToLowerInvariant().Contains($term.ToLowerInvariant())) { [void]$termsFound.Add($term) }
            }
            if ($termsFound.Count -gt 0) {
                [void]$report.Add(('- {0}: referenced (`{1}`)' -f $spec.Name, ([string]::Join('`, `', $termsFound))))
            }
            else {
                [void]$report.Add(('- {0}: not referenced' -f $spec.Name))
            }
        }
        [void]$report.Add('')
    }
}

[void]$report.Add('## Recommended next step')
[void]$report.Add('')
[void]$report.Add('Use this report to create a targeted builder restoration set. Do not add placeholder routes unless a real canonical or reference page candidate exists.')

$reportPath = Join-Path $docsRoot 'P10.2CP-AdminWebBuilderRestorationPromotionPlan.Report.md'
$csvPath = Join-Path $artifactsRoot 'builder-restoration-candidates.csv'
$report | Set-Content -Path $reportPath -Encoding UTF8
$allRows | Export-Csv -Path $csvPath -NoTypeInformation -Encoding UTF8

Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host ('Wrote candidate CSV: {0}' -f $csvPath)
Write-Host 'P10.2CP Admin Web builder restoration promotion plan applied.'
