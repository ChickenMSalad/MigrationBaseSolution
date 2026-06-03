Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$docsRoot = Join-Path $repoRoot 'docs\P10'
if (-not (Test-Path -LiteralPath $docsRoot)) {
    New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null
}

$reportPath = Join-Path $docsRoot 'P10.3H-AdminWebManualUxAcceptanceChecklist.md'
$lines = New-Object 'System.Collections.Generic.List[string]'
[void]$lines.Add('# P10.3H - Admin Web Manual UX Acceptance Checklist')
[void]$lines.Add('')
[void]$lines.Add('Purpose: provide a focused manual click-through checklist after the Admin Web runtime gates are green.')
[void]$lines.Add('')
[void]$lines.Add('## Preconditions')
[void]$lines.Add('')
[void]$lines.Add('- Admin API is running locally.')
[void]$lines.Add('- Admin Web is running locally.')
[void]$lines.Add('- P10.3A through P10.3G have passed and been committed.')
[void]$lines.Add('- Missing historical builder-feature parity remains deferred and should not block this site-up acceptance pass.')
[void]$lines.Add('')
[void]$lines.Add('## Acceptance routes')
[void]$lines.Add('')
[void]$lines.Add('| Area | Route | Expected result |')
[void]$lines.Add('| --- | --- | --- |')
[void]$lines.Add('| Home | `/` | App shell renders without a white screen. |')
[void]$lines.Add('| Runtime Dashboard | `/runtime-dashboard` | Runtime dashboard page loads and handles empty data. |')
[void]$lines.Add('| Runtime Detail | `/runtime-dashboard/:runId` | Detail route renders without breaking the shell. |')
[void]$lines.Add('| Execution Sessions | `/execution-sessions` | Execution sessions page loads. |')
[void]$lines.Add('| Failure Retry | `/failure-retry` | Failure retry page loads. |')
[void]$lines.Add('| Operational Events | `/operations/operational-events` | Operational events page loads. |')
[void]$lines.Add('| Connector Configuration | `/connector-configuration` | Connector configuration page loads. |')
[void]$lines.Add('| Credential Vault | `/credential-vault` | Credential vault page loads. |')
[void]$lines.Add('| Manifest Builder | `/manifest-builder` | Deferred page shell renders if present; feature parity remains tracked separately. |')
[void]$lines.Add('| Taxonomy Builder | `/taxonomy-builder` | Deferred page shell renders if present; feature parity remains tracked separately. |')
[void]$lines.Add('| Mapping Builder | `/mapping-builder` | Deferred page shell renders if present; feature parity remains tracked separately. |')
[void]$lines.Add('')
[void]$lines.Add('## Manual checks')
[void]$lines.Add('')
[void]$lines.Add('- Navigate every visible left-nav item once.')
[void]$lines.Add('- Confirm the app shell/header/nav remains visible after each navigation.')
[void]$lines.Add('- Confirm loading and empty states are readable.')
[void]$lines.Add('- Confirm no obvious unhandled error block appears during navigation.')
[void]$lines.Add('- Confirm core API-backed pages show data, empty state, or a controlled error message rather than a crash.')
[void]$lines.Add('- Do not treat deferred historical builder parity as part of this gate.')
[void]$lines.Add('')
[void]$lines.Add('## Evidence commands')
[void]$lines.Add('')
[void]$lines.Add('Run the P10.3H checklist evidence runner after Admin Web and Admin API are already running:')
[void]$lines.Add('')
[void]$lines.Add('```powershell')
[void]$lines.Add('powershell -ExecutionPolicy Bypass -File .\tools\p10\P10.3H\Run-P10.3H-AdminWebManualUxAcceptanceChecklist.ps1 -AdminWebBaseUrl "http://localhost:5173" -AdminApiBaseUrl "https://localhost:55436"')
[void]$lines.Add('```')
[void]$lines.Add('')
[void]$lines.Add('The runner writes artifacts under `artifacts\p10\P10.3H`.')

Set-Content -LiteralPath $reportPath -Value $lines.ToArray() -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.3H Admin Web manual UX acceptance checklist applied.'
