Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}
$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..')).Path
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$sourceRoot = Join-Path $adminWebRoot 'src'
$apiRoot = Join-Path $sourceRoot 'api'
$docsRoot = Join-Path $repoRoot 'docs\P10'
$artifactRoot = Join-Path $repoRoot 'artifacts\p10\P10.2CN'

if (-not (Test-Path -LiteralPath $adminWebRoot -PathType Container)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}
if (-not (Test-Path -LiteralPath $sourceRoot -PathType Container)) {
    throw ('Admin Web source root was not found: {0}' -f $sourceRoot)
}

New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null
New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2CN - Admin Web Runtime API Smoke Matrix')
[void]$report.Add('')
[void]$report.Add(('Generated: {0:O}' -f (Get-Date)))
[void]$report.Add(('Admin Web root: `{0}`' -f $adminWebRoot))
[void]$report.Add('')
[void]$report.Add('## Purpose')
[void]$report.Add('')
[void]$report.Add('Provide a local runtime API smoke matrix for the canonical Admin Web without rewriting source files.')
[void]$report.Add('')
[void]$report.Add('## API source files')
[void]$report.Add('')

$apiFiles = @()
if (Test-Path -LiteralPath $apiRoot -PathType Container) {
    $apiFiles = @(Get-ChildItem -LiteralPath $apiRoot -Recurse -File -Include '*.ts','*.tsx' | Sort-Object FullName)
}

if ($apiFiles.Length -eq 0) {
    [void]$report.Add('- No API source files found under canonical `src/api`.')
} else {
    foreach ($file in $apiFiles) {
        $relative = $file.FullName.Substring($sourceRoot.Length).TrimStart('\')
        [void]$report.Add(('- `{0}`' -f $relative.Replace('\','/')))
    }
}

[void]$report.Add('')
[void]$report.Add('## Runtime smoke runner')
[void]$report.Add('')
[void]$report.Add('Run after starting Admin API locally:')
[void]$report.Add('')
[void]$report.Add('```powershell')
[void]$report.Add('powershell -ExecutionPolicy Bypass -File .\tools\p10\P10.2CN\Run-P10.2CN-AdminWebRuntimeApiSmokeMatrix.ps1')
[void]$report.Add('```')
[void]$report.Add('')
[void]$report.Add('The runner discovers static API path strings from Admin Web source and performs conservative GET-style probes only.')

$reportPath = Join-Path $docsRoot 'P10.2CN-AdminWebRuntimeApiSmokeMatrix.Report.md'
[System.IO.File]::WriteAllLines($reportPath, $report.ToArray(), [System.Text.Encoding]::UTF8)

Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2CN Admin Web runtime API smoke matrix applied.'
