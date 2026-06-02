Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$repoRootPath = $repoRoot.Path

$docsRoot = Join-Path $repoRootPath 'docs\P10'
if (-not (Test-Path -LiteralPath $docsRoot)) {
    New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null
}

$reportPath = Join-Path $docsRoot 'P10.2CY-AdminWebBuilderBackendVerbAwareSmoke.md'
$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2CY - Admin Web Builder Backend Verb-Aware Smoke')
[void]$report.Add('')
[void]$report.Add('Adds a bounded, verb-aware smoke runner for restored builder backend contracts.')
[void]$report.Add('')
[void]$report.Add('## Scope')
[void]$report.Add('')
[void]$report.Add('- Manifest Builder build/validate/preview endpoints are probed with POST.')
[void]$report.Add('- Taxonomy Builder collection/validate/preview endpoints are probed with safe GET/POST semantics.')
[void]$report.Add('- Mapping Builder collection/profiles/validate/preview endpoints are probed with safe GET/POST semantics.')
[void]$report.Add('- Every request has a timeout and the runner always writes summary/details evidence.')
[void]$report.Add('')
[void]$report.Add('## Optional runner')
[void]$report.Add('')
[void]$report.Add('```powershell')
[void]$report.Add('powershell -ExecutionPolicy Bypass -File .\tools\p10\P10.2CY\Run-P10.2CY-AdminWebBuilderBackendVerbAwareSmoke.ps1 -AdminApiBaseUrl "https://localhost:55436"')
[void]$report.Add('```')

Set-Content -LiteralPath $reportPath -Value $report.ToArray() -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2CY Admin Web builder backend verb-aware smoke applied.'
