Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path -Path (Join-Path -Path $PSScriptRoot -ChildPath '..\..\..')).Path
$adminWebRoot = Join-Path -Path $repoRoot -ChildPath 'src\Admin\Migration.Admin.Web'
$packageJson = Join-Path -Path $adminWebRoot -ChildPath 'package.json'
$viteConfig = Join-Path -Path $adminWebRoot -ChildPath 'vite.config.ts'
$envExample = Join-Path -Path $adminWebRoot -ChildPath '.env.local.example'
$artifactRoot = Join-Path -Path $repoRoot -ChildPath 'artifacts\p10\P10.2CC'
$docPath = Join-Path -Path $repoRoot -ChildPath 'docs\P10\P10.2CC-AdminWebLocalSiteSmokeHarness.Report.md'

if (-not (Test-Path -Path $adminWebRoot -PathType Container)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}

if (-not (Test-Path -Path $packageJson -PathType Leaf)) {
    throw ('package.json was not found: {0}' -f $packageJson)
}

if (-not (Test-Path -Path $viteConfig -PathType Leaf)) {
    throw ('vite.config.ts was not found: {0}' -f $viteConfig)
}

if (-not (Test-Path -Path $artifactRoot -PathType Container)) {
    New-Item -Path $artifactRoot -ItemType Directory -Force | Out-Null
}

$packageText = Get-Content -Path $packageJson -Raw
$viteText = Get-Content -Path $viteConfig -Raw

if ($packageText -notmatch '"dev"\s*:\s*"vite"') {
    throw 'Admin Web package.json does not expose the expected dev script.'
}

if ($packageText -notmatch '"build"\s*:\s*"tsc -b && vite build"') {
    throw 'Admin Web package.json does not expose the expected build script.'
}

if ($viteText -notmatch 'VITE_ADMIN_API_PROXY_TARGET') {
    throw 'vite.config.ts does not reference VITE_ADMIN_API_PROXY_TARGET.'
}

if ($viteText -notmatch 'server') {
    throw 'vite.config.ts does not contain a server configuration block.'
}

if (-not (Test-Path -Path $envExample -PathType Leaf)) {
    $envLines = New-Object 'System.Collections.Generic.List[string]'
    [void]$envLines.Add('# P10.2CC local Admin Web configuration')
    [void]$envLines.Add('# Copy to .env.local for local site-up work.')
    [void]$envLines.Add('VITE_ADMIN_API_PROXY_TARGET=https://localhost:55436')
    [System.IO.File]::WriteAllLines($envExample, $envLines.ToArray(), [System.Text.Encoding]::UTF8)
}

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2CC - Admin Web Local Site Smoke Harness Report')
[void]$report.Add('')
[void]$report.Add(('Generated: {0:u}' -f (Get-Date)))
[void]$report.Add('')
[void]$report.Add(('Repo root: `{0}`' -f $repoRoot))
[void]$report.Add(('Admin Web root: `{0}`' -f $adminWebRoot))
[void]$report.Add('')
[void]$report.Add('## Verified files')
[void]$report.Add('')
[void]$report.Add(('- package.json: `{0}`' -f $packageJson))
[void]$report.Add(('- vite.config.ts: `{0}`' -f $viteConfig))
[void]$report.Add(('- .env.local.example: `{0}`' -f $envExample))
[void]$report.Add('')
[void]$report.Add('## Local smoke command')
[void]$report.Add('')
[void]$report.Add('```powershell')
[void]$report.Add('powershell -ExecutionPolicy Bypass -File .\tools\p10\P10.2CC\Run-P10.2CC-AdminWebDevSmoke.ps1')
[void]$report.Add('```')
[void]$report.Add('')
[void]$report.Add('The smoke runner starts Vite, waits for the local root page, records stdout/stderr, and stops the process.')
[System.IO.File]::WriteAllLines($docPath, $report.ToArray(), [System.Text.Encoding]::UTF8)

Write-Host ('Wrote report: {0}' -f $docPath)
Write-Host 'P10.2CC Admin Web local site smoke harness applied.'
