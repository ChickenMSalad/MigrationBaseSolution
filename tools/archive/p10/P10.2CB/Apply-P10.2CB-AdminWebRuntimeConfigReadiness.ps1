Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..')).Path
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$docsRoot = Join-Path $repoRoot 'docs\P10'
$artifactsRoot = Join-Path $repoRoot 'artifacts\p10\P10.2CB'

if (-not (Test-Path -Path $adminWebRoot -PathType Container)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}

if (-not (Test-Path -Path $docsRoot -PathType Container)) {
    New-Item -Path $docsRoot -ItemType Directory -Force | Out-Null
}

if (-not (Test-Path -Path $artifactsRoot -PathType Container)) {
    New-Item -Path $artifactsRoot -ItemType Directory -Force | Out-Null
}

$envExamplePath = Join-Path $adminWebRoot '.env.local.example'
$createdEnvExample = $false
if (-not (Test-Path -Path $envExamplePath -PathType Leaf)) {
    $envLines = New-Object 'System.Collections.Generic.List[string]'
    [void]$envLines.Add('# Local Admin Web runtime configuration')
    [void]$envLines.Add('# Leave VITE_ADMIN_API_BASE_URL empty when using the Vite /api proxy during local development.')
    [void]$envLines.Add('VITE_ADMIN_API_BASE_URL=')
    [void]$envLines.Add('VITE_ADMIN_API_PROXY_TARGET=https://localhost:55436')
    [System.IO.File]::WriteAllLines($envExamplePath, $envLines.ToArray(), [System.Text.UTF8Encoding]::new($false))
    $createdEnvExample = $true
}

$docPath = Join-Path $docsRoot 'P10.2CB-AdminWebRuntimeConfigReadiness.md'
$doc = New-Object 'System.Collections.Generic.List[string]'
[void]$doc.Add('# P10.2CB - Admin Web Runtime Config Readiness')
[void]$doc.Add('')
[void]$doc.Add('## Summary')
[void]$doc.Add('')
[void]$doc.Add('This set prepares the canonical Admin Web for local/site-up runtime configuration after consolidation compile readiness.')
[void]$doc.Add('')
[void]$doc.Add('## Files')
[void]$doc.Add('')
[void]$doc.Add(('- Admin Web root: `{0}`' -f $adminWebRoot))
[void]$doc.Add(('- Environment example: `{0}`' -f $envExamplePath))
[void]$doc.Add('')
[void]$doc.Add('## Environment settings')
[void]$doc.Add('')
[void]$doc.Add('```text')
[void]$doc.Add('VITE_ADMIN_API_BASE_URL=')
[void]$doc.Add('VITE_ADMIN_API_PROXY_TARGET=https://localhost:55436')
[void]$doc.Add('```')
[void]$doc.Add('')
[void]$doc.Add('## Apply result')
[void]$doc.Add('')
if ($createdEnvExample) {
    [void]$doc.Add('- Created `.env.local.example`.')
} else {
    [void]$doc.Add('- `.env.local.example` already existed and was not overwritten.')
}
[void]$doc.Add('- No routes, pages, source imports, or package versions were changed.')
[System.IO.File]::WriteAllLines($docPath, $doc.ToArray(), [System.Text.UTF8Encoding]::new($false))

$reportPath = Join-Path $artifactsRoot 'runtime-config-readiness.summary.md'
$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2CB - Runtime Config Readiness Summary')
[void]$report.Add('')
[void]$report.Add(('- Admin Web root exists: {0}' -f (Test-Path -Path $adminWebRoot -PathType Container)))
[void]$report.Add(('- .env.local.example exists: {0}' -f (Test-Path -Path $envExamplePath -PathType Leaf)))
[void]$report.Add(('- Documentation: {0}' -f $docPath))
[System.IO.File]::WriteAllLines($reportPath, $report.ToArray(), [System.Text.UTF8Encoding]::new($false))

Write-Host ('Wrote documentation: {0}' -f $docPath)
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2CB Admin Web runtime config readiness applied.'
