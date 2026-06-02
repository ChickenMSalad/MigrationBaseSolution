param(
    [string]$AdminWebBaseUrl = 'http://localhost:5173',
    [int]$TimeoutSec = 5
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$repoRoot = $repoRoot.Path

$artifactRoot = Join-Path $repoRoot 'artifacts\p10\P10.2CT-Repair'
if (-not (Test-Path -LiteralPath $artifactRoot)) {
    New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
}

$summaryPath = Join-Path $artifactRoot 'builder-workspace-smoke.summary.md'
$detailsPath = Join-Path $artifactRoot 'builder-workspace-smoke.details.csv'

$baseUrl = $AdminWebBaseUrl.TrimEnd('/')
$routes = New-Object 'System.Collections.Generic.List[string]'
[void]$routes.Add('/manifest-builder')
[void]$routes.Add('/taxonomy-builder')
[void]$routes.Add('/mapping-builder')

$details = New-Object 'System.Collections.Generic.List[object]'
$startedUtc = [DateTime]::UtcNow

foreach ($route in $routes) {
    $url = ('{0}{1}' -f $baseUrl, $route)
    $statusCode = ''
    $result = 'Unknown'
    $message = ''
    try {
        $response = Invoke-WebRequest -Uri $url -Method Get -TimeoutSec $TimeoutSec -UseBasicParsing
        $statusCode = [string][int]$response.StatusCode
        if ([int]$response.StatusCode -ge 200 -and [int]$response.StatusCode -lt 400) {
            $result = 'Success'
        }
        else {
            $result = 'UnexpectedStatus'
        }
        $message = $response.StatusDescription
    }
    catch {
        $result = 'RequestFailed'
        $message = $_.Exception.Message
        if ($_.Exception.Response -ne $null) {
            try {
                $statusCode = [string][int]$_.Exception.Response.StatusCode
            }
            catch {
                $statusCode = ''
            }
        }
    }

    $row = [pscustomobject]@{
        Route = $route
        Url = $url
        Result = $result
        StatusCode = $statusCode
        Message = $message
    }
    [void]$details.Add($row)
}

$details | Export-Csv -LiteralPath $detailsPath -NoTypeInformation -Encoding UTF8

$finishedUtc = [DateTime]::UtcNow
$summary = New-Object 'System.Collections.Generic.List[string]'
[void]$summary.Add('# P10.2CT Repair - Admin Web Builder Workspace Smoke')
[void]$summary.Add('')
[void]$summary.Add(('Started UTC: `{0}`' -f $startedUtc.ToString('o')))
[void]$summary.Add(('Finished UTC: `{0}`' -f $finishedUtc.ToString('o')))
[void]$summary.Add(('Admin Web base URL: `{0}`' -f $baseUrl))
[void]$summary.Add(('Timeout seconds: `{0}`' -f $TimeoutSec))
[void]$summary.Add(('Routes probed: `{0}`' -f $routes.Count))
[void]$summary.Add('')
[void]$summary.Add(('Details: `{0}`' -f $detailsPath))
[void]$summary.Add('')
[void]$summary.Add('## Results')
[void]$summary.Add('')
foreach ($item in $details) {
    [void]$summary.Add(('- `{0}` => `{1}` `{2}`' -f $item.Route, $item.Result, $item.StatusCode))
}

Set-Content -LiteralPath $summaryPath -Value $summary.ToArray() -Encoding UTF8
Write-Host ('Wrote summary: {0}' -f $summaryPath)
Write-Host ('Wrote details: {0}' -f $detailsPath)

$failed = $false
foreach ($item in $details) {
    if ($item.Result -ne 'Success') {
        $failed = $true
    }
}

if ($failed) {
    throw ('One or more builder workspace routes failed. Review {0}.' -f $detailsPath)
}

Write-Host 'P10.2CT Repair Admin Web builder workspace smoke passed.'
