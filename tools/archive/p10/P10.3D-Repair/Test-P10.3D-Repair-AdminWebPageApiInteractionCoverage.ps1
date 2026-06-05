Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
$toolRoot = Join-Path $repoRoot 'tools\p10\P10.3D-Repair'
$runnerPath = Join-Path $toolRoot 'Run-P10.3D-Repair-AdminWebPageApiInteractionCoverage.ps1'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.3D-Repair-AdminWebPageApiInteractionCoverage.md'

if (-not (Test-Path -LiteralPath $runnerPath)) {
    throw ('Expected runner missing: {0}' -f $runnerPath)
}

if (-not (Test-Path -LiteralPath $reportPath)) {
    throw ('Expected report missing: {0}' -f $reportPath)
}

$scriptFiles = Get-ChildItem -LiteralPath $toolRoot -Filter '*.ps1' -File
foreach ($script in $scriptFiles) {
    $content = Get-Content -LiteralPath $script.FullName -Raw
    [void][scriptblock]::Create($content)
}

$runner = Get-Content -LiteralPath $runnerPath -Raw
if ($runner -notmatch 'SkippedActionLikeEndpoint') {
    throw 'Runner does not contain action-like endpoint classification.'
}

if ($runner -notmatch 'SkippedVerbMismatch') {
    throw 'Runner does not contain verb mismatch classification.'
}

if ($runner -notmatch 'TimeoutSec') {
    throw 'Runner does not contain request timeout handling.'
}

Write-Host 'P10.3D Repair Admin Web page API interaction coverage validation passed.'
