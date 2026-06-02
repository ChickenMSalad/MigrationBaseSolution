Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$repoRootPath = $repoRoot.Path

$applyPath = Join-Path $scriptRoot 'Apply-P10.2CY-AdminWebBuilderBackendVerbAwareSmoke.ps1'
$runnerPath = Join-Path $scriptRoot 'Run-P10.2CY-AdminWebBuilderBackendVerbAwareSmoke.ps1'
$reportPath = Join-Path $repoRootPath 'docs\P10\P10.2CY-AdminWebBuilderBackendVerbAwareSmoke.md'

if (-not (Test-Path -LiteralPath $applyPath)) {
    throw ('Missing apply script: {0}' -f $applyPath)
}

if (-not (Test-Path -LiteralPath $runnerPath)) {
    throw ('Missing smoke runner: {0}' -f $runnerPath)
}

if (-not (Test-Path -LiteralPath $reportPath)) {
    throw ('Missing report: {0}' -f $reportPath)
}

$runnerText = Get-Content -LiteralPath $runnerPath -Raw
if ($runnerText -notmatch 'param\s*\(') {
    throw 'Smoke runner is missing a param block.'
}
if ($runnerText -notmatch 'TimeoutSec') {
    throw 'Smoke runner is missing per-request timeout behavior.'
}
if ($runnerText -notmatch 'Manifest Builder') {
    throw 'Smoke runner does not include Manifest Builder probes.'
}
if ($runnerText -notmatch 'Taxonomy Builder') {
    throw 'Smoke runner does not include Taxonomy Builder probes.'
}
if ($runnerText -notmatch 'Mapping Builder') {
    throw 'Smoke runner does not include Mapping Builder probes.'
}

$reportText = Get-Content -LiteralPath $reportPath -Raw
if ($reportText -notmatch 'verb-aware') {
    throw 'Report does not describe verb-aware builder smoke.'
}

Write-Host 'P10.2CY Admin Web builder backend verb-aware smoke validation passed.'
