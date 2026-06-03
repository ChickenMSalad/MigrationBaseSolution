Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$repoRoot = $repoRoot.ProviderPath

$applyPath = Join-Path $scriptRoot 'Apply-P10.3B-AdminWebRuntimeRouteAcceptance.ps1'
$runnerPath = Join-Path $scriptRoot 'Run-P10.3B-AdminWebRuntimeRouteAcceptance.ps1'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.3B-AdminWebRuntimeRouteAcceptance.md'
$appPath = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web\src\App.tsx'
$layoutPath = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web\src\components\Layout.tsx'

foreach ($path in @($applyPath, $runnerPath, $reportPath, $appPath, $layoutPath)) {
    if (-not (Test-Path -LiteralPath $path)) { throw ('Required file missing: {0}' -f $path) }
}

foreach ($path in @($applyPath, $runnerPath, $MyInvocation.MyCommand.Path)) {
    $text = Get-Content -LiteralPath $path -Raw
    [void][scriptblock]::Create($text)
}

$reportText = Get-Content -LiteralPath $reportPath -Raw
if ($reportText.IndexOf('Runtime Route Acceptance') -lt 0) { throw 'Report does not contain the expected route acceptance heading.' }
if ($reportText.IndexOf('Run-P10.3B-AdminWebRuntimeRouteAcceptance.ps1') -lt 0) { throw 'Report does not mention the runtime route acceptance runner.' }

$runnerText = Get-Content -LiteralPath $runnerPath -Raw
if ($runnerText.IndexOf('param(') -lt 0) { throw 'Runner does not contain a param block.' }
if ($runnerText.IndexOf('TimeoutSec') -lt 0) { throw 'Runner does not contain bounded web request timeout usage.' }
if ($runnerText.IndexOf('runtime-route-acceptance.summary.md') -lt 0) { throw 'Runner does not write the expected summary.' }

Write-Host 'P10.3B Admin Web runtime route acceptance validation passed.'
