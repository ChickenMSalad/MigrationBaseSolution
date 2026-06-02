Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2CT-AdminWebBuilderWorkspaceSmokeHarness.md'
$runnerPath = Join-Path $scriptRoot 'Run-P10.2CT-AdminWebBuilderWorkspaceSmoke.ps1'
$appPath = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web\src\App.tsx'

if (-not (Test-Path $reportPath)) {
    throw ('Expected report was not found: {0}' -f $reportPath)
}
if (-not (Test-Path $runnerPath)) {
    throw ('Expected smoke runner was not found: {0}' -f $runnerPath)
}
if (-not (Test-Path $appPath)) {
    throw ('Expected App.tsx was not found: {0}' -f $appPath)
}

$report = Get-Content -Path $reportPath -Raw
if ($report.IndexOf('Manifest Builder', [StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw 'Report does not contain Manifest Builder section.'
}
if ($report.IndexOf('Taxonomy Builder', [StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw 'Report does not contain Taxonomy Builder section.'
}
if ($report.IndexOf('Mapping Builder', [StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw 'Report does not contain Mapping Builder section.'
}

$runner = Get-Content -Path $runnerPath -Raw
if ($runner.IndexOf('AdminWebBaseUrl', [StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw 'Smoke runner does not expose AdminWebBaseUrl parameter.'
}
if ($runner.IndexOf('/manifest-builder', [StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw 'Smoke runner does not include manifest builder route.'
}
if ($runner.IndexOf('/taxonomy-builder', [StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw 'Smoke runner does not include taxonomy builder route.'
}
if ($runner.IndexOf('/mapping-builder', [StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw 'Smoke runner does not include mapping builder route.'
}

Write-Host 'P10.2CT Admin Web builder workspace smoke harness validation passed.'
