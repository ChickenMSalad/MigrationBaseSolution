Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$docsRoot = Join-Path $repoRoot 'docs\P10'
$artifactRoot = Join-Path $repoRoot 'artifacts\p10\P10.3A'
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'

if (-not (Test-Path -LiteralPath $adminWebRoot)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}

if (-not (Test-Path -LiteralPath $docsRoot)) {
    New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null
}

if (-not (Test-Path -LiteralPath $artifactRoot)) {
    New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
}

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.3A - Admin Web Runtime Acceptance Harness')
[void]$report.Add('')
[void]$report.Add('Purpose: provide a bounded runtime acceptance probe for the canonical Admin Web and Admin API.')
[void]$report.Add('')
[void]$report.Add('This set does not modify Admin Web or Admin API source files.')
[void]$report.Add('')
[void]$report.Add('Expected manual prerequisites:')
[void]$report.Add('')
[void]$report.Add('- Admin API is running locally.')
[void]$report.Add('- Admin Web is running locally via Vite dev server or preview server.')
[void]$report.Add('- Use the runtime runner with the local Admin Web and Admin API base URLs.')
[void]$report.Add('')
[void]$report.Add('Runner:')
[void]$report.Add('')
[void]$report.Add('```powershell')
[void]$report.Add('powershell -ExecutionPolicy Bypass -File .\tools\p10\P10.3A\Run-P10.3A-AdminWebRuntimeAcceptance.ps1 -AdminWebBaseUrl "http://localhost:5173" -AdminApiBaseUrl "https://localhost:55436"')
[void]$report.Add('```')

$reportPath = Join-Path $docsRoot 'P10.3A-AdminWebRuntimeAcceptanceHarness.md'
Set-Content -LiteralPath $reportPath -Value $report.ToArray() -Encoding UTF8

Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.3A Admin Web runtime acceptance harness applied.'
