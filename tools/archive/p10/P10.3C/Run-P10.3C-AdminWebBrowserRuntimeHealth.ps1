param(
    [string]$AdminWebBaseUrl = 'http://localhost:5173',
    [int]$TimeoutSeconds = 5,
    [int]$MaxRoutes = 25
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Join-Url {
    param(
        [string]$BaseUrl,
        [string]$Path
    )

    $baseValue = $BaseUrl.TrimEnd('/')
    if ([string]::IsNullOrWhiteSpace($Path) -or $Path -eq '/') {
        return $baseValue + '/'
    }

    return $baseValue + '/' + $Path.TrimStart('/')
}

function Add-ProbeResult {
    param(
        [System.Collections.Generic.List[object]]$Results,
        [string]$Kind,
        [string]$Name,
        [string]$Url,
        [string]$Status,
        [int]$StatusCode,
        [string]$Message
    )

    [void]$Results.Add([pscustomobject]@{
        Kind = $Kind
        Name = $Name
        Url = $Url
        Status = $Status
        StatusCode = $StatusCode
        Message = $Message
    })
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$artifactsRoot = Join-Path $repoRoot 'artifacts\p10\P10.3C'
$summaryPath = Join-Path $artifactsRoot 'browser-runtime-health.summary.md'
$detailsPath = Join-Path $artifactsRoot 'browser-runtime-health.details.csv'
$appPath = Join-Path $adminWebRoot 'src\App.tsx'

New-Item -ItemType Directory -Force -Path $artifactsRoot | Out-Null

$results = New-Object 'System.Collections.Generic.List[object]'
$summary = New-Object 'System.Collections.Generic.List[string]'
$startedUtc = [DateTime]::UtcNow.ToString('o')

[void]$summary.Add('# P10.3C - Admin Web Browser Runtime Health')
[void]$summary.Add('')
[void]$summary.Add(('Started UTC: `{0}`' -f $startedUtc))
[void]$summary.Add(('Admin Web base URL: `{0}`' -f $AdminWebBaseUrl))
[void]$summary.Add(('Timeout seconds: `{0}`' -f $TimeoutSeconds))
[void]$summary.Add(('Max routes: `{0}`' -f $MaxRoutes))
[void]$summary.Add('')
Set-Content -LiteralPath $summaryPath -Value $summary.ToArray() -Encoding UTF8

try {
    $rootUrl = Join-Url -BaseUrl $AdminWebBaseUrl -Path '/'
    $rootResponse = Invoke-WebRequest -Uri $rootUrl -UseBasicParsing -TimeoutSec $TimeoutSeconds
    Add-ProbeResult -Results $results -Kind 'Html' -Name '/' -Url $rootUrl -Status 'Success' -StatusCode ([int]$rootResponse.StatusCode) -Message 'Root HTML responded.'
} catch {
    Add-ProbeResult -Results $results -Kind 'Html' -Name '/' -Url (Join-Url -BaseUrl $AdminWebBaseUrl -Path '/') -Status 'RequestFailed' -StatusCode 0 -Message $_.Exception.Message
    $results | Export-Csv -LiteralPath $detailsPath -NoTypeInformation -Encoding UTF8
    [void]$summary.Add('Root HTML failed; asset and route probes were skipped.')
    [void]$summary.Add(('Finished UTC: `{0}`' -f ([DateTime]::UtcNow.ToString('o'))))
    [void]$summary.Add(('Details: `{0}`' -f $detailsPath))
    Set-Content -LiteralPath $summaryPath -Value $summary.ToArray() -Encoding UTF8
    throw ('Admin Web root did not respond. Review {0}' -f $summaryPath)
}

$html = [string]$rootResponse.Content
$assetMatches = [regex]::Matches($html, '(?:src|href)="([^"]+\.(?:js|css))"')
$assetUrls = New-Object 'System.Collections.Generic.List[string]'
foreach ($match in $assetMatches) {
    $assetPath = [string]$match.Groups[1].Value
    if ([string]::IsNullOrWhiteSpace($assetPath)) { continue }
    if ($assetPath.StartsWith('http://') -or $assetPath.StartsWith('https://')) {
        [void]$assetUrls.Add($assetPath)
    } else {
        [void]$assetUrls.Add((Join-Url -BaseUrl $AdminWebBaseUrl -Path $assetPath))
    }
}

foreach ($assetUrl in $assetUrls) {
    try {
        $assetResponse = Invoke-WebRequest -Uri $assetUrl -UseBasicParsing -TimeoutSec $TimeoutSeconds
        Add-ProbeResult -Results $results -Kind 'Asset' -Name $assetUrl -Url $assetUrl -Status 'Success' -StatusCode ([int]$assetResponse.StatusCode) -Message 'Asset responded.'
    } catch {
        Add-ProbeResult -Results $results -Kind 'Asset' -Name $assetUrl -Url $assetUrl -Status 'RequestFailed' -StatusCode 0 -Message $_.Exception.Message
    }
}

$routes = New-Object 'System.Collections.Generic.List[string]'
[void]$routes.Add('/')
if (Test-Path -LiteralPath $appPath -PathType Leaf) {
    $appText = Get-Content -LiteralPath $appPath -Raw
    $routeMatches = [regex]::Matches($appText, 'path\s*=\s*[''\"]([^''\"]+)[''\"]')
    foreach ($routeMatch in $routeMatches) {
        $route = [string]$routeMatch.Groups[1].Value
        if ([string]::IsNullOrWhiteSpace($route)) { continue }
        if ($route.Contains(':')) { continue }
        if ($route -eq '*') { continue }
        if (-not $route.StartsWith('/')) { $route = '/' + $route }
        if (-not $routes.Contains($route)) {
            [void]$routes.Add($route)
        }
        if ($routes.Count -ge $MaxRoutes) { break }
    }
}

foreach ($route in $routes) {
    $routeUrl = Join-Url -BaseUrl $AdminWebBaseUrl -Path $route
    try {
        $routeResponse = Invoke-WebRequest -Uri $routeUrl -UseBasicParsing -TimeoutSec $TimeoutSeconds
        $message = 'Route responded.'
        if ([string]$routeResponse.Content -notmatch '<div\s+id="root"') {
            $message = 'Route responded but root mount marker was not found.'
        }
        Add-ProbeResult -Results $results -Kind 'Route' -Name $route -Url $routeUrl -Status 'Success' -StatusCode ([int]$routeResponse.StatusCode) -Message $message
    } catch {
        Add-ProbeResult -Results $results -Kind 'Route' -Name $route -Url $routeUrl -Status 'RequestFailed' -StatusCode 0 -Message $_.Exception.Message
    }
}

$results | Export-Csv -LiteralPath $detailsPath -NoTypeInformation -Encoding UTF8

$total = $results.Count
$success = 0
foreach ($result in $results) {
    if ($result.Status -eq 'Success') { $success++ }
}
$failed = $total - $success

[void]$summary.Add(('Finished UTC: `{0}`' -f ([DateTime]::UtcNow.ToString('o'))))
[void]$summary.Add(('Total probes: `{0}`' -f $total))
[void]$summary.Add(('Successful probes: `{0}`' -f $success))
[void]$summary.Add(('Non-success probes: `{0}`' -f $failed))
[void]$summary.Add(('Assets discovered: `{0}`' -f $assetUrls.Count))
[void]$summary.Add(('Routes discovered: `{0}`' -f $routes.Count))
[void]$summary.Add('')
[void]$summary.Add(('Details: `{0}`' -f $detailsPath))
Set-Content -LiteralPath $summaryPath -Value $summary.ToArray() -Encoding UTF8

Write-Host ('Wrote summary: {0}' -f $summaryPath)
Write-Host ('Wrote details: {0}' -f $detailsPath)

if ($failed -gt 0) {
    throw ('Browser runtime health completed with {0} non-success probes. Review {1}' -f $failed, $summaryPath)
}

Write-Host 'P10.3C Admin Web browser runtime health completed successfully.'
