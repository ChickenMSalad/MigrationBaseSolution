Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $scriptRoot = $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
        $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    }

    $candidate = Resolve-Path -LiteralPath (Join-Path $scriptRoot '..\..\..')
    return $candidate.Path
}

function Get-RelativeImportPath {
    param(
        [Parameter(Mandatory=$true)][string]$FromFile,
        [Parameter(Mandatory=$true)][string]$ToFile
    )

    $fromDirectory = Split-Path -Parent $FromFile
    $fromUri = New-Object System.Uri(($fromDirectory.TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar))
    $toUri = New-Object System.Uri($ToFile)
    $relativeUri = $fromUri.MakeRelativeUri($toUri)
    $relativePath = [System.Uri]::UnescapeDataString($relativeUri.ToString())
    $relativePath = $relativePath -replace '\\.tsx$', ''
    if (-not $relativePath.StartsWith('.')) {
        $relativePath = './' + $relativePath
    }
    return $relativePath
}

function Add-ImportIfMissing {
    param(
        [Parameter(Mandatory=$true)][string]$AppPath,
        [Parameter(Mandatory=$true)][string]$ImportLine
    )

    $lines = New-Object System.Collections.Generic.List[string]
    $existingLines = [System.IO.File]::ReadAllLines($AppPath)
    foreach ($line in $existingLines) { [void]$lines.Add($line) }

    foreach ($line in $lines) {
        if ($line -eq $ImportLine) {
            return $false
        }
    }

    $insertIndex = 0
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $trimmed = $lines[$i].TrimStart()
        if ($trimmed.StartsWith('import ')) {
            $insertIndex = $i + 1
        }
    }

    $lines.Insert($insertIndex, $ImportLine)
    [System.IO.File]::WriteAllLines($AppPath, $lines.ToArray())
    return $true
}

function Add-RouteIfMissing {
    param(
        [Parameter(Mandatory=$true)][string]$AppPath,
        [Parameter(Mandatory=$true)][string]$RouteLine
    )

    $lines = New-Object System.Collections.Generic.List[string]
    $existingLines = [System.IO.File]::ReadAllLines($AppPath)
    foreach ($line in $existingLines) { [void]$lines.Add($line) }

    foreach ($line in $lines) {
        if ($line -like '*<OperationalEvents*') {
            return $false
        }
        if ($line -like '*operations/operational-events*') {
            return $false
        }
    }

    $insertIndex = -1
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -like '*<CommandCenter*') {
            $insertIndex = $i + 1
        }
    }

    if ($insertIndex -lt 0) {
        for ($i = 0; $i -lt $lines.Count; $i++) {
            if ($lines[$i].Trim() -eq '</Routes>') {
                $insertIndex = $i
                break
            }
        }
    }

    if ($insertIndex -lt 0) {
        throw 'Unable to find a safe insertion point for the Operational Events route.'
    }

    $lines.Insert($insertIndex, $RouteLine)
    [System.IO.File]::WriteAllLines($AppPath, $lines.ToArray())
    return $true
}

$repoRoot = Get-RepoRoot
$sourceRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web\src'
$appPath = Join-Path $sourceRoot 'App.tsx'
$pagePath = Join-Path $sourceRoot 'features\operations\operationalEvents\pages\OperationalEvents.tsx'
$docsRoot = Join-Path $repoRoot 'docs\P10'
$reportPath = Join-Path $docsRoot 'P10.2BL-AdminWebOperationalEventsRouteReachability.md'

if (-not (Test-Path -LiteralPath $appPath -PathType Leaf)) {
    throw ('App.tsx was not found: {0}' -f $appPath)
}

if (-not (Test-Path -LiteralPath $pagePath -PathType Leaf)) {
    throw ('Operational Events page was not found: {0}' -f $pagePath)
}

if (-not (Test-Path -LiteralPath $docsRoot -PathType Container)) {
    New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null
}

$importPath = Get-RelativeImportPath -FromFile $appPath -ToFile $pagePath
$importLine = ('import {{ OperationalEvents }} from "{0}";' -f $importPath)
$routeLine = '          <Route path="/operations/operational-events" element={<OperationalEvents />} />'

$importAdded = Add-ImportIfMissing -AppPath $appPath -ImportLine $importLine
$routeAdded = Add-RouteIfMissing -AppPath $appPath -RouteLine $routeLine

$appContent = [System.IO.File]::ReadAllText($appPath)
if ($appContent -notlike '*OperationalEvents*') {
    throw 'OperationalEvents is still not referenced by App.tsx after apply.'
}
if ($appContent -notlike '*operations/operational-events*') {
    throw 'Operational Events route path is still not present in App.tsx after apply.'
}

$report = New-Object System.Collections.Generic.List[string]
[void]$report.Add('# P10.2BL - Admin Web Operational Events Route Reachability')
[void]$report.Add('')
[void]$report.Add(('App.tsx: `{0}`' -f $appPath))
[void]$report.Add(('Operational Events page: `{0}`' -f $pagePath))
[void]$report.Add('')
[void]$report.Add(('Import added: `{0}`' -f $importAdded))
[void]$report.Add(('Route added: `{0}`' -f $routeAdded))
[void]$report.Add('')
[void]$report.Add('Canonical route: `/operations/operational-events`')
[void]$report.Add('')
[void]$report.Add('This set restores reachability for the existing Operational Events page without changing other routes or moving files.')
[System.IO.File]::WriteAllLines($reportPath, $report.ToArray())

Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2BL Admin Web Operational Events route reachability applied.'
