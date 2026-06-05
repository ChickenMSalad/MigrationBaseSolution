Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$apply = Join-Path $scriptRoot 'Apply-P10.3E-Repair2-AdminWebOperatorWorkflowAcceptance.ps1'
$runner = Join-Path $scriptRoot 'Run-P10.3E-Repair2-AdminWebOperatorWorkflowAcceptance.ps1'
$report = Join-Path $repoRoot 'docs\P10\P10.3E-Repair2-AdminWebOperatorWorkflowAcceptance.md'

$required = @($apply, $runner, $report)
foreach ($path in $required) {
    if (-not (Test-Path $path)) {
        throw ('Required file missing: {0}' -f $path)
    }
}

foreach ($path in @($apply, $runner)) {
    $content = Get-Content -Path $path -Raw
    [void][scriptblock]::Create($content)
}

$runnerText = Get-Content -Path $runner -Raw
if ($runnerText -notlike '*curl.exe*') {
    throw 'Runner does not reference curl.exe for local HTTPS probes.'
}
if ($runnerText -notlike '*--max-time*') {
    throw 'Runner does not include bounded curl timeout handling.'
}

Write-Host 'P10.3E Repair2 Admin Web operator workflow acceptance validation passed.'
