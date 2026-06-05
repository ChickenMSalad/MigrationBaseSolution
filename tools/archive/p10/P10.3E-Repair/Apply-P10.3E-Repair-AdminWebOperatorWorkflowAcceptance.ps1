Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..\..')
$toolRoot = Join-Path $repoRoot 'tools\p10\P10.3E-Repair'
$docsRoot = Join-Path $repoRoot 'docs\P10'

if (-not (Test-Path -LiteralPath $toolRoot)) {
    throw ('Tool root was not found: {0}' -f $toolRoot)
}

if (-not (Test-Path -LiteralPath $docsRoot)) {
    New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null
}

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.3E Repair - Admin Web Operator Workflow Acceptance')
[void]$report.Add('')
[void]$report.Add('This repair keeps the P10.3E operator workflow harness focused on runtime acceptance.')
[void]$report.Add('')
[void]$report.Add('## Changes')
[void]$report.Add('')
[void]$report.Add('- Replaces the optional P10.3E runner with a local-dev HTTPS aware version.')
[void]$report.Add('- Enables TLS 1.2 and allows localhost development certificates for smoke probes.')
[void]$report.Add('- Keeps Admin Web route probes and Admin API core operator probes bounded by timeout.')
[void]$report.Add('- Writes summary and detail evidence under artifacts/p10/P10.3E-Repair.')
[void]$report.Add('')
[void]$report.Add('## Non-goals')
[void]$report.Add('')
[void]$report.Add('- No Admin Web source changes.')
[void]$report.Add('- No backend source changes.')
[void]$report.Add('- No missing feature restoration.')
[void]$report.Add('- No generated TypeScript or TSX payloads.')

$reportPath = Join-Path $docsRoot 'P10.3E-Repair-AdminWebOperatorWorkflowAcceptance.md'
Set-Content -LiteralPath $reportPath -Value $report.ToArray() -Encoding UTF8

Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.3E Repair Admin Web operator workflow acceptance applied.'
