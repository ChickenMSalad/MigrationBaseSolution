Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..')).Path
$docsRoot = Join-Path $repoRoot 'docs\P10'
$artifactRoot = Join-Path $repoRoot 'artifacts\p10\P10.3F'

if (-not (Test-Path -LiteralPath $docsRoot)) {
    New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null
}
if (-not (Test-Path -LiteralPath $artifactRoot)) {
    New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
}

$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$packageJson = Join-Path $adminWebRoot 'package.json'
$appTsx = Join-Path $adminWebRoot 'src\App.tsx'
$layoutTsx = Join-Path $adminWebRoot 'src\components\Layout.tsx'

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.3F - Admin Web Release Readiness Evidence Gate')
[void]$report.Add('')
[void]$report.Add(('Generated UTC: `{0}`' -f ([DateTime]::UtcNow.ToString('o'))))
[void]$report.Add('')
[void]$report.Add('## Purpose')
[void]$report.Add('')
[void]$report.Add('Aggregate the P10.3 runtime acceptance evidence into a single release-readiness gate for the canonical Admin Web site-up track.')
[void]$report.Add('')
[void]$report.Add('## Local structure checks')
[void]$report.Add('')
[void]$report.Add(('- Admin Web root exists: `{0}`' -f (Test-Path -LiteralPath $adminWebRoot)))
[void]$report.Add(('- package.json exists: `{0}`' -f (Test-Path -LiteralPath $packageJson)))
[void]$report.Add(('- App.tsx exists: `{0}`' -f (Test-Path -LiteralPath $appTsx)))
[void]$report.Add(('- Layout.tsx exists: `{0}`' -f (Test-Path -LiteralPath $layoutTsx)))
[void]$report.Add('')
[void]$report.Add('## Evidence sources expected')
[void]$report.Add('')
[void]$report.Add('- P10.3A runtime acceptance')
[void]$report.Add('- P10.3B route acceptance')
[void]$report.Add('- P10.3C browser runtime health')
[void]$report.Add('- P10.3D page/API interaction coverage')
[void]$report.Add('- P10.3E operator workflow acceptance')
[void]$report.Add('')
[void]$report.Add('Run the release readiness evidence runner after the latest runtime acceptance scripts have been executed locally.')

$reportPath = Join-Path $docsRoot 'P10.3F-AdminWebReleaseReadinessEvidenceGate.md'
Set-Content -LiteralPath $reportPath -Value $report.ToArray() -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.3F Admin Web release readiness evidence gate applied.'
