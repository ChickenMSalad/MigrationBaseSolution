param(
    [string]$AdminWebBaseUrl = 'http://localhost:5173',
    [int]$TimeoutSeconds = 5,
    [int]$MaxRoutes = 50
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$repoRoot = $repoRoot.ProviderPath

$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$srcRoot = Join-Path $adminWebRoot 'src'
$appPath = Join-Path $srcRoot 'App.tsx'
$layoutPath = Join-Path $srcRoot 'components\Layout.tsx'
$artifactRoot = Join-Path $repoRoot 'artifacts\p10\P10.3B'
$summaryPath = Join-Path $artifactRoot 'runtime-route-acceptance.summary.md'
$detailsPath = Join-Path $artifactRoot 'runtime-route-acceptance.details.csv'

if (-not (Test-Path -LiteralPath $appPath)) { throw ('App.tsx was not found: {0}' -f $appPath) }
if (-not (Test-Path -LiteralPath $layoutPath)) { throw ('Layout.tsx was not found: {0}' -f $layoutPath) }
New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null

$baseUrl = $AdminWebBaseUrl.TrimEnd('/')
$startedUtc = [DateTime]::UtcNow

$routeSet = New-Object 'System.Collections.Generic.HashSet[string]'
[void]$routeSet.Add('/')

foreach ($filePath in @($appPath, $layoutPath)) {
    $content = Get-Content -LiteralPath $filePath -Raw
    $routeMatches = [regex]::Matches($content, 'path\s*=\s*["'']([^"'']+)["'']')
    foreach ($match in $routeMatches) {
        if ($match.Groups.Count -gt 1) {
            $candidate = $match.Groups[1].Value
            if (-not [string]::IsNullOrWhiteSpace($candidate)) {
                if ($candidate.StartsWith('/')) { [void]$routeSet.Add($candidate) }
            }
        }
    }
    $navMatches = [regex]::Matches($content, 'to\s*:\s*["'']([^"'']+)["'']')
    foreach ($match in $navMatches) {
        if ($match.Groups.Count -gt 1) {
            $candidate = $match.Groups[1].Value
            if (-not [string]::IsNullOrWhiteSpace($candidate)) {
                if ($candidate.StartsWith('/')) { [void]$routeSet.Add($candidate) }
            }
        }
    }
}

$routes = New-Object 'System.Collections.Generic.List[string]'
foreach ($route in $routeSet) { [void]$routes.Add($route) }
$routes.Sort()

$limitedRoutes = New-Object 'System.Collections.Generic.List[string]'
$count = 0
foreach ($route in $routes) {
    if ($count -ge $MaxRoutes) { break }
    [void]$limitedRoutes.Add($route)
    $count++
}

$details = New-Object 'System.Collections.Generic.List[object]'
foreach ($route in $limitedRoutes) {
    $targetUrl = if ($route -eq '/') { $baseUrl + '/' } else { $baseUrl + $route }
    $status = 'Unknown'
    $statusCode = ''
    $message = ''
    try {
        $response = Invoke-WebRequest -Uri $targetUrl -UseBasicParsing -TimeoutSec $TimeoutSeconds -Method Get
        $statusCode = [string][int]$response.StatusCode
        if ([int]$response.StatusCode -ge 200 -and [int]$response.StatusCode -lt 400) {
            $status = 'Success'
        } else {
            $status = 'NonSuccess'
        }
        $text = [string]$response.Content
        if ($text.IndexOf('<div id="root"') -ge 0 -or $text.IndexOf('<div id=''root''') -ge 0) {
            if ($status -eq 'Success') { $message = 'SPA root found' }
        } else {
            if ($status -eq 'Success') { $message = 'HTTP success; SPA root marker not found' }
        }
    } catch {
        $status = 'RequestFailed'
        $message = $_.Exception.Message
        if ($_.Exception.Response -ne $null) {
            try { $statusCode = [string][int]$_.Exception.Response.StatusCode } catch { $statusCode = '' }
        }
    }
    [void]$details.Add([pscustomobject]@{
        Route = $route
        Url = $targetUrl
        Status = $status
        StatusCode = $statusCode
        Message = $message
    })
    Write-Host ('{0} => {1} {2}' -f $route, $status, $statusCode)
}

$details | Export-Csv -LiteralPath $detailsPath -NoTypeInformation -Encoding UTF8

$successCount = 0
$nonSuccessCount = 0
foreach ($item in $details) {
    if ($item.Status -eq 'Success') { $successCount++ } else { $nonSuccessCount++ }
}

$summary = New-Object 'System.Collections.Generic.List[string]'
[void]$summary.Add('# P10.3B - Admin Web Runtime Route Acceptance')
[void]$summary.Add('')
[void]$summary.Add(('Started UTC: `{0}`' -f $startedUtc.ToString('o')))
[void]$summary.Add(('Finished UTC: `{0}`' -f ([DateTime]::UtcNow.ToString('o'))))
[void]$summary.Add(('Admin Web base URL: `{0}`' -f $baseUrl))
[void]$summary.Add(('Timeout seconds: `{0}`' -f $TimeoutSeconds))
[void]$summary.Add(('Discovered routes: `{0}`' -f $routes.Count))
[void]$summary.Add(('Probed routes: `{0}`' -f $details.Count))
[void]$summary.Add(('Successful probes: `{0}`' -f $successCount))
[void]$summary.Add(('Non-success probes: `{0}`' -f $nonSuccessCount))
[void]$summary.Add('')
[void]$summary.Add(('Details: `{0}`' -f $detailsPath))
Set-Content -LiteralPath $summaryPath -Value $summary.ToArray() -Encoding UTF8

Write-Host ('Wrote summary: {0}' -f $summaryPath)
Write-Host ('Wrote details: {0}' -f $detailsPath)

if ($nonSuccessCount -gt 0) {
    throw ('Admin Web runtime route acceptance completed with {0} non-success route probe(s). Review {1}.' -f $nonSuccessCount, $summaryPath)
}

Write-Host 'Admin Web runtime route acceptance completed successfully.'
