param(
    [string]$AdminWebBaseUrl = 'http://localhost:5173',
    [string]$AdminApiBaseUrl = 'https://localhost:55436',
    [int]$TimeoutSeconds = 5
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$artifactRoot = Join-Path $repoRoot 'artifacts\p10\P10.3E-Repair2'
if (-not (Test-Path $artifactRoot)) {
    New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
}

$summaryPath = Join-Path $artifactRoot 'operator-workflow-acceptance.summary.md'
$detailsPath = Join-Path $artifactRoot 'operator-workflow-acceptance.details.csv'

function Join-Url {
    param(
        [string]$BaseUrl,
        [string]$Path
    )
    $base = $BaseUrl.TrimEnd('/')
    if ($Path.StartsWith('/')) {
        return ($base + $Path)
    }
    return ($base + '/' + $Path)
}

function Invoke-HttpProbe {
    param(
        [string]$Category,
        [string]$Name,
        [string]$Method,
        [string]$Url,
        [int]$Timeout
    )

    $statusCode = ''
    $outcome = 'RequestFailed'
    $message = ''

    if ($Url.StartsWith('https://')) {
        $curl = Get-Command 'curl.exe' -ErrorAction SilentlyContinue
        if ($null -eq $curl) {
            $message = 'curl.exe was not found; cannot probe local HTTPS without PS certificate callback.'
            return [pscustomobject]@{ Category = $Category; Name = $Name; Method = $Method; Url = $Url; StatusCode = $statusCode; Outcome = $outcome; Message = $message }
        }

        $curlOutput = & $curl.Source -k --silent --show-error --output NUL --write-out '%{http_code}' --max-time $Timeout -X $Method $Url 2>&1
        $exitCode = $LASTEXITCODE
        $text = [string]::Join(' ', @($curlOutput))
        $matches = [System.Text.RegularExpressions.Regex]::Matches($text, '\d{3}')
        if ($matches.Count -gt 0) {
            $statusCode = $matches[$matches.Count - 1].Value
        }
        if ($exitCode -ne 0) {
            $outcome = 'RequestFailed'
            $message = ('curl.exe exit code {0}: {1}' -f $exitCode, $text)
        }
        elseif ($statusCode -eq '200' -or $statusCode -eq '204') {
            $outcome = 'Success'
            $message = 'OK'
        }
        elseif ($statusCode -eq '401' -or $statusCode -eq '403') {
            $outcome = 'AuthRequired'
            $message = ('HTTP {0}' -f $statusCode)
        }
        else {
            $outcome = 'NonSuccess'
            $message = ('HTTP {0}' -f $statusCode)
        }

        return [pscustomobject]@{ Category = $Category; Name = $Name; Method = $Method; Url = $Url; StatusCode = $statusCode; Outcome = $outcome; Message = $message }
    }

    try {
        $response = Invoke-WebRequest -Uri $Url -Method $Method -TimeoutSec $Timeout -UseBasicParsing
        $statusCode = [string]$response.StatusCode
        if ($response.StatusCode -eq 200 -or $response.StatusCode -eq 204) {
            $outcome = 'Success'
            $message = 'OK'
        }
        else {
            $outcome = 'NonSuccess'
            $message = ('HTTP {0}' -f $response.StatusCode)
        }
    }
    catch {
        $status = $null
        if ($_.Exception.PSObject.Properties.Name -contains 'Response') {
            if ($null -ne $_.Exception.Response) {
                $status = $_.Exception.Response.StatusCode
            }
        }
        if ($null -ne $status) {
            $code = [int]$status
            $statusCode = [string]$code
            if ($code -eq 401 -or $code -eq 403) {
                $outcome = 'AuthRequired'
                $message = ('HTTP {0}' -f $code)
            }
            else {
                $outcome = 'NonSuccess'
                $message = ('HTTP {0}' -f $code)
            }
        }
        else {
            $outcome = 'RequestFailed'
            $message = $_.Exception.Message
            if ($_.Exception.InnerException) {
                $message = ('{0} Inner: {1}' -f $message, $_.Exception.InnerException.Message)
            }
        }
    }

    return [pscustomobject]@{ Category = $Category; Name = $Name; Method = $Method; Url = $Url; StatusCode = $statusCode; Outcome = $outcome; Message = $message }
}

$results = New-Object 'System.Collections.Generic.List[object]'

$webRoutes = @(
    '/',
    '/connector-configuration',
    '/credential-vault',
    '/execution-sessions',
    '/failure-retry',
    '/manifest-builder',
    '/mapping-builder',
    '/operations/operational-events',
    '/runtime-dashboard',
    '/runtime-dashboard/:runId',
    '/taxonomy-builder'
)

foreach ($route in $webRoutes) {
    $probeRoute = $route
    if ($probeRoute -eq '/runtime-dashboard/:runId') {
        $probeRoute = '/runtime-dashboard/sample-run-id'
    }
    $url = Join-Url -BaseUrl $AdminWebBaseUrl -Path $probeRoute
    $result = Invoke-HttpProbe -Category 'AdminWebRoute' -Name $route -Method 'GET' -Url $url -Timeout $TimeoutSeconds
    [void]$results.Add($result)
    Write-Host ('GET {0} => {1} {2}' -f $route, $result.Outcome, $result.StatusCode)
}

$coreApiPaths = @(
    '/api/connectors',
    '/api/projects',
    '/api/runs',
    '/api/artifacts',
    '/api/credentials',
    '/api/cloud/auth/configuration',
    '/api/cloud/storage/provider'
)

foreach ($path in $coreApiPaths) {
    $url = Join-Url -BaseUrl $AdminApiBaseUrl -Path $path
    $result = Invoke-HttpProbe -Category 'CoreOperatorApi' -Name $path -Method 'GET' -Url $url -Timeout $TimeoutSeconds
    [void]$results.Add($result)
    Write-Host ('GET {0} => {1} {2}' -f $path, $result.Outcome, $result.StatusCode)
}

$results.ToArray() | Export-Csv -Path $detailsPath -NoTypeInformation -Encoding UTF8

$webCount = @($results.ToArray() | Where-Object { $_.Category -eq 'AdminWebRoute' }).Count
$apiCount = @($results.ToArray() | Where-Object { $_.Category -eq 'CoreOperatorApi' }).Count
$successCount = @($results.ToArray() | Where-Object { $_.Outcome -eq 'Success' }).Count
$authCount = @($results.ToArray() | Where-Object { $_.Outcome -eq 'AuthRequired' }).Count
$nonSuccess = @($results.ToArray() | Where-Object { $_.Outcome -ne 'Success' -and $_.Outcome -ne 'AuthRequired' })
$nonSuccessCount = $nonSuccess.Count

$summary = New-Object 'System.Collections.Generic.List[string]'
[void]$summary.Add('# P10.3E Repair2 - Admin Web Operator Workflow Acceptance')
[void]$summary.Add('')
[void]$summary.Add(('Started UTC: `{0}`' -f ([DateTime]::UtcNow.ToString('o'))))
[void]$summary.Add(('Admin Web base URL: `{0}`' -f $AdminWebBaseUrl))
[void]$summary.Add(('Admin API base URL: `{0}`' -f $AdminApiBaseUrl))
[void]$summary.Add(('Timeout seconds: `{0}`' -f $TimeoutSeconds))
[void]$summary.Add('')
[void]$summary.Add(('Admin Web route probes: `{0}`' -f $webCount))
[void]$summary.Add(('Core operator API probes: `{0}`' -f $apiCount))
[void]$summary.Add(('Successful probes: `{0}`' -f $successCount))
[void]$summary.Add(('Auth-required probes: `{0}`' -f $authCount))
[void]$summary.Add(('Non-success probes: `{0}`' -f $nonSuccessCount))
[void]$summary.Add('')
[void]$summary.Add(('Details: `{0}`' -f $detailsPath))
Set-Content -Path $summaryPath -Value $summary.ToArray() -Encoding UTF8

Write-Host ('Wrote summary: {0}' -f $summaryPath)
Write-Host ('Wrote details: {0}' -f $detailsPath)

if ($nonSuccessCount -gt 0) {
    throw ('Operator workflow acceptance completed with {0} non-success probe(s). Review {1}' -f $nonSuccessCount, $summaryPath)
}

Write-Host 'Operator workflow acceptance passed.'
