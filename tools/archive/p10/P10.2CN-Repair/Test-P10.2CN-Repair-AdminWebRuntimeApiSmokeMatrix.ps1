Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$repoRootPath = $repoRoot.Path
$toolRoot = Join-Path $repoRootPath 'tools\p10\P10.2CN-Repair'
$runnerPath = Join-Path $toolRoot 'Run-P10.2CN-Repair-AdminWebRuntimeApiSmokeMatrix.ps1'
$docPath = Join-Path $repoRootPath 'docs\P10\P10.2CN-Repair-AdminWebRuntimeApiSmokeMatrix.md'

if (-not (Test-Path -LiteralPath $runnerPath)) {
    throw ('Expected runner was not found: {0}' -f $runnerPath)
}
if (-not (Test-Path -LiteralPath $docPath)) {
    throw ('Expected documentation was not found: {0}' -f $docPath)
}

$runnerText = Get-Content -LiteralPath $runnerPath -Raw
if (-not $runnerText.Contains('TimeoutSec')) {
    throw 'Runtime smoke runner does not expose TimeoutSec.'
}
if (-not $runnerText.Contains('MaxEndpoints')) {
    throw 'Runtime smoke runner does not expose MaxEndpoints.'
}
if (-not $runnerText.Contains('Set-Content -Path $summaryPath')) {
    throw 'Runtime smoke runner does not write a summary file.'
}
if (-not $runnerText.Contains('Invoke-WebRequest')) {
    throw 'Runtime smoke runner does not perform bounded web probes.'
}
if ($runnerText.Contains('while ($true)')) {
    throw 'Runtime smoke runner contains an unbounded loop.'
}

Write-Host 'P10.2CN Repair Admin Web runtime API smoke matrix validation passed.'
