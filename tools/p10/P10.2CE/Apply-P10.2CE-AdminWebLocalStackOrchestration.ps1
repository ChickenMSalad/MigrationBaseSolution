Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    if ($PSScriptRoot) {
        $candidate = Resolve-Path -Path (Join-Path $PSScriptRoot '..\..\..')
        return $candidate.Path
    }

    return (Get-Location).Path
}

$repoRoot = Get-RepositoryRoot
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$adminApiProject = Join-Path $repoRoot 'src\Core\Migration.Admin.Api\Migration.Admin.Api.csproj'
$toolRoot = Join-Path $repoRoot 'tools\p10\P10.2CE'
$artifactRoot = Join-Path $repoRoot 'artifacts\p10\P10.2CE'
$docPath = Join-Path $repoRoot 'docs\P10\P10.2CE-AdminWebLocalStackOrchestration.Report.md'

if (-not (Test-Path -Path $adminWebRoot -PathType Container)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}

if (-not (Test-Path -Path $adminApiProject -PathType Leaf)) {
    throw ('Admin API project was not found: {0}' -f $adminApiProject)
}

if (-not (Test-Path -Path $toolRoot -PathType Container)) {
    throw ('P10.2CE tool folder was not found: {0}' -f $toolRoot)
}

if (-not (Test-Path -Path $artifactRoot -PathType Container)) {
    New-Item -Path $artifactRoot -ItemType Directory -Force | Out-Null
}

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2CE - Admin Web Local Stack Orchestration')
[void]$report.Add('')
[void]$report.Add(('Repository root: `{0}`' -f $repoRoot))
[void]$report.Add(('Admin Web root: `{0}`' -f $adminWebRoot))
[void]$report.Add(('Admin API project: `{0}`' -f $adminApiProject))
[void]$report.Add(('Tool root: `{0}`' -f $toolRoot))
[void]$report.Add(('Artifact root: `{0}`' -f $artifactRoot))
[void]$report.Add('')
[void]$report.Add('## Helper scripts')
[void]$report.Add('')
[void]$report.Add('- `Run-P10.2CE-AdminApi.ps1` starts the Admin API with a configurable `--urls` value.')
[void]$report.Add('- `Run-P10.2CE-AdminWeb.ps1` starts the Admin Web Vite dev server with `VITE_ADMIN_API_PROXY_TARGET`.')
[void]$report.Add('- `Run-P10.2CE-LocalStackSmoke.ps1` checks the configured Admin API health URL and Admin Web URL.')
[void]$report.Add('')
[void]$report.Add('## Default URLs')
[void]$report.Add('')
[void]$report.Add('- Admin API: `https://localhost:55436`')
[void]$report.Add('- Admin Web: `http://127.0.0.1:5173`')

$docDir = Split-Path -Parent $docPath
if (-not (Test-Path -Path $docDir -PathType Container)) {
    New-Item -Path $docDir -ItemType Directory -Force | Out-Null
}
[System.IO.File]::WriteAllLines($docPath, $report.ToArray(), [System.Text.UTF8Encoding]::new($false))

Write-Host ('Wrote report: {0}' -f $docPath)
Write-Host 'P10.2CE Admin Web local stack orchestration applied.'
