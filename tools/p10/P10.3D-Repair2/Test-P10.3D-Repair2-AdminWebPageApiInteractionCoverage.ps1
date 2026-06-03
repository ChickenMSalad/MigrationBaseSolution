Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..\..')
$applyScript = Join-Path $PSScriptRoot 'Apply-P10.3D-Repair2-AdminWebPageApiInteractionCoverage.ps1'
$runnerScript = Join-Path $PSScriptRoot 'Run-P10.3D-Repair2-AdminWebPageApiInteractionCoverage.ps1'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.3D-Repair2-AdminWebPageApiInteractionCoverage.md'

foreach ($path in @($applyScript, $runnerScript)) {
    if (-not (Test-Path $path)) {
        throw ('Expected script missing: {0}' -f $path)
    }
    $content = Get-Content -Path $path -Raw
    [void][scriptblock]::Create($content)
}

if (-not (Test-Path $reportPath)) {
    throw ('Expected report missing: {0}' -f $reportPath)
}

$report = Get-Content -Path $reportPath -Raw
if (-not $report.Contains('P10.3D Repair2')) {
    throw 'Repair2 report does not contain expected heading.'
}
if (-not $report.Contains('regex')) {
    throw 'Repair2 report does not describe the regex-free repair.'
}

$runner = Get-Content -Path $runnerScript -Raw
if (-not $runner.Contains('param(')) {
    throw 'Runner does not contain a valid param block.'
}
if (-not $runner.Contains('VerbMismatchEvidence')) {
    throw 'Runner does not classify verb mismatch evidence.'
}
if (-not $runner.Contains('TimeoutSec')) {
    throw 'Runner does not use bounded web request timeouts.'
}

Write-Host 'P10.3D Repair2 Admin Web page API interaction coverage validation passed.'
