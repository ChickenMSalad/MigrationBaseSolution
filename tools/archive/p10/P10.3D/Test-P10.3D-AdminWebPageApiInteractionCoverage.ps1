Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$runbookPath = Join-Path $repoRoot 'docs\P10\P10.3D-AdminWebPageApiInteractionCoverage.md'
$runnerPath = Join-Path $repoRoot 'tools\p10\P10.3D\Run-P10.3D-AdminWebPageApiInteractionCoverage.ps1'

if (-not (Test-Path -LiteralPath $adminWebRoot)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}

if (-not (Test-Path -LiteralPath $runbookPath)) {
    throw ('Expected runbook missing: {0}' -f $runbookPath)
}

if (-not (Test-Path -LiteralPath $runnerPath)) {
    throw ('Expected runner missing: {0}' -f $runnerPath)
}

$runbook = Get-Content -LiteralPath $runbookPath -Raw
if ($runbook -notlike '*Page API Interaction Coverage*') {
    throw 'Runbook does not contain the expected title.'
}

$runner = Get-Content -LiteralPath $runnerPath -Raw
if ($runner -notlike '*param(*') {
    throw 'Runner does not contain a param block.'
}
if ($runner -notlike '*TimeoutSec*') {
    throw 'Runner does not include bounded request timeout handling.'
}
if ($runner -notlike '*page-api-interaction-coverage.details.csv*') {
    throw 'Runner does not reference the expected details output.'
}

Write-Host 'P10.3D Admin Web page API interaction coverage validation passed.'
