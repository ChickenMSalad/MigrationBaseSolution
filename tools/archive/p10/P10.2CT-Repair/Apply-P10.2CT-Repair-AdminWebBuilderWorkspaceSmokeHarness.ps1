Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$repoRoot = $repoRoot.Path

$docsRoot = Join-Path $repoRoot 'docs\P10'
if (-not (Test-Path -LiteralPath $docsRoot)) {
    New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null
}

$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$appPath = Join-Path $adminWebRoot 'src\App.tsx'
$layoutPath = Join-Path $adminWebRoot 'src\components\Layout.tsx'
$runnerPath = Join-Path $scriptRoot 'Run-P10.2CT-Repair-AdminWebBuilderWorkspaceSmoke.ps1'
$reportPath = Join-Path $docsRoot 'P10.2CT-Repair-AdminWebBuilderWorkspaceSmokeHarness.md'

if (-not (Test-Path -LiteralPath $adminWebRoot)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}

if (-not (Test-Path -LiteralPath $appPath)) {
    throw ('App.tsx was not found: {0}' -f $appPath)
}

if (-not (Test-Path -LiteralPath $runnerPath)) {
    throw ('Repair smoke runner was not found: {0}' -f $runnerPath)
}

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2CT Repair - Admin Web Builder Workspace Smoke Harness')
[void]$report.Add('')
[void]$report.Add(('Generated UTC: `{0}`' -f ([DateTime]::UtcNow.ToString('o'))))
[void]$report.Add('')
[void]$report.Add('## Scope')
[void]$report.Add('')
[void]$report.Add('- Repairs the P10.2CT optional builder workspace smoke runner parameter parsing issue.')
[void]$report.Add('- Does not modify Admin Web source files.')
[void]$report.Add('- Provides a bounded route smoke runner for Manifest, Taxonomy, and Mapping builder routes.')
[void]$report.Add('')
[void]$report.Add('## Local paths')
[void]$report.Add('')
[void]$report.Add(('- Admin Web root: `{0}`' -f $adminWebRoot))
[void]$report.Add(('- App.tsx: `{0}`' -f $appPath))
[void]$report.Add(('- Layout.tsx exists: `{0}`' -f (Test-Path -LiteralPath $layoutPath)))
[void]$report.Add(('- Runner: `{0}`' -f $runnerPath))
[void]$report.Add('')
[void]$report.Add('## Runner usage')
[void]$report.Add('')
[void]$report.Add('```powershell')
[void]$report.Add('powershell -ExecutionPolicy Bypass -File .\tools\p10\P10.2CT-Repair\Run-P10.2CT-Repair-AdminWebBuilderWorkspaceSmoke.ps1 -AdminWebBaseUrl "http://localhost:5173"')
[void]$report.Add('```')

Set-Content -LiteralPath $reportPath -Value $report.ToArray() -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2CT Repair Admin Web builder workspace smoke harness applied.'
