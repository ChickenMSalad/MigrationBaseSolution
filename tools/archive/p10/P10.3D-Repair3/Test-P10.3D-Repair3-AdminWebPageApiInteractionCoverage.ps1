Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$toolRoot = Join-Path $repoRoot 'tools\p10\P10.3D-Repair3'
$docsRoot = Join-Path $repoRoot 'docs\P10'

$applyPath = Join-Path $toolRoot 'Apply-P10.3D-Repair3-AdminWebPageApiInteractionCoverage.ps1'
$runnerPath = Join-Path $toolRoot 'Run-P10.3D-Repair3-AdminWebPageApiInteractionCoverage.ps1'
$reportPath = Join-Path $docsRoot 'P10.3D-Repair3-AdminWebPageApiInteractionCoverage.md'

foreach ($path in @($applyPath, $runnerPath, $reportPath)) {
    if (-not (Test-Path $path)) {
        throw ('Expected file was not found: {0}' -f $path)
    }
}

foreach ($path in @($applyPath, $runnerPath)) {
    $content = Get-Content -Path $path -Raw
    [void][scriptblock]::Create($content)
}

$runner = Get-Content -Path $runnerPath -Raw

foreach ($required in @(
    'function Get-QuotedLiterals',
    'Should-Skip-LiteralPath',
    'SkippedModuleImportOrRelativePath',
    'SkippedDynamicOrTemplatePath',
    'VerbMismatchEvidence',
    'TimeoutSec'
)) {
    if (-not $runner.Contains($required)) {
        throw ('Runner missing expected token: {0}' -f $required)
    }
}

$report = Get-Content -Path $reportPath -Raw
if (-not $report.Contains('only probes literal HTTP API paths')) {
    throw 'Report did not describe the Repair3 literal path behavior.'
}

Write-Host 'P10.3D Repair3 Admin Web page API interaction coverage validation passed.'
