Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$webRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$sourceRoot = Join-Path $webRoot 'src'
$appPath = Join-Path $sourceRoot 'App.tsx'
$layoutPath = Join-Path $sourceRoot 'components\Layout.tsx'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2CA-AdminWebNavigationDeduplication.Report.md'

if (-not (Test-Path -Path $appPath -PathType Leaf)) {
    throw ('Missing App.tsx: {0}' -f $appPath)
}
if (-not (Test-Path -Path $layoutPath -PathType Leaf)) {
    throw ('Missing Layout.tsx: {0}' -f $layoutPath)
}
if (-not (Test-Path -Path $reportPath -PathType Leaf)) {
    throw ('Missing report: {0}' -f $reportPath)
}

$appContent = Get-Content -Path $appPath -Raw
$layoutContent = Get-Content -Path $layoutPath -Raw

if ($layoutContent -match '\.tsx[''\"]') {
    throw 'Layout.tsx contains an extension-bearing TypeScript import.'
}

$routeMatches = [regex]::Matches($appContent, 'path\s*=\s*[''\"]([^''\"]+)[''\"]')
$routePaths = New-Object 'System.Collections.Generic.HashSet[string]'
foreach ($match in $routeMatches) {
    if ($null -ne $match -and $match.Groups.Count -gt 1) {
        $routeValue = $match.Groups[1].Value
        if (-not [string]::IsNullOrWhiteSpace($routeValue)) {
            [void]$routePaths.Add($routeValue)
        }
    }
}

$navMatches = [regex]::Matches($layoutContent, 'to\s*:\s*[''\"]([^''\"]+)[''\"]')
$navPaths = New-Object 'System.Collections.Generic.List[string]'
$seen = New-Object 'System.Collections.Generic.HashSet[string]'
$duplicates = New-Object 'System.Collections.Generic.List[string]'
foreach ($match in $navMatches) {
    if ($null -ne $match -and $match.Groups.Count -gt 1) {
        $navPath = $match.Groups[1].Value
        if (-not [string]::IsNullOrWhiteSpace($navPath)) {
            [void]$navPaths.Add($navPath)
            if (-not $seen.Add($navPath)) {
                [void]$duplicates.Add($navPath)
            }
        }
    }
}

if ($duplicates.Count -gt 0) {
    throw ('Duplicate navigation paths remain: {0}' -f ([string]::Join(', ', $duplicates.ToArray())))
}

foreach ($navPath in $navPaths) {
    if (-not $routePaths.Contains($navPath)) {
        throw ('Navigation path is not route-backed in App.tsx: {0}' -f $navPath)
    }
}

$operationalEventsExists = $routePaths.Contains('/operations/operational-events') -or $routePaths.Contains('/operational-events')
if ($operationalEventsExists) {
    $hasNavOperationalEvents = $false
    foreach ($navPath in $navPaths) {
        if ($navPath -eq '/operations/operational-events' -or $navPath -eq '/operational-events') {
            $hasNavOperationalEvents = $true
        }
    }
    if (-not $hasNavOperationalEvents) {
        throw 'Operational Events route exists but navigation does not include it.'
    }
}

$reportContent = Get-Content -Path $reportPath -Raw
if ($reportContent -notmatch 'Route-backed navigation entries') {
    throw 'Report is missing route-backed navigation section.'
}

Write-Host 'P10.2CA Admin Web navigation deduplication validation passed.'
