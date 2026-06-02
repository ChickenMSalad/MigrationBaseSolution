Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..')).Path
$toolRoot = Join-Path $repoRoot 'tools\p10\P10.2CU-Repair'
$docsRoot = Join-Path $repoRoot 'docs\P10'
$runnerPath = Join-Path $toolRoot 'Run-P10.2CU-Repair-AdminWebBuilderApiContractSmoke.ps1'
$applyPath = Join-Path $toolRoot 'Apply-P10.2CU-Repair-AdminWebBuilderApiContractSmoke.ps1'
$reportPath = Join-Path $docsRoot 'P10.2CU-Repair-AdminWebBuilderApiContractSmoke.md'

if (-not (Test-Path -LiteralPath $applyPath)) {
    throw ('Missing apply script: {0}' -f $applyPath)
}
if (-not (Test-Path -LiteralPath $runnerPath)) {
    throw ('Missing runner script: {0}' -f $runnerPath)
}
if (-not (Test-Path -LiteralPath $reportPath)) {
    throw ('Missing report: {0}' -f $reportPath)
}

[scriptblock]::Create((Get-Content -Path $applyPath -Raw)) | Out-Null
[scriptblock]::Create((Get-Content -Path $runnerPath -Raw)) | Out-Null

$runnerText = Get-Content -Path $runnerPath -Raw
if ($runnerText -notmatch 'param\s*\(') {
    throw 'Runner does not contain a valid param block.'
}
if ($runnerText -notmatch 'TimeoutSec') {
    throw 'Runner does not contain request timeout handling.'
}
if ($runnerText -notmatch 'Export-Csv') {
    throw 'Runner does not write CSV details.'
}

$reportText = Get-Content -Path $reportPath -Raw
if ($reportText -notmatch 'Builder API Contract Smoke') {
    throw 'Report does not describe builder API contract smoke.'
}

Write-Host 'P10.2CU Repair Admin Web builder API contract smoke validation passed.'
