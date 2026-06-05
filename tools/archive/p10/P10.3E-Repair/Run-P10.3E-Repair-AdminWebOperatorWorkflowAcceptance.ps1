param(
    [string]$AdminWebBaseUrl = 'http://localhost:5173',
    [string]$AdminApiBaseUrl = 'https://localhost:55436',
    [int]$TimeoutSeconds = 5
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..\..')
$artifactRoot = Join-Path $repoRoot 'artifacts\p10\P10.3E-Repair'

if (-not (Test-Path -LiteralPath $artifactRoot)) {
    New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
}

$summaryPath = Join-Path $artifactRoot 'operator-workflow-acceptance.summary.md'
$detailsPath = Join-Path $artifactRoot 'operator-workflow-acceptance.details.csv'

$startedUtc = [DateTime]::UtcNow.ToString('o')

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
[Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }

function Join-Url {
    param(
        [string]$BaseUrl,
        [string]$Path
    )

    $baseValue = $BaseUrl.TrimEnd('/')
    $pathValue = $Path
    if (-not $pathValue.StartsWith('/')) {
        $pathValue = '/' + $pathValue
    }

    return ($baseValue + $pathValue)
}

function Invoke-Probe {
    param(
        [string]$Category,
        [string]$Name,
        [string]$Method,
        [string]$Url
    )

    $row = [ordered]@{
        Category = $Category
        Name = $Name
        Method = $Method
        Url = $Url
        StatusCode = ''
        Outcome = ''
        Message = ''
    }

    try {
        $response = Invoke-WebRequest -Uri $Url -Method $Method -TimeoutSec $TimeoutSeconds -UseBasicParsing
        $row.StatusCode = [string][int]$response.StatusCode
        if ([int]$response.StatusCode -ge 200 -and [int]$response.StatusCode -lt 300) {
            $row.Outcome = 'Success'
            $row.Message = 'OK'
        }
        elseif ([int]$response.StatusCode -eq 401 -or [int]$response.StatusCode -eq 403) {
            $row.Outcome = 'AuthRequired'
            $row.Message = ('HTTP {0}' -f [int]$response.StatusCode)
        }
        else {
            $row.Outcome = 'NonSuccess'
            $row.Message = ('HTTP {0}' -f [int]$response.StatusCode)
        }
    }
    catch [System.Net.WebException] {
        $webResponse = $_.Exception.Response
        if ($null -ne $webResponse) {
            $statusCode = [int]$webResponse.StatusCode
            $row.StatusCode = [string]$statusCode
            if ($statusCode -eq 401 -or $statusCode -eq 403) {
                $row.Outcome = 'AuthRequired'
                $row.Message = ('HTTP {0}' -f $statusCode)
            }
            else {
                $row.Outcome = 'NonSuccess'
                $row.Message = ('HTTP {0}' -f $statusCode)
            }
        }
        else {
            $row.Outcome = 'RequestFailed'
            $message = $_.Exception.Message
            if ($_.Exception.InnerException -and $_.Exception.InnerException.Message) {
                $message = ('{0} Inner: {1}' -f $message, $_.Exception.InnerException.Message)
            }
            $row.Message = $message
        }
    }
    catch {
        $row.Outcome = 'RequestFailed'
        $message = $_.Exception.Message
        if ($_.Exception.InnerException -and $_.Exception.InnerException.Message) {
            $message = ('{0} Inner: {1}' -f $message, $_.Exception.InnerException.Message)
        }
        $row.Message = $message
    }

    return New-Object PSObject -Property $row
}

$rows = New-Object 'System.Collections.Generic.List[object]'

$webRoutes = New-Object 'System.Collections.Generic.List[string]'
[void]$webRoutes.Add('/')
[void]$webRoutes.Add('/connector-configuration')
[void]$webRoutes.Add('/credential-vault')
[void]$webRoutes.Add('/execution-sessions')
[void]$webRoutes.Add('/failure-retry')
[void]$webRoutes.Add('/manifest-builder')
[void]$webRoutes.Add('/mapping-builder')
[void]$webRoutes.Add('/operations/operational-events')
[void]$webRoutes.Add('/runtime-dashboard')
[void]$webRoutes.Add('/runtime-dashboard/:runId')
[void]$webRoutes.Add('/taxonomy-builder')

foreach ($route in $webRoutes) {
    $url = Join-Url -BaseUrl $AdminWebBaseUrl -Path $route
    [void]$rows.Add((Invoke-Probe -Category 'AdminWebRoute' -Name $route -Method 'GET' -Url $url))
}

$coreApiPaths = New-Object 'System.Collections.Generic.List[string]'
[void]$coreApiPaths.Add('/api/connectors')
[void]$coreApiPaths.Add('/api/projects')
[void]$coreApiPaths.Add('/api/runs')
[void]$coreApiPaths.Add('/api/artifacts')
[void]$coreApiPaths.Add('/api/credentials')
[void]$coreApiPaths.Add('/api/cloud/auth/configuration')
[void]$coreApiPaths.Add('/api/cloud/storage/provider')

foreach ($apiPath in $coreApiPaths) {
    $url = Join-Url -BaseUrl $AdminApiBaseUrl -Path $apiPath
    [void]$rows.Add((Invoke-Probe -Category 'CoreOperatorApi' -Name $apiPath -Method 'GET' -Url $url))
}

$rows.ToArray() | Export-Csv -LiteralPath $detailsPath -NoTypeInformation -Encoding UTF8

$total = $rows.Count
$success = 0
$authRequired = 0
$nonSuccess = 0
foreach ($row in $rows) {
    if ($row.Outcome -eq 'Success') {
        $success++
    }
    elseif ($row.Outcome -eq 'AuthRequired') {
        $authRequired++
    }
    else {
        $nonSuccess++
    }
}

$routeCount = 0
$apiCount = 0
foreach ($row in $rows) {
    if ($row.Category -eq 'AdminWebRoute') {
        $routeCount++
    }
    elseif ($row.Category -eq 'CoreOperatorApi') {
        $apiCount++
    }
}

$summary = New-Object 'System.Collections.Generic.List[string]'
[void]$summary.Add('# P10.3E Repair - Admin Web Operator Workflow Acceptance')
[void]$summary.Add('')
[void]$summary.Add(('Started UTC: `{0}`' -f $startedUtc))
[void]$summary.Add(('Finished UTC: `{0}`' -f [DateTime]::UtcNow.ToString('o')))
[void]$summary.Add(('Admin Web base URL: `{0}`' -f $AdminWebBaseUrl))
[void]$summary.Add(('Admin API base URL: `{0}`' -f $AdminApiBaseUrl))
[void]$summary.Add(('Timeout seconds: `{0}`' -f $TimeoutSeconds))
[void]$summary.Add('')
[void]$summary.Add(('Admin Web route probes: `{0}`' -f $routeCount))
[void]$summary.Add(('Core operator API probes: `{0}`' -f $apiCount))
[void]$summary.Add(('Successful probes: `{0}`' -f $success))
[void]$summary.Add(('Auth-required probes: `{0}`' -f $authRequired))
[void]$summary.Add(('Non-success probes: `{0}`' -f $nonSuccess))
[void]$summary.Add('')
[void]$summary.Add(('Details: `{0}`' -f $detailsPath))

Set-Content -LiteralPath $summaryPath -Value $summary.ToArray() -Encoding UTF8

Write-Host ('Wrote summary: {0}' -f $summaryPath)
Write-Host ('Wrote details: {0}' -f $detailsPath)

if ($nonSuccess -gt 0) {
    throw ('Operator workflow acceptance completed with {0} non-success probe(s). Review {1}' -f $nonSuccess, $summaryPath)
}

Write-Host 'Operator workflow acceptance completed successfully.'
