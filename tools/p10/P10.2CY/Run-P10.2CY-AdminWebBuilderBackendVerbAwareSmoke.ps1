param(
    [string]$AdminApiBaseUrl = 'https://localhost:55436',
    [int]$TimeoutSeconds = 5
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$repoRootPath = $repoRoot.Path

$artifactRoot = Join-Path $repoRootPath 'artifacts\p10\P10.2CY'
if (-not (Test-Path -LiteralPath $artifactRoot)) {
    New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
}

$summaryPath = Join-Path $artifactRoot 'builder-backend-verb-aware-smoke.summary.md'
$detailsPath = Join-Path $artifactRoot 'builder-backend-verb-aware-smoke.details.csv'

$summary = New-Object 'System.Collections.Generic.List[string]'
$details = New-Object 'System.Collections.Generic.List[string]'

$startedUtc = [DateTime]::UtcNow.ToString('o')
[void]$summary.Add('# P10.2CY - Admin Web Builder Backend Verb-Aware Smoke')
[void]$summary.Add('')
[void]$summary.Add(('Started UTC: `{0}`' -f $startedUtc))
[void]$summary.Add(('Admin API base URL: `{0}`' -f $AdminApiBaseUrl))
[void]$summary.Add(('Timeout seconds: `{0}`' -f $TimeoutSeconds))
[void]$summary.Add('')

[void]$details.Add('Area,Method,Path,Url,StatusCode,Result,Message')

$baseUrl = $AdminApiBaseUrl.TrimEnd('/')
$probes = @(
    @{ Area = 'Manifest Builder'; Method = 'POST'; Path = '/api/manifest-builder/build'; Body = '{}' },
    @{ Area = 'Manifest Builder'; Method = 'POST'; Path = '/api/manifest-builder/validate'; Body = '{}' },
    @{ Area = 'Manifest Builder'; Method = 'POST'; Path = '/api/manifest-builder/preview'; Body = '{}' },
    @{ Area = 'Taxonomy Builder'; Method = 'GET'; Path = '/api/taxonomy-builder'; Body = $null },
    @{ Area = 'Taxonomy Builder'; Method = 'POST'; Path = '/api/taxonomy-builder/validate'; Body = '{}' },
    @{ Area = 'Taxonomy Builder'; Method = 'POST'; Path = '/api/taxonomy-builder/preview'; Body = '{}' },
    @{ Area = 'Mapping Builder'; Method = 'GET'; Path = '/api/mapping-builder'; Body = $null },
    @{ Area = 'Mapping Builder'; Method = 'GET'; Path = '/api/mapping-profiles'; Body = $null },
    @{ Area = 'Mapping Builder'; Method = 'POST'; Path = '/api/mapping-builder/validate'; Body = '{}' },
    @{ Area = 'Mapping Builder'; Method = 'POST'; Path = '/api/mapping-builder/preview'; Body = '{}' }
)

$successCount = 0
$notFoundCount = 0
$methodCount = 0
$failureCount = 0

foreach ($probe in $probes) {
    $area = [string]$probe.Area
    $method = [string]$probe.Method
    $path = [string]$probe.Path
    $body = $probe.Body
    $url = ('{0}{1}' -f $baseUrl, $path)
    $statusCode = ''
    $result = 'Unknown'
    $message = ''

    try {
        $headers = @{ Accept = 'application/json' }
        if ($method -eq 'POST') {
            $response = Invoke-WebRequest -Uri $url -Method Post -Headers $headers -Body $body -ContentType 'application/json' -TimeoutSec $TimeoutSeconds -UseBasicParsing
        }
        else {
            $response = Invoke-WebRequest -Uri $url -Method Get -Headers $headers -TimeoutSec $TimeoutSeconds -UseBasicParsing
        }

        $statusCode = [string]$response.StatusCode
        if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 300) {
            $result = 'Success'
            $successCount++
        }
        else {
            $result = 'HttpStatus'
            $failureCount++
        }
    }
    catch {
        $exception = $_.Exception
        $message = $exception.Message
        if ($null -ne $exception.Response) {
            try {
                $statusCode = [string][int]$exception.Response.StatusCode
            }
            catch {
                $statusCode = ''
            }
        }

        if ($statusCode -eq '404') {
            $result = 'NotFound'
            $notFoundCount++
        }
        elseif ($statusCode -eq '405') {
            $result = 'MethodNotAllowed'
            $methodCount++
        }
        else {
            $result = 'RequestFailed'
            $failureCount++
        }
    }

    $safeMessage = $message.Replace('"', '""').Replace("`r", ' ').Replace("`n", ' ')
    [void]$details.Add(('"{0}","{1}","{2}","{3}","{4}","{5}","{6}"' -f $area, $method, $path, $url, $statusCode, $result, $safeMessage))
    Write-Host ('{0} {1} => {2} {3}' -f $method, $path, $result, $statusCode)
}

$finishedUtc = [DateTime]::UtcNow.ToString('o')
[void]$summary.Add(('Finished UTC: `{0}`' -f $finishedUtc))
[void]$summary.Add(('Probed endpoints: `{0}`' -f $probes.Count))
[void]$summary.Add(('Success: `{0}`' -f $successCount))
[void]$summary.Add(('Not Found: `{0}`' -f $notFoundCount))
[void]$summary.Add(('Method Not Allowed: `{0}`' -f $methodCount))
[void]$summary.Add(('Other failures: `{0}`' -f $failureCount))
[void]$summary.Add('')
[void]$summary.Add(('Details: `{0}`' -f $detailsPath))

Set-Content -LiteralPath $summaryPath -Value $summary.ToArray() -Encoding UTF8
Set-Content -LiteralPath $detailsPath -Value $details.ToArray() -Encoding UTF8

Write-Host ('Wrote summary: {0}' -f $summaryPath)
Write-Host ('Wrote details: {0}' -f $detailsPath)

if ($failureCount -gt 0 -or $notFoundCount -gt 0 -or $methodCount -gt 0) {
    throw ('Builder backend verb-aware smoke completed with non-success results. Review {0}' -f $summaryPath)
}
