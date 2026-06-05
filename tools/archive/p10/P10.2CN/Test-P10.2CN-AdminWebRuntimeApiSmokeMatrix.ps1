Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}
$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..')).Path
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$sourceRoot = Join-Path $adminWebRoot 'src'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2CN-AdminWebRuntimeApiSmokeMatrix.Report.md'
$runnerPath = Join-Path $scriptRoot 'Run-P10.2CN-AdminWebRuntimeApiSmokeMatrix.ps1'

if (-not (Test-Path -LiteralPath $adminWebRoot -PathType Container)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}
if (-not (Test-Path -LiteralPath $sourceRoot -PathType Container)) {
    throw ('Admin Web source root was not found: {0}' -f $sourceRoot)
}
if (-not (Test-Path -LiteralPath $reportPath -PathType Leaf)) {
    throw ('Expected report was not found: {0}' -f $reportPath)
}
if (-not (Test-Path -LiteralPath $runnerPath -PathType Leaf)) {
    throw ('Expected runner was not found: {0}' -f $runnerPath)
}

$reportText = Get-Content -LiteralPath $reportPath -Raw
if ($reportText -notlike '*Runtime API Smoke Matrix*') {
    throw ('Report does not contain expected title: {0}' -f $reportPath)
}

$runnerText = Get-Content -LiteralPath $runnerPath -Raw
if ($runnerText -notlike '*Invoke-WebRequest*') {
    throw ('Runner does not contain endpoint probing logic: {0}' -f $runnerPath)
}
if ($runnerText -notlike '*Admin Web Runtime API Smoke Matrix*') {
    throw ('Runner does not contain expected report title token: {0}' -f $runnerPath)
}

Write-Host 'P10.2CN Admin Web runtime API smoke matrix validation passed.'
