Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..\..')
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$docsRoot = Join-Path $repoRoot 'docs\P10'
$reportPath = Join-Path $docsRoot 'P10.3E-AdminWebOperatorWorkflowAcceptance.md'
$runScript = Join-Path $PSScriptRoot 'Run-P10.3E-AdminWebOperatorWorkflowAcceptance.ps1'

if (-not (Test-Path -LiteralPath $adminWebRoot -PathType Container)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}

if (-not (Test-Path -LiteralPath $docsRoot -PathType Container)) {
    New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null
}

$lines = New-Object 'System.Collections.Generic.List[string]'
[void]$lines.Add('# P10.3E - Admin Web Operator Workflow Acceptance')
[void]$lines.Add('')
[void]$lines.Add('Adds a bounded local operator workflow acceptance runner for the canonical Admin Web.')
[void]$lines.Add('')
[void]$lines.Add('## Scope')
[void]$lines.Add('')
[void]$lines.Add('- No Admin Web source rewrites.')
[void]$lines.Add('- No backend source rewrites.')
[void]$lines.Add('- No feature restoration or old-commit parity work.')
[void]$lines.Add('- Probes current runtime routes and core operator API reads only.')
[void]$lines.Add('')
[void]$lines.Add('## Runner')
[void]$lines.Add('')
[void]$lines.Add('```powershell')
[void]$lines.Add('powershell -ExecutionPolicy Bypass -File .\tools\p10\P10.3E\Run-P10.3E-AdminWebOperatorWorkflowAcceptance.ps1 -AdminWebBaseUrl "http://localhost:5173" -AdminApiBaseUrl "https://localhost:55436"')
[void]$lines.Add('```')
[void]$lines.Add('')
[void]$lines.Add('## Expected evidence')
[void]$lines.Add('')
[void]$lines.Add('- `artifacts\p10\P10.3E\operator-workflow-acceptance.summary.md`')
[void]$lines.Add('- `artifacts\p10\P10.3E\operator-workflow-acceptance.details.csv`')
[void]$lines.Add('')
[void]$lines.Add(('Applied UTC: `{0}`' -f ([DateTime]::UtcNow.ToString('o'))))

Set-Content -LiteralPath $reportPath -Value $lines.ToArray() -Encoding UTF8

if (-not (Test-Path -LiteralPath $runScript -PathType Leaf)) {
    throw ('Runtime acceptance runner missing: {0}' -f $runScript)
}

Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.3E Admin Web operator workflow acceptance applied.'
