Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$docsRoot = Join-Path $repoRoot 'docs\P10'
$artifactsRoot = Join-Path $repoRoot 'artifacts\p10\P10.3C'
$runnerPath = Join-Path $PSScriptRoot 'Run-P10.3C-AdminWebBrowserRuntimeHealth.ps1'
$reportPath = Join-Path $docsRoot 'P10.3C-AdminWebBrowserRuntimeHealth.md'

if (-not (Test-Path -LiteralPath $adminWebRoot -PathType Container)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}
if (-not (Test-Path -LiteralPath (Join-Path $adminWebRoot 'package.json') -PathType Leaf)) {
    throw ('Admin Web package.json was not found: {0}' -f (Join-Path $adminWebRoot 'package.json'))
}

New-Item -ItemType Directory -Force -Path $docsRoot | Out-Null
New-Item -ItemType Directory -Force -Path $artifactsRoot | Out-Null

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.3C - Admin Web Browser Runtime Health')
[void]$report.Add('')
[void]$report.Add('Adds a bounded browser-runtime health harness for the canonical Admin Web runtime.')
[void]$report.Add('')
[void]$report.Add('## Scope')
[void]$report.Add('')
[void]$report.Add('- No Admin Web source rewrites.')
[void]$report.Add('- No backend source rewrites.')
[void]$report.Add('- Probes the running Vite app over HTTP.')
[void]$report.Add('- Verifies HTML, module script assets, stylesheet assets, and route shell responses.')
[void]$report.Add('- Writes summary and detail evidence under artifacts/p10/P10.3C.')
[void]$report.Add('')
[void]$report.Add('## Runner')
[void]$report.Add('')
[void]$report.Add('```powershell')
[void]$report.Add('powershell -ExecutionPolicy Bypass -File .\tools\p10\P10.3C\Run-P10.3C-AdminWebBrowserRuntimeHealth.ps1 -AdminWebBaseUrl "http://localhost:5173"')
[void]$report.Add('```')

Set-Content -LiteralPath $reportPath -Value $report.ToArray() -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.3C Admin Web browser runtime health harness applied.'
