Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $scriptRoot '..\..\..'))
$sourceRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web\src'
$appPath = Join-Path $sourceRoot 'App.tsx'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2BH-AdminWebLocalBuildReadinessVerification.Report.md'

if (-not (Test-Path -Path $sourceRoot -PathType Container)) {
    throw ('Canonical Admin Web src folder missing: {0}' -f $sourceRoot)
}
if (-not (Test-Path -Path $appPath -PathType Leaf)) {
    throw ('Canonical App.tsx missing: {0}' -f $appPath)
}
if (-not (Test-Path -Path $reportPath -PathType Leaf)) {
    throw ('Expected report missing: {0}' -f $reportPath)
}

$appContent = Get-Content -Path $appPath -Raw
if ([string]::IsNullOrWhiteSpace($appContent)) {
    throw ('App.tsx was empty: {0}' -f $appPath)
}
if (-not $appContent.Contains('export default function App')) {
    throw 'App.tsx does not contain export default function App.'
}
if (-not $appContent.Contains('react-router-dom')) {
    throw 'App.tsx does not contain react-router-dom import usage.'
}

$reportContent = Get-Content -Path $reportPath -Raw
if (-not $reportContent.Contains('P10.2BH - Admin Web Local Build Readiness Verification Report')) {
    throw ('Report did not contain expected title: {0}' -f $reportPath)
}
if (-not $reportContent.Contains('App.tsx local posture')) {
    throw ('Report did not contain App.tsx posture section: {0}' -f $reportPath)
}
if (-not $reportContent.Contains('Remaining canonical flat folders')) {
    throw ('Report did not contain flat-folder section: {0}' -f $reportPath)
}

Write-Host 'P10.2BH Admin Web local build readiness verification passed.'
