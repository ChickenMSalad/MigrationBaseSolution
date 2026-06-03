Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$repoRoot = $repoRoot.ProviderPath
$docsRoot = Join-Path $repoRoot 'docs\P10'
if (-not (Test-Path -LiteralPath $docsRoot)) {
    New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null
}

$reportPath = Join-Path $docsRoot 'P10.3J-AdminWebP10ClosureEvidenceBundle.md'
$lines = New-Object 'System.Collections.Generic.List[string]'

[void]$lines.Add('# P10.3J - Admin Web P10 Closure Evidence Bundle')
[void]$lines.Add('')
[void]$lines.Add('## Purpose')
[void]$lines.Add('')
[void]$lines.Add('Capture the current P10 Admin Web site-up evidence posture without changing application source.')
[void]$lines.Add('')
[void]$lines.Add('## Scope')
[void]$lines.Add('')
[void]$lines.Add('- Report-only closure bundle.')
[void]$lines.Add('- No Admin Web source changes.')
[void]$lines.Add('- No backend source changes.')
[void]$lines.Add('- No feature restoration work.')
[void]$lines.Add('- Missing builder parity remains deferred to a separate feature-parity phase using the recovered source commit.')
[void]$lines.Add('')
[void]$lines.Add('## Expected Evidence Inputs')
[void]$lines.Add('')
[void]$lines.Add('| Area | Expected artifact/source |')
[void]$lines.Add('| --- | --- |')
[void]$lines.Add('| Runtime connectivity | artifacts/p10/P10.3A/runtime-acceptance.summary.md |')
[void]$lines.Add('| Route acceptance | artifacts/p10/P10.3B/runtime-route-acceptance.summary.md |')
[void]$lines.Add('| Browser runtime health | artifacts/p10/P10.3C/browser-runtime-health.summary.md |')
[void]$lines.Add('| Page/API coverage | artifacts/p10/P10.3D-Repair3/page-api-interaction-coverage.summary.md |')
[void]$lines.Add('| Operator workflow acceptance | artifacts/p10/P10.3E-Repair2/operator-workflow-acceptance.summary.md |')
[void]$lines.Add('| Release readiness gate | artifacts/p10/P10.3F/release-readiness-evidence.summary.md or docs/P10 evidence |')
[void]$lines.Add('| Site-up runbook | docs/P10/P10.3G-AdminWebSiteUpRunbook.md |')
[void]$lines.Add('| Manual UX checklist | docs/P10/P10.3H-AdminWebManualUxAcceptanceChecklist.md |')
[void]$lines.Add('| Production hardening inventory | docs/P10/P10.3I-AdminWebProductionHardeningInventory.md |')
[void]$lines.Add('')
[void]$lines.Add('## Closure Statement')
[void]$lines.Add('')
[void]$lines.Add('P10 Admin Web consolidation and site-up work is considered closure-ready when the runtime acceptance, route acceptance, browser/runtime health, operator workflow acceptance, release readiness evidence, runbook, UX checklist, and production hardening inventory artifacts are present and current.')
[void]$lines.Add('')
[void]$lines.Add('Deferred items are intentionally outside this closure bundle: builder feature parity restoration, old-site commit reconciliation, and any page/API parity recovery that requires product decisions.')

Set-Content -LiteralPath $reportPath -Value $lines.ToArray() -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.3J Admin Web P10 closure evidence bundle applied.'
