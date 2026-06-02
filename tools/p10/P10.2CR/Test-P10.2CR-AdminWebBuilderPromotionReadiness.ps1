
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = (Resolve-Path -LiteralPath $PSScriptRoot).Path
$repoRoot = $scriptRoot
while ($true) {
    if (Test-Path -LiteralPath (Join-Path $repoRoot '.git')) { break }
    $parent = Split-Path -Parent $repoRoot
    if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $repoRoot) { throw 'Unable to locate repository root.' }
    $repoRoot = $parent
}

$adminRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2CR-AdminWebBuilderPromotionReadiness.Report.md'
$applyScript = Join-Path $repoRoot 'tools\p10\P10.2CR\Apply-P10.2CR-AdminWebBuilderPromotionReadiness.ps1'

if (-not (Test-Path -LiteralPath $adminRoot)) { throw ('Admin Web root missing: {0}' -f $adminRoot) }
if (-not (Test-Path -LiteralPath $applyScript)) { throw ('Apply script missing: {0}' -f $applyScript) }
if (-not (Test-Path -LiteralPath $reportPath)) { throw ('Expected report missing: {0}' -f $reportPath) }

$reportText = [System.IO.File]::ReadAllText($reportPath)
$requiredText = @('Manifest Builder','Taxonomy Builder','Mapping Builder','Canonical candidates','Reference candidates','Recommended next action')
foreach ($text in $requiredText) {
    if (-not $reportText.Contains($text)) { throw ('Report missing required text: {0}' -f $text) }
}

$applyText = [System.IO.File]::ReadAllText($applyScript)
if ($applyText.Contains('return @($matches)')) { throw 'Apply script contains the failed regex collection-return pattern.' }
if ($applyText.Contains('Normalize-ImportLine')) { throw 'Apply script contains a line-normalization helper pattern that has failed before.' }
if ($applyText.Contains('Add-ReportText')) { throw 'Apply script contains the failed Add-ReportText helper pattern.' }

Write-Host 'P10.2CR Admin Web builder promotion readiness validation passed.'
