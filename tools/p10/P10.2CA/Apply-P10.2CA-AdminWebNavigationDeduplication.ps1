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
    throw ('Required App.tsx was not found: {0}' -f $appPath)
}

if (-not (Test-Path -Path $layoutPath -PathType Leaf)) {
    throw ('Required Layout.tsx was not found: {0}' -f $layoutPath)
}

$appContent = Get-Content -Path $appPath -Raw
$layoutContent = Get-Content -Path $layoutPath -Raw

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

if ($routePaths.Count -eq 0) {
    throw 'No route paths were found in App.tsx; refusing to rewrite navigation.'
}

$candidates = @(
    [pscustomobject]@{ Path = '/'; Label = 'Dashboard'; Icon = 'Home'; End = $true },
    [pscustomobject]@{ Path = '/projects'; Label = 'Projects'; Icon = 'FolderKanban'; End = $false },
    [pscustomobject]@{ Path = '/runs'; Label = 'Runs'; Icon = 'Activity'; End = $false },
    [pscustomobject]@{ Path = '/runtime-dashboard'; Label = 'Runtime Dashboard'; Icon = 'Gauge'; End = $false },
    [pscustomobject]@{ Path = '/execution-sessions'; Label = 'Execution Sessions'; Icon = 'Workflow'; End = $false },
    [pscustomobject]@{ Path = '/failure-retry'; Label = 'Failure Retry'; Icon = 'RefreshCcw'; End = $false },
    [pscustomobject]@{ Path = '/execution-worker-telemetry'; Label = 'Worker Telemetry'; Icon = 'Activity'; End = $false },
    [pscustomobject]@{ Path = '/command-center'; Label = 'Command Center'; Icon = 'Boxes'; End = $false },
    [pscustomobject]@{ Path = '/operations/operational-events'; Label = 'Operational Events'; Icon = 'Activity'; End = $false },
    [pscustomobject]@{ Path = '/operational-events'; Label = 'Operational Events'; Icon = 'Activity'; End = $false },
    [pscustomobject]@{ Path = '/connectors'; Label = 'Connectors'; Icon = 'PlugZap'; End = $false },
    [pscustomobject]@{ Path = '/connector-configuration'; Label = 'Connector Configuration'; Icon = 'Settings'; End = $false },
    [pscustomobject]@{ Path = '/credentials'; Label = 'Credentials'; Icon = 'KeyRound'; End = $false },
    [pscustomobject]@{ Path = '/credential-vault'; Label = 'Credential Vault'; Icon = 'KeyRound'; End = $false },
    [pscustomobject]@{ Path = '/notification-routing'; Label = 'Notification Routing'; Icon = 'GitBranch'; End = $false },
    [pscustomobject]@{ Path = '/audit-trail'; Label = 'Audit Trail'; Icon = 'Activity'; End = $false },
    [pscustomobject]@{ Path = '/artifacts'; Label = 'Artifacts'; Icon = 'Amphora'; End = $false },
    [pscustomobject]@{ Path = '/capacity-forecast'; Label = 'Capacity Forecast'; Icon = 'Gauge'; End = $false },
    [pscustomobject]@{ Path = '/cost-analytics'; Label = 'Cost Analytics'; Icon = 'Activity'; End = $false },
    [pscustomobject]@{ Path = '/manifest-builder'; Label = 'Manifest Builder'; Icon = 'FileSpreadsheet'; End = $false },
    [pscustomobject]@{ Path = '/taxonomy-builder'; Label = 'Taxonomy Builder'; Icon = 'Tags'; End = $false },
    [pscustomobject]@{ Path = '/mapping-builder'; Label = 'Mapping Builder'; Icon = 'Map'; End = $false }
)

$selected = New-Object 'System.Collections.Generic.List[object]'
$seenLabels = New-Object 'System.Collections.Generic.HashSet[string]'
foreach ($candidate in $candidates) {
    $candidatePath = [string]$candidate.Path
    $candidateLabel = [string]$candidate.Label
    if ($routePaths.Contains($candidatePath) -and -not $seenLabels.Contains($candidateLabel)) {
        [void]$selected.Add($candidate)
        [void]$seenLabels.Add($candidateLabel)
    }
}

if ($selected.Count -eq 0) {
    throw 'No route-backed navigation entries were selected; refusing to rewrite Layout.tsx.'
}

$navLines = New-Object 'System.Collections.Generic.List[string]'
[void]$navLines.Add('const nav = [')
for ($i = 0; $i -lt $selected.Count; $i++) {
    $item = $selected[$i]
    $endText = ''
    if ([bool]$item.End) {
        $endText = ', end: true'
    }
    $comma = ','
    if ($i -eq ($selected.Count - 1)) {
        $comma = ''
    }
    [void]$navLines.Add(('  {{ to: "{0}", label: "{1}", icon: {2}{3} }}{4}' -f $item.Path, $item.Label, $item.Icon, $endText, $comma))
}
[void]$navLines.Add('];')
$newNav = [string]::Join([Environment]::NewLine, $navLines.ToArray())

$navPattern = 'const\s+nav\s*=\s*\[[\s\S]*?\];'
$navMatch = [regex]::Match($layoutContent, $navPattern)
if (-not $navMatch.Success) {
    throw 'Unable to locate the const nav = [...] block in Layout.tsx.'
}

$updatedLayout = [regex]::Replace($layoutContent, $navPattern, [System.Text.RegularExpressions.MatchEvaluator]{ param($m) $newNav }, 1)
if ($updatedLayout -eq $layoutContent) {
    Write-Host 'Layout.tsx navigation was already normalized.'
} else {
    Set-Content -Path $layoutPath -Value $updatedLayout -Encoding UTF8
    Write-Host ('Updated navigation block: {0}' -f $layoutPath)
}

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2CA - Admin Web Navigation Deduplication')
[void]$report.Add('')
[void]$report.Add(('App.tsx: `{0}`' -f $appPath))
[void]$report.Add(('Layout.tsx: `{0}`' -f $layoutPath))
[void]$report.Add('')
[void]$report.Add('## Route-backed navigation entries')
foreach ($item in $selected) {
    [void]$report.Add(('- `{0}` -> {1}' -f $item.Path, $item.Label))
}
[void]$report.Add('')
[void]$report.Add('## Known route paths observed')
$routeList = New-Object 'System.Collections.Generic.List[string]'
foreach ($route in $routePaths) {
    [void]$routeList.Add($route)
}
$routeArray = $routeList.ToArray()
[Array]::Sort($routeArray)
foreach ($route in $routeArray) {
    [void]$report.Add(('- `{0}`' -f $route))
}

$reportDirectory = Split-Path -Parent $reportPath
if (-not (Test-Path -Path $reportDirectory -PathType Container)) {
    New-Item -Path $reportDirectory -ItemType Directory -Force | Out-Null
}
Set-Content -Path $reportPath -Value $report.ToArray() -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2CA Admin Web navigation deduplication applied.'
