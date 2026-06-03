Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..')).Path
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$docsRoot = Join-Path $repoRoot 'docs\P10'
$artifactRoot = Join-Path $repoRoot 'artifacts\p10\P10.3K'

if (-not (Test-Path -LiteralPath $adminWebRoot -PathType Container)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}

$packageJson = Join-Path $adminWebRoot 'package.json'
if (-not (Test-Path -LiteralPath $packageJson -PathType Leaf)) {
    throw ('Admin Web package.json was not found: {0}' -f $packageJson)
}

if (-not (Test-Path -LiteralPath $docsRoot -PathType Container)) {
    New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null
}
if (-not (Test-Path -LiteralPath $artifactRoot -PathType Container)) {
    New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
}

$runScript = Join-Path $scriptRoot 'Run-P10.3K-AdminWebCloudPublishPackage.ps1'
if (-not (Test-Path -LiteralPath $runScript -PathType Leaf)) {
    throw ('Expected publish package runner was not found: {0}' -f $runScript)
}

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.3K - Admin Web Cloud Publish Readiness')
[void]$report.Add('')
[void]$report.Add(('Generated UTC: `{0}`' -f ([DateTime]::UtcNow.ToString('o'))))
[void]$report.Add('')
[void]$report.Add('## Scope')
[void]$report.Add('')
[void]$report.Add('- Prepare a deployable static Admin Web package from the canonical Vite app.')
[void]$report.Add('- Do not assume cloud resource names, subscriptions, credentials, or hosting choice.')
[void]$report.Add('- Do not mutate Admin Web source files.')
[void]$report.Add('')
[void]$report.Add('## Canonical Admin Web')
[void]$report.Add('')
[void]$report.Add(('Admin Web root: `{0}`' -f $adminWebRoot))
[void]$report.Add(('package.json: `{0}`' -f $packageJson))
[void]$report.Add('')
[void]$report.Add('## Publish Package Runner')
[void]$report.Add('')
[void]$report.Add(('Runner: `{0}`' -f $runScript))
[void]$report.Add('')
[void]$report.Add('Run from repo root:')
[void]$report.Add('')
[void]$report.Add('```powershell')
[void]$report.Add('powershell -ExecutionPolicy Bypass -File .\tools\p10\P10.3K\Run-P10.3K-AdminWebCloudPublishPackage.ps1')
[void]$report.Add('```')
[void]$report.Add('')
[void]$report.Add('This creates a zipped static-site artifact under `artifacts\p10\P10.3K`.')
[void]$report.Add('')
[void]$report.Add('## Cloud Publish Notes')
[void]$report.Add('')
[void]$report.Add('Use the generated static artifact with the selected host, such as Azure Static Web Apps, Azure Storage static website hosting, or an App Service/static file deployment. Configure the Admin API base URL/environment for the target environment before publishing.')

$reportPath = Join-Path $docsRoot 'P10.3K-AdminWebCloudPublishReadiness.md'
Set-Content -LiteralPath $reportPath -Value $report.ToArray() -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.3K Admin Web cloud publish readiness applied.'
