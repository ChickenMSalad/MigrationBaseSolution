Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..')).Path
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2CK-AdminWebSiteUpEvidenceSnapshot.Report.md'
$runnerPath = Join-Path $scriptRoot 'Run-P10.2CK-AdminWebSiteUpEvidenceSnapshot.ps1'

if (-not (Test-Path -Path $adminWebRoot -PathType Container)) {
    throw ('Canonical Admin Web root was not found: {0}' -f $adminWebRoot)
}
if (-not (Test-Path -Path $reportPath -PathType Leaf)) {
    throw ('Expected report was not found: {0}' -f $reportPath)
}
if (-not (Test-Path -Path $runnerPath -PathType Leaf)) {
    throw ('Expected runner was not found: {0}' -f $runnerPath)
}

$reportText = Get-Content -Path $reportPath -Raw
if ($reportText -notmatch 'Admin Web Site-Up Evidence Snapshot') {
    throw ('Report does not contain the expected title: {0}' -f $reportPath)
}

$runnerText = Get-Content -Path $runnerPath -Raw
if ($runnerText -notmatch 'site-up-evidence.md') {
    throw ('Runner does not write the expected evidence file: {0}' -f $runnerPath)
}
if ($runnerText -notmatch 'Migration.Admin.Web') {
    throw ('Runner does not reference the canonical Admin Web root: {0}' -f $runnerPath)
}
if ($runnerText -match 'src\t') {
    throw ('Runner contains a suspicious escaped tab path token: {0}' -f $runnerPath)
}

Write-Host 'P10.2CK Admin Web site-up evidence snapshot validation passed.'
