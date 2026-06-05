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
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2CP-AdminWebBuilderRestorationPromotionPlan.Report.md'
$csvPath = Join-Path $repoRoot 'artifacts\p10\P10.2CP\builder-restoration-candidates.csv'
$applyPath = Join-Path $repoRoot 'tools\p10\P10.2CP\Apply-P10.2CP-AdminWebBuilderRestorationPromotionPlan.ps1'
$adminSrc = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web\src'

if (-not (Test-Path $applyPath)) { throw ('Apply script missing: {0}' -f $applyPath) }
if (-not (Test-Path $adminSrc)) { throw ('Admin Web src root missing: {0}' -f $adminSrc) }
if (-not (Test-Path $reportPath)) { throw ('Expected report was not found: {0}' -f $reportPath) }
if (-not (Test-Path $csvPath)) { throw ('Expected candidate CSV was not found: {0}' -f $csvPath) }

$report = Get-Content -Raw -Path $reportPath
foreach ($text in @('Manifest Builder','Taxonomy Builder','Mapping Builder','Recommended next step')) {
    if (-not $report.Contains($text)) { throw ('Report missing expected section/text: {0}' -f $text) }
}

# Verify this set is report-only with respect to Admin Web source modification intent.
$apply = Get-Content -Raw -Path $applyPath
foreach ($bad in @('Move-Item','Copy-Item','Remove-Item')) {
    if ($apply.Contains($bad)) { throw ('Apply script contains source mutation command not allowed for this report-only set: {0}' -f $bad) }
}

Write-Host 'P10.2CP Admin Web builder restoration promotion plan validation passed.'
