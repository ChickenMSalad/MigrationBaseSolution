Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$adminRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$srcRoot = Join-Path $adminRoot 'src'
$appPath = Join-Path $srcRoot 'App.tsx'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2CO-AdminWebBuilderReachabilityInventory.md'

if (-not (Test-Path -LiteralPath $adminRoot)) { throw ('Admin Web root not found: {0}' -f $adminRoot) }
if (-not (Test-Path -LiteralPath $appPath)) { throw ('App.tsx not found: {0}' -f $appPath) }
if (-not (Test-Path -LiteralPath $reportPath)) { throw ('Expected report not found: {0}' -f $reportPath) }

$report = Get-Content -LiteralPath $reportPath -Raw
$requiredReportTokens = @(
    'Manifest Builder',
    'Taxonomy Builder',
    'Mapping Builder',
    'Canonical candidate count',
    'Reference candidate count'
)
foreach ($token in $requiredReportTokens) {
    if (-not $report.Contains($token)) {
        throw ('Report missing expected token: {0}' -f $token)
    }
}

$sourceFiles = @(Get-ChildItem -LiteralPath $srcRoot -Recurse -File -Include '*.ts','*.tsx' -ErrorAction SilentlyContinue)
foreach ($file in $sourceFiles) {
    $content = Get-Content -LiteralPath $file.FullName -Raw
    if ($content -match 'from\s+["''][^"'']+\.tsx["'']') {
        throw ('Extension-bearing TSX import found in compiled source: {0}' -f $file.FullName)
    }
    if ($content -match 'from\s+["''][^"'']+reference/') {
        throw ('Compiled source imports reference material: {0}' -f $file.FullName)
    }
}

$appContent = Get-Content -LiteralPath $appPath -Raw
$builderRouteCount = 0
foreach ($route in @('/manifest-builder', '/taxonomy-builder', '/mapping-builder')) {
    if ($appContent.Contains($route)) { $builderRouteCount++ }
}

Write-Host ('Builder route count currently present in App.tsx: {0}' -f $builderRouteCount)
Write-Host 'P10.2CO Admin Web builder reachability inventory validation passed.'
