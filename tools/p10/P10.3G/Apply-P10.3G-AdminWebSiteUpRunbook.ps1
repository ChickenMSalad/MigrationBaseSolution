Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$docsRoot = Join-Path $repoRoot 'docs\P10'
if (-not (Test-Path -LiteralPath $docsRoot)) {
    New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null
}

$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
if (-not (Test-Path -LiteralPath $adminWebRoot)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}

$packageJson = Join-Path $adminWebRoot 'package.json'
if (-not (Test-Path -LiteralPath $packageJson)) {
    throw ('Admin Web package.json was not found: {0}' -f $packageJson)
}

$runbookPath = Join-Path $docsRoot 'P10.3G-AdminWebSiteUpRunbook.md'
$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.3G - Admin Web Site-Up Runbook')
[void]$report.Add('')
[void]$report.Add('## Scope')
[void]$report.Add('')
[void]$report.Add('This set records the current site-up path after Admin Web consolidation and runtime acceptance. It does not change Admin Web or Admin API source code.')
[void]$report.Add('')
[void]$report.Add('## Canonical local endpoints')
[void]$report.Add('')
[void]$report.Add('- Admin Web: `http://localhost:5173`')
[void]$report.Add('- Admin API: `https://localhost:55436`')
[void]$report.Add('')
[void]$report.Add('## Start sequence')
[void]$report.Add('')
[void]$report.Add('1. Start Admin API using the repo-native host command or IDE launch profile.')
[void]$report.Add('2. Start Admin Web from `src\Admin\Migration.Admin.Web` using `npm run dev`.')
[void]$report.Add('3. Keep both processes running while executing runtime acceptance scripts.')
[void]$report.Add('')
[void]$report.Add('## Verification sequence')
[void]$report.Add('')
[void]$report.Add('From repo root:')
[void]$report.Add('')
[void]$report.Add('```powershell')
[void]$report.Add('powershell -ExecutionPolicy Bypass -File .\tools\p10\P10.3A\Run-P10.3A-AdminWebRuntimeAcceptance.ps1 -AdminWebBaseUrl "http://localhost:5173" -AdminApiBaseUrl "https://localhost:55436"')
[void]$report.Add('powershell -ExecutionPolicy Bypass -File .\tools\p10\P10.3B\Run-P10.3B-AdminWebRuntimeRouteAcceptance.ps1 -AdminWebBaseUrl "http://localhost:5173"')
[void]$report.Add('powershell -ExecutionPolicy Bypass -File .\tools\p10\P10.3C\Run-P10.3C-AdminWebBrowserRuntimeHealth.ps1 -AdminWebBaseUrl "http://localhost:5173"')
[void]$report.Add('powershell -ExecutionPolicy Bypass -File .\tools\p10\P10.3E-Repair2\Run-P10.3E-Repair2-AdminWebOperatorWorkflowAcceptance.ps1 -AdminWebBaseUrl "http://localhost:5173" -AdminApiBaseUrl "https://localhost:55436"')
[void]$report.Add('```')
[void]$report.Add('')
[void]$report.Add('## Accepted P10.3 evidence gates')
[void]$report.Add('')
[void]$report.Add('- Runtime acceptance: Admin Web and Admin API reachable.')
[void]$report.Add('- Route acceptance: current Admin Web route table reachable.')
[void]$report.Add('- Browser runtime health: route and asset probes succeed.')
[void]$report.Add('- Operator workflow acceptance: current route/API smoke checks succeed.')
[void]$report.Add('')
[void]$report.Add('## Deferred work')
[void]$report.Add('')
[void]$report.Add('- Builder parity restoration from the known-good historical site commit is intentionally deferred.')
[void]$report.Add('- Do not use runtime site-up work to infer missing historical feature intent.')
[void]$report.Add('- Restore missing pages later from the known-good commit with an explicit feature inventory and route/nav/API contract map.')
[void]$report.Add('')
[void]$report.Add('## Guardrails')
[void]$report.Add('')
[void]$report.Add('- No feature recovery during site-up acceptance unless tied to a concrete runtime failure on the current route surface.')
[void]$report.Add('- No generated React or C# source in this runbook set.')
[void]$report.Add('- Keep PowerShell scripts compatible with Windows PowerShell 5.1.')
[void]$report.Add('- Prefer runtime evidence over speculative cleanup.')
[void]$report.Add('')
[void]$report.Add('## Files checked by apply')
[void]$report.Add('')
[void]$report.Add(('- Admin Web root: `{0}`' -f $adminWebRoot))
[void]$report.Add(('- package.json: `{0}`' -f $packageJson))

Set-Content -LiteralPath $runbookPath -Value $report.ToArray() -Encoding UTF8
Write-Host ('Wrote runbook: {0}' -f $runbookPath)
Write-Host 'P10.3G Admin Web site-up runbook applied.'
