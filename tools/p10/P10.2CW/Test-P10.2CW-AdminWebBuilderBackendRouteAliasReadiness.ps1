Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$repoRootPath = $repoRoot.Path
$reportPath = Join-Path $repoRootPath 'docs\P10\P10.2CW-AdminWebBuilderBackendRouteAliasReadiness.md'
$applyPath = Join-Path $scriptRoot 'Apply-P10.2CW-AdminWebBuilderBackendRouteAliasReadiness.ps1'

if (-not (Test-Path -LiteralPath $applyPath)) {
    throw ('Apply script missing: {0}' -f $applyPath)
}
if (-not (Test-Path -LiteralPath $reportPath)) {
    throw ('Expected report missing: {0}' -f $reportPath)
}

$report = Get-Content -LiteralPath $reportPath -Raw
foreach ($required in @('# P10.2CW', 'Builder UI files', 'Backend controller/source candidates', 'Route alias readiness')) {
    if ($report.IndexOf($required, [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw ('Report missing required section/content: {0}' -f $required)
    }
}

$adminWebSrc = Join-Path $repoRootPath 'src\Admin\Migration.Admin.Web\src'
if (-not (Test-Path -LiteralPath $adminWebSrc)) {
    throw ('Admin Web source root missing: {0}' -f $adminWebSrc)
}

Write-Host 'P10.2CW Admin Web builder backend route alias readiness validation passed.'
