Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$sourceRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web\src'
$appPath = Join-Path $sourceRoot 'App.tsx'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2BN-AdminWebFeatureRouteCoverageAudit.Report.md'

if (-not (Test-Path -Path $appPath -PathType Leaf)) {
    throw ('App.tsx was not found: {0}' -f $appPath)
}
if (-not (Test-Path -Path $reportPath -PathType Leaf)) {
    throw ('Expected report was not found: {0}' -f $reportPath)
}

$appContent = Get-Content -Path $appPath -Raw
$reportContent = Get-Content -Path $reportPath -Raw

if ([string]::IsNullOrWhiteSpace($reportContent)) {
    throw ('Report was empty: {0}' -f $reportPath)
}

$requiredSections = @(
    '# P10.2BN - Admin Web Feature Route Coverage Audit',
    '## Counts',
    '## Duplicate route paths',
    '## Imports ending in .tsx',
    '## Feature page files not directly routed by component name',
    '## Imported page components not used by routes'
)
foreach ($section in $requiredSections) {
    if ($reportContent.IndexOf($section, [System.StringComparison]::Ordinal) -lt 0) {
        throw ('Expected report section was missing: {0}' -f $section)
    }
}

$badImportMatches = @([regex]::Matches($appContent, 'from\s+[''\"][^''\"]+\.tsx[''\"]'))
if ($badImportMatches.Length -gt 0) {
    throw 'App.tsx contains one or more imports ending in .tsx.'
}

$referenceImportMatches = @([regex]::Matches($appContent, 'from\s+[''\"][^''\"]*(reference|apps)[^''\"]*[''\"]'))
if ($referenceImportMatches.Length -gt 0) {
    throw 'App.tsx contains one or more imports from reference/apps paths.'
}

Write-Host 'P10.2BN Admin Web feature route coverage audit validation passed.'
