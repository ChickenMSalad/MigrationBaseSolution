Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.3C-AdminWebBrowserRuntimeHealth.md'
$runnerPath = Join-Path $PSScriptRoot 'Run-P10.3C-AdminWebBrowserRuntimeHealth.ps1'
$applyPath = Join-Path $PSScriptRoot 'Apply-P10.3C-AdminWebBrowserRuntimeHealth.ps1'

if (-not (Test-Path -LiteralPath $adminWebRoot -PathType Container)) {
    throw ('Admin Web root missing: {0}' -f $adminWebRoot)
}
if (-not (Test-Path -LiteralPath $reportPath -PathType Leaf)) {
    throw ('Report missing: {0}' -f $reportPath)
}
if (-not (Test-Path -LiteralPath $runnerPath -PathType Leaf)) {
    throw ('Runner missing: {0}' -f $runnerPath)
}
if (-not (Test-Path -LiteralPath $applyPath -PathType Leaf)) {
    throw ('Apply script missing: {0}' -f $applyPath)
}

$runnerText = Get-Content -LiteralPath $runnerPath -Raw
if ($runnerText -notmatch 'param\s*\(') {
    throw ('Runner is missing a param block: {0}' -f $runnerPath)
}
if ($runnerText -notmatch 'Invoke-WebRequest') {
    throw ('Runner does not probe HTTP responses: {0}' -f $runnerPath)
}
if ($runnerText -notmatch 'TimeoutSec') {
    throw ('Runner does not include request timeouts: {0}' -f $runnerPath)
}

$reportText = Get-Content -LiteralPath $reportPath -Raw
if ($reportText -notmatch 'Browser Runtime Health') {
    throw ('Report does not contain expected heading: {0}' -f $reportPath)
}

Write-Host 'P10.3C Admin Web browser runtime health validation passed.'
