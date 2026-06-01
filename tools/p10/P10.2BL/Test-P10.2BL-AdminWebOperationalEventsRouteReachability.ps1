Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}
$repoRoot = (Resolve-Path -LiteralPath (Join-Path $scriptRoot '..\..\..')).Path
$sourceRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web\src'
$appPath = Join-Path $sourceRoot 'App.tsx'
$pagePath = Join-Path $sourceRoot 'features\operations\operationalEvents\pages\OperationalEvents.tsx'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2BL-AdminWebOperationalEventsRouteReachability.md'

if (-not (Test-Path -LiteralPath $appPath -PathType Leaf)) {
    throw ('Missing App.tsx: {0}' -f $appPath)
}
if (-not (Test-Path -LiteralPath $pagePath -PathType Leaf)) {
    throw ('Missing Operational Events page: {0}' -f $pagePath)
}
if (-not (Test-Path -LiteralPath $reportPath -PathType Leaf)) {
    throw ('Missing P10.2BL report: {0}' -f $reportPath)
}

$appContent = [System.IO.File]::ReadAllText($appPath)
if ($appContent -notlike '*import { OperationalEvents }*') {
    throw 'OperationalEvents import was not found in App.tsx.'
}
if ($appContent -notlike '*features/operations/operationalEvents/pages/OperationalEvents*') {
    throw 'OperationalEvents import path was not found in App.tsx.'
}
if ($appContent -notlike '*path="/operations/operational-events"*') {
    throw 'Operational Events route path was not found in App.tsx.'
}
if ($appContent -notlike '*<OperationalEvents*') {
    throw 'Operational Events route element was not found in App.tsx.'
}

$importOccurrences = ([regex]::Matches($appContent, 'import \{ OperationalEvents \}')).Count
if ($importOccurrences -ne 1) {
    throw ('Expected exactly one OperationalEvents import; found {0}.' -f $importOccurrences)
}
$routeOccurrences = ([regex]::Matches($appContent, 'path="/operations/operational-events"')).Count
if ($routeOccurrences -ne 1) {
    throw ('Expected exactly one Operational Events route; found {0}.' -f $routeOccurrences)
}

Write-Host 'P10.2BL Admin Web Operational Events route reachability validation passed.'
