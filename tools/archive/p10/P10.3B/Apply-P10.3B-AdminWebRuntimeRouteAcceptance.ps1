Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$repoRoot = $repoRoot.ProviderPath

$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$srcRoot = Join-Path $adminWebRoot 'src'
$appPath = Join-Path $srcRoot 'App.tsx'
$layoutPath = Join-Path $srcRoot 'components\Layout.tsx'
$artifactRoot = Join-Path $repoRoot 'artifacts\p10\P10.3B'
$docsRoot = Join-Path $repoRoot 'docs\P10'
$reportPath = Join-Path $docsRoot 'P10.3B-AdminWebRuntimeRouteAcceptance.md'
$runnerPath = Join-Path $scriptRoot 'Run-P10.3B-AdminWebRuntimeRouteAcceptance.ps1'

if (-not (Test-Path -LiteralPath $adminWebRoot)) { throw ('Admin Web root was not found: {0}' -f $adminWebRoot) }
if (-not (Test-Path -LiteralPath $appPath)) { throw ('App.tsx was not found: {0}' -f $appPath) }
if (-not (Test-Path -LiteralPath $layoutPath)) { throw ('Layout.tsx was not found: {0}' -f $layoutPath) }

New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.3B - Admin Web Runtime Route Acceptance')
[void]$report.Add('')
[void]$report.Add(('Generated UTC: `{0}`' -f ([DateTime]::UtcNow.ToString('o'))))
[void]$report.Add('')
[void]$report.Add('## Purpose')
[void]$report.Add('')
[void]$report.Add('Adds a bounded local runtime route acceptance runner for the canonical Admin Web. This set does not change React or C# source.')
[void]$report.Add('')
[void]$report.Add('## Local inputs')
[void]$report.Add('')
[void]$report.Add(('- Admin Web root: `{0}`' -f $adminWebRoot))
[void]$report.Add(('- App.tsx: `{0}`' -f $appPath))
[void]$report.Add(('- Layout.tsx: `{0}`' -f $layoutPath))
[void]$report.Add('')
[void]$report.Add('## Runner')
[void]$report.Add('')
[void]$report.Add(('`{0}`' -f $runnerPath))
[void]$report.Add('')
[void]$report.Add('Run after Admin Web is available locally:')
[void]$report.Add('')
[void]$report.Add('```powershell')
[void]$report.Add('powershell -ExecutionPolicy Bypass -File .\tools\p10\P10.3B\Run-P10.3B-AdminWebRuntimeRouteAcceptance.ps1 -AdminWebBaseUrl "http://localhost:5173"')
[void]$report.Add('```')

Set-Content -LiteralPath $reportPath -Value $report.ToArray() -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.3B Admin Web runtime route acceptance harness applied.'
