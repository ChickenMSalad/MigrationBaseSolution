Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Resolve-RepoRoot {
    $scriptRoot = $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
        $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    }

    $candidate = Resolve-Path -LiteralPath (Join-Path $scriptRoot '..\..\..')
    return $candidate.ProviderPath
}

$repoRoot = Resolve-RepoRoot
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$artifactRoot = Join-Path $repoRoot 'artifacts\p10\P10.2CH'
$docPath = Join-Path $repoRoot 'docs\P10\P10.2CH-AdminWebPreviewSmokeHarness.md'
$runnerPath = Join-Path $repoRoot 'tools\p10\P10.2CH\Run-P10.2CH-AdminWebPreviewSmoke.ps1'

if (-not (Test-Path -LiteralPath $adminWebRoot -PathType Container)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}

if (-not (Test-Path -LiteralPath (Join-Path $adminWebRoot 'package.json') -PathType Leaf)) {
    throw ('Admin Web package.json was not found under: {0}' -f $adminWebRoot)
}

if (-not (Test-Path -LiteralPath $runnerPath -PathType Leaf)) {
    throw ('Preview smoke runner is missing: {0}' -f $runnerPath)
}

if (-not (Test-Path -LiteralPath $artifactRoot -PathType Container)) {
    New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
}

$lines = New-Object 'System.Collections.Generic.List[string]'
[void]$lines.Add('# P10.2CH - Admin Web Preview Smoke Harness')
[void]$lines.Add('')
[void]$lines.Add(('Generated: {0:u}' -f (Get-Date)))
[void]$lines.Add('')
[void]$lines.Add(('Admin Web root: `{0}`' -f $adminWebRoot))
[void]$lines.Add(('Artifact root: `{0}`' -f $artifactRoot))
[void]$lines.Add('')
[void]$lines.Add('## Optional runner')
[void]$lines.Add('')
[void]$lines.Add('```powershell')
[void]$lines.Add('powershell -ExecutionPolicy Bypass -File .\tools\p10\P10.2CH\Run-P10.2CH-AdminWebPreviewSmoke.ps1')
[void]$lines.Add('```')
[void]$lines.Add('')
[void]$lines.Add('This set adds scripts and documentation only. It does not modify Admin Web source files.')

$docDirectory = Split-Path -Parent $docPath
if (-not (Test-Path -LiteralPath $docDirectory -PathType Container)) {
    New-Item -ItemType Directory -Path $docDirectory -Force | Out-Null
}
Set-Content -LiteralPath $docPath -Value $lines -Encoding UTF8

Write-Host ('Wrote report: {0}' -f $docPath)
Write-Host 'P10.2CH Admin Web preview smoke harness applied.'
