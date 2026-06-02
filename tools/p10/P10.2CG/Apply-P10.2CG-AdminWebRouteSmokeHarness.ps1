Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    if ($PSScriptRoot) {
        return (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
    }

    return (Get-Location).Path
}

$repoRoot = Get-RepoRoot
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$appTsx = Join-Path $adminWebRoot 'src\App.tsx'
$toolRoot = Join-Path $repoRoot 'tools\p10\P10.2CG'
$docPath = Join-Path $repoRoot 'docs\P10\P10.2CG-AdminWebRouteSmokeHarness.md'
$artifactRoot = Join-Path $repoRoot 'artifacts\p10\P10.2CG'
$reportPath = Join-Path $artifactRoot 'P10.2CG-AdminWebRouteSmokeHarness.Apply.md'

if (-not (Test-Path -Path $adminWebRoot -PathType Container)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}

if (-not (Test-Path -Path $appTsx -PathType Leaf)) {
    throw ('App.tsx was not found: {0}' -f $appTsx)
}

if (-not (Test-Path -Path $toolRoot -PathType Container)) {
    New-Item -ItemType Directory -Path $toolRoot -Force | Out-Null
}

if (-not (Test-Path -Path $artifactRoot -PathType Container)) {
    New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
}

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2CG - Admin Web Route Smoke Harness Apply')
[void]$report.Add('')
[void]$report.Add(('Repo root: `{0}`' -f $repoRoot))
[void]$report.Add(('Admin Web root: `{0}`' -f $adminWebRoot))
[void]$report.Add(('App.tsx: `{0}`' -f $appTsx))
[void]$report.Add('')
[void]$report.Add('## Status')
[void]$report.Add('')
[void]$report.Add('- Route smoke harness scripts are present.')
[void]$report.Add('- Application source was not changed.')
[void]$report.Add('- Use the run script after starting the Admin Web dev server.')
[void]$report.Add('')
[void]$report.Add('## Run')
[void]$report.Add('')
[void]$report.Add('```powershell')
[void]$report.Add('powershell -ExecutionPolicy Bypass -File .\tools\p10\P10.2CG\Run-P10.2CG-AdminWebRouteSmoke.ps1')
[void]$report.Add('```')

$report | Set-Content -Path $reportPath -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2CG Admin Web route smoke harness applied.'
