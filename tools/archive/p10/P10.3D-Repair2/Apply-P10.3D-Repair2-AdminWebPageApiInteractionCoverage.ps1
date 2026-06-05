Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..\..')
$docsRoot = Join-Path $repoRoot 'docs\P10'
if (-not (Test-Path $docsRoot)) {
    New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null
}

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.3D Repair2 - Admin Web Page API Interaction Coverage')
[void]$report.Add('')
[void]$report.Add('Repairs the P10.3D Repair coverage runner/test parse issue caused by fragile regex quoting.')
[void]$report.Add('')
[void]$report.Add('Behavior:')
[void]$report.Add('- Uses simple string scanning for `/api/` candidates instead of regex quote parsing.')
[void]$report.Add('- Classifies 405 responses for action/probe-style endpoints as verb-mismatch evidence, not acceptance failures.')
[void]$report.Add('- Keeps bounded request timeouts and writes summary/details artifacts.')
[void]$report.Add('- Does not change Admin Web or Admin API source files.')

$reportPath = Join-Path $docsRoot 'P10.3D-Repair2-AdminWebPageApiInteractionCoverage.md'
Set-Content -Path $reportPath -Value $report.ToArray() -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.3D Repair2 Admin Web page API interaction coverage applied.'
