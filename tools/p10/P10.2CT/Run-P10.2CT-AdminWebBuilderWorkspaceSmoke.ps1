Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

param(
    [string]$AdminWebBaseUrl = 'http://localhost:5173',
    [int]$TimeoutSeconds = 5
)

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$artifactRoot = Join-Path $repoRoot 'artifacts\p10\P10.2CT'
New-Item -ItemType Directory -Force -Path $artifactRoot | Out-Null

$summaryPath = Join-Path $artifactRoot 'builder-workspace-smoke.summary.md'
$detailsPath = Join-Path $artifactRoot 'builder-workspace-smoke.details.csv'

$routes = @(
    [pscustomobject]@{ Name = 'Manifest Builder'; Path = '/manifest-builder' },
    [pscustomobject]@{ Name = 'Taxonomy Builder'; Path = '/taxonomy-builder' },
    [pscustomobject]@{ Name = 'Mapping Builder'; Path = '/mapping-builder' }
)

$rows = New-Object 'System.Collections.Generic.List[object]'
$summary = New-Object 'System.Collections.Generic.List[string]'
[void]$summary.Add('# P10.2CT - Admin Web Builder Workspace Smoke')
[void]$summary.Add('')
[void]$summary.Add(('Started UTC: `{0}`' -f ([DateTime]::UtcNow.ToString('o'))))
[void]$summary.Add(('Admin Web base URL: `{0}`' -f $AdminWebBaseUrl))
[void]$summary.Add(('Timeout seconds: `{0}`' -f $TimeoutSeconds))
[void]$summary.Add('')

foreach ($route in $routes) {
    $url = $AdminWebBaseUrl.TrimEnd('/') + $route.Path
    $statusCode = ''
    $result = 'Unknown'
    $message = ''
    try {
        $response = Invoke-WebRequest -Uri $url -Method Get -TimeoutSec $TimeoutSeconds -UseBasicParsing
        $statusCode = [string]$response.StatusCode
        $content = [string]$response.Content
        if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 400 -and $content.IndexOf('<html', [StringComparison]::OrdinalIgnoreCase) -ge 0) {
            $result = 'Success'
            $message = 'HTML response received.'
        } else {
            $result = 'UnexpectedResponse'
            $message = ('Status {0}; HTML marker not found.' -f $response.StatusCode)
        }
    } catch {
        $result = 'RequestFailed'
        $message = $_.Exception.Message
    }

    [void]$rows.Add([pscustomobject]@{
        Name = $route.Name
        Path = $route.Path
        Url = $url
        StatusCode = $statusCode
        Result = $result
        Message = $message
    })
}

$rows | Export-Csv -Path $detailsPath -NoTypeInformation -Encoding UTF8

$successCount = 0
foreach ($row in $rows) {
    if ($row.Result -eq 'Success') {
        $successCount++
    }
}
[void]$summary.Add(('Routes probed: `{0}`' -f $rows.Count))
[void]$summary.Add(('Successful routes: `{0}`' -f $successCount))
[void]$summary.Add(('Details: `{0}`' -f $detailsPath))
[void]$summary.Add(('Finished UTC: `{0}`' -f ([DateTime]::UtcNow.ToString('o'))))
Set-Content -Path $summaryPath -Value $summary.ToArray() -Encoding UTF8
Write-Host ('Wrote summary: {0}' -f $summaryPath)
Write-Host ('Wrote details: {0}' -f $detailsPath)

if ($successCount -ne $routes.Count) {
    throw ('Builder workspace smoke completed with {0} successful route(s) out of {1}. Review {2}.' -f $successCount, $routes.Count, $detailsPath)
}
