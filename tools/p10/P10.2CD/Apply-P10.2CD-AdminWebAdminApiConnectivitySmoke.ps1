Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$repoRootPath = $repoRoot.Path

$adminWebRoot = Join-Path $repoRootPath 'src\Admin\Migration.Admin.Web'
$adminApiRoot = Join-Path $repoRootPath 'src\Core\Migration.Admin.Api'
$docPath = Join-Path $repoRootPath 'docs\P10\P10.2CD-AdminWebAdminApiConnectivitySmoke.md'
$runnerPath = Join-Path $scriptRoot 'Run-P10.2CD-AdminApiConnectivitySmoke.ps1'

if (-not (Test-Path -Path $adminWebRoot -PathType Container)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}

if (-not (Test-Path -Path $adminApiRoot -PathType Container)) {
    throw ('Admin API root was not found: {0}' -f $adminApiRoot)
}

if (-not (Test-Path -Path $docPath -PathType Leaf)) {
    throw ('Expected documentation file was not found: {0}' -f $docPath)
}

if (-not (Test-Path -Path $runnerPath -PathType Leaf)) {
    throw ('Expected smoke runner was not found: {0}' -f $runnerPath)
}

$reportDir = Join-Path $repoRootPath 'artifacts\p10\P10.2CD'
New-Item -ItemType Directory -Force -Path $reportDir | Out-Null
$reportPath = Join-Path $reportDir 'apply-summary.md'

$lines = New-Object 'System.Collections.Generic.List[string]'
[void]$lines.Add('# P10.2CD - Admin Web Admin API Connectivity Smoke')
[void]$lines.Add('')
[void]$lines.Add(('Repo root: `{0}`' -f $repoRootPath))
[void]$lines.Add(('Admin Web root: `{0}`' -f $adminWebRoot))
[void]$lines.Add(('Admin API root: `{0}`' -f $adminApiRoot))
[void]$lines.Add('')
[void]$lines.Add('This set added a non-mutating local connectivity smoke harness.')
[void]$lines.Add('Run the optional smoke only after starting the Admin API locally.')

Set-Content -Path $reportPath -Value $lines -Encoding UTF8
Write-Host ('Wrote apply summary: {0}' -f $reportPath)
Write-Host 'P10.2CD Admin Web Admin API connectivity smoke applied.'
