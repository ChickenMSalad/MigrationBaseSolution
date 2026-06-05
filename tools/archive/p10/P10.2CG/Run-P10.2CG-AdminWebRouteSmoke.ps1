param(
    [string]$BaseUrl = 'http://localhost:5173'
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    if ($PSScriptRoot) {
        return (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
    }

    return (Get-Location).Path
}

function Join-Url {
    param(
        [string]$Root,
        [string]$Path
    )

    $trimmedRoot = $Root.TrimEnd('/')
    if ([string]::IsNullOrWhiteSpace($Path) -or $Path -eq '/') {
        return ($trimmedRoot + '/')
    }

    return ($trimmedRoot + '/' + $Path.TrimStart('/'))
}

$repoRoot = Get-RepoRoot
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$appTsx = Join-Path $adminWebRoot 'src\App.tsx'
$artifactRoot = Join-Path $repoRoot 'artifacts\p10\P10.2CG'
$reportPath = Join-Path $artifactRoot 'P10.2CG-AdminWebRouteSmoke.Report.md'

if (-not (Test-Path -Path $appTsx -PathType Leaf)) {
    throw ('App.tsx was not found: {0}' -f $appTsx)
}

if (-not (Test-Path -Path $artifactRoot -PathType Container)) {
    New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
}

$appText = Get-Content -Path $appTsx -Raw
$routeMatches = [System.Text.RegularExpressions.Regex]::Matches($appText, 'path\s*=\s*"([^"]+)"')
$routes = New-Object 'System.Collections.Generic.List[string]'

foreach ($match in $routeMatches) {
    if ($null -eq $match) { continue }
    if ($match.Groups.Count -lt 2) { continue }

    $routePath = $match.Groups[1].Value
    if ([string]::IsNullOrWhiteSpace($routePath)) { continue }
    if ($routePath.Contains('*')) { continue }
    if (-not $routes.Contains($routePath)) {
        [void]$routes.Add($routePath)
    }
}

if (-not $routes.Contains('/')) {
    [void]$routes.Insert(0, '/')
}

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2CG - Admin Web Route Smoke Report')
[void]$report.Add('')
[void]$report.Add(('Base URL: `{0}`' -f $BaseUrl))
[void]$report.Add(('Route count: `{0}`' -f $routes.Count))
[void]$report.Add('')
[void]$report.Add('| Route | URL | Status | Result |')
[void]$report.Add('|---|---|---:|---|')

$failures = 0
foreach ($route in $routes) {
    $url = Join-Url -Root $BaseUrl -Path $route
    $statusCode = 0
    $result = 'Failed'

    try {
        $response = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 10
        $statusCode = [int]$response.StatusCode
        if ($statusCode -ge 200 -and $statusCode -lt 500) {
            $result = 'Passed'
        } else {
            $failures++
        }
    }
    catch {
        $message = $_.Exception.Message
        if ([string]::IsNullOrWhiteSpace($message)) {
            $message = 'Request failed'
        }
        $result = $message.Replace('|', '/')
        $failures++
    }

    [void]$report.Add(('| `{0}` | `{1}` | {2} | {3} |' -f $route, $url, $statusCode, $result))
}

$report | Set-Content -Path $reportPath -Encoding UTF8
Write-Host ('Wrote route smoke report: {0}' -f $reportPath)

if ($failures -gt 0) {
    throw ('Admin Web route smoke completed with {0} failure(s). Review {1}.' -f $failures, $reportPath)
}

Write-Host 'P10.2CG Admin Web route smoke passed.'
