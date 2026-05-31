[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$toolRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($toolRoot)) {
    if (-not [string]::IsNullOrWhiteSpace($PSCommandPath)) {
        $toolRoot = Split-Path -Parent $PSCommandPath
    }
}
if ([string]::IsNullOrWhiteSpace($toolRoot)) {
    throw 'Unable to resolve validator root.'
}

$repoRoot = Split-Path -Parent $toolRoot

$requiredFiles = @(
    'docs\p10\P10.2D-Admin-Web-Runtime-Dashboard-Client.md',
    'config-samples\p10-runtime-dashboard-web-client.sample.json',
    'src\Admin\Migration.Admin.Web\src\types\runtimeDashboard.ts',
    'src\Admin\Migration.Admin.Web\src\api\runtimeDashboardApi.ts',
    'src\Admin\Migration.Admin.Web\src\pages\RuntimeDashboard.tsx'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw ('Required P10.2D file is missing: {0}' -f $relativePath)
    }
}

$apiPath = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web\src\api\runtimeDashboardApi.ts'
$apiText = Get-Content -LiteralPath $apiPath -Raw
foreach ($term in @('/api/runtime/dashboard/summary', '/api/runtime/dashboard/runs', 'runtimeDashboardApi', 'queryString')) {
    if ($apiText.IndexOf($term, [System.StringComparison]::Ordinal) -lt 0) {
        throw ('Runtime dashboard API client is missing expected term: {0}' -f $term)
    }
}

$pagePath = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web\src\pages\RuntimeDashboard.tsx'
$pageText = Get-Content -LiteralPath $pagePath -Raw
foreach ($term in @('RuntimeDashboard', 'runtimeDashboardApi.summary', 'runtimeDashboardApi.runs', 'Recent runtime runs')) {
    if ($pageText.IndexOf($term, [System.StringComparison]::Ordinal) -lt 0) {
        throw ('Runtime dashboard page is missing expected term: {0}' -f $term)
    }
}

$typePath = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web\src\types\runtimeDashboard.ts'
$typeText = Get-Content -LiteralPath $typePath -Raw
foreach ($term in @('RuntimeDashboardSummary', 'RuntimeDashboardRun', 'RuntimeDashboardRunDetail')) {
    if ($typeText.IndexOf($term, [System.StringComparison]::Ordinal) -lt 0) {
        throw ('Runtime dashboard type file is missing expected term: {0}' -f $term)
    }
}

Write-Host 'P10.2D admin web runtime dashboard client validation passed.'
