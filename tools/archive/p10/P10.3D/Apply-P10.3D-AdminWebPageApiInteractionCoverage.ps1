Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$toolRoot = Join-Path $repoRoot 'tools\p10\P10.3D'
$docsRoot = Join-Path $repoRoot 'docs\P10'
$artifactRoot = Join-Path $repoRoot 'artifacts\p10\P10.3D'

if (-not (Test-Path -LiteralPath $adminWebRoot)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}

New-Item -ItemType Directory -Force -Path $docsRoot | Out-Null
New-Item -ItemType Directory -Force -Path $artifactRoot | Out-Null

$runbookPath = Join-Path $docsRoot 'P10.3D-AdminWebPageApiInteractionCoverage.md'
$lines = New-Object 'System.Collections.Generic.List[string]'
[void]$lines.Add('# P10.3D - Admin Web Page API Interaction Coverage')
[void]$lines.Add('')
[void]$lines.Add('Purpose: inventory current Admin Web API usage and optionally probe safe GET endpoints against a running Admin API.')
[void]$lines.Add('')
[void]$lines.Add('This set does not modify Admin Web or Admin API source files.')
[void]$lines.Add('')
[void]$lines.Add('Run after Admin API is running:')
[void]$lines.Add('')
[void]$lines.Add('```powershell')
[void]$lines.Add('powershell -ExecutionPolicy Bypass -File .\tools\p10\P10.3D\Run-P10.3D-AdminWebPageApiInteractionCoverage.ps1 -AdminApiBaseUrl "https://localhost:55436"')
[void]$lines.Add('```')
[void]$lines.Add('')
[void]$lines.Add('Outputs:')
[void]$lines.Add('')
[void]$lines.Add('- `artifacts/p10/P10.3D/page-api-interaction-coverage.summary.md`')
[void]$lines.Add('- `artifacts/p10/P10.3D/page-api-interaction-coverage.details.csv`')

Set-Content -LiteralPath $runbookPath -Value $lines.ToArray() -Encoding UTF8
Write-Host ('Wrote runbook: {0}' -f $runbookPath)
Write-Host 'P10.3D Admin Web page API interaction coverage applied.'
