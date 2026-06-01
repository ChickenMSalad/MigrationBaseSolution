Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$repoRoot = $repoRoot.ProviderPath

$adminRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web\src'
$appsRoot = Join-Path $repoRoot 'apps\migration-admin-ui\src'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2AW-Repair-AdminWebNonOverwriteAppsParityRefresh.md'

if (-not (Test-Path -Path $adminRoot -PathType Container)) {
    throw ('Canonical Admin Web source root was not found: {0}' -f $adminRoot)
}
if (-not (Test-Path -Path $appsRoot -PathType Container)) {
    throw ('Apps source root was not found: {0}' -f $appsRoot)
}
if (-not (Test-Path -Path $reportPath -PathType Leaf)) {
    throw ('Expected report was not found: {0}' -f $reportPath)
}

$requiredCanonicalFolders = @('features', 'components', 'auth', 'lib')
foreach ($folder in $requiredCanonicalFolders) {
    $path = Join-Path $adminRoot $folder
    if (-not (Test-Path -Path $path -PathType Container)) {
        throw ('Expected canonical folder was not found: {0}' -f $path)
    }
}

$reportLines = @(Get-Content -Path $reportPath)
if ($reportLines.Length -eq 0) {
    throw ('Report was empty: {0}' -f $reportPath)
}

$summaryLine = $reportLines | Where-Object { $_ -like '- Copied files:*' } | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($summaryLine)) {
    throw ('Report did not contain copied-files summary: {0}' -f $reportPath)
}

Write-Host 'P10.2AW Repair Admin Web non-overwrite apps parity refresh validation passed.'
