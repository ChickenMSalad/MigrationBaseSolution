param(
    [string]$AdminWebBaseUrl = 'http://localhost:5173',
    [string]$AdminApiBaseUrl = 'https://localhost:55436',
    [int]$TimeoutSeconds = 5,
    [int]$MaxRoutes = 50
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($AdminWebBaseUrl)) {
    throw 'AdminWebBaseUrl is required.'
}

if ([string]::IsNullOrWhiteSpace($AdminApiBaseUrl)) {
    throw 'AdminApiBaseUrl is required.'
}

if ($TimeoutSeconds -lt 1) {
    throw 'TimeoutSeconds must be greater than zero.'
}

if ($MaxRoutes -lt 1) {
    throw 'MaxRoutes must be greater than zero.'
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..\..')
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$appPath = Join-Path $adminWebRoot 'src\App.tsx'
$artifactRoot = Join-Path $repoRoot 'artifacts\p10\P10.3E'
$summaryPath = Join-Path $artifactRoot 'operator-workflow-acceptance.summary.md'
$detailsPath = Join-Path $artifactRoot 'operator-workflow-acceptance.details.csv'

if (-not (Test-Path -LiteralPath $adminWebRoot -PathType Container)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}

if (-not (Test-Path -LiteralPath $artifactRoot -PathType Container)) {
    New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
}

$startedUtc = [DateTime]::UtcNow
$initial = New-Object 'System.Collections.Generic.List[string]'
[void]$initial.Add('# P10.3E - Admin Web Operator Workflow Acceptance')
[void]$initial.Add('')
[void]$initial.Add(('Started UTC: `{0}`' -f $startedUtc.ToString('o')))
[void]$initial.Add(('Admin Web base URL: `{0}`' -f $AdminWebBaseUrl))
[void]$initial.Add(('Admin API base URL: `{0}`' -f $AdminApiBaseUrl))
[void]$initial.Add(('Timeout seconds: `{0}`' -f $TimeoutSeconds))
Set-Content -LiteralPath $summaryPath -Value $initial.ToArray() -Encoding UTF8

try {
    [System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
} catch {
    Write-Host ('Unable to install local certificate bypass callback: {0}' -f $_.Exception.Message)
}

function Join-Url {
    param(
        [string]$BaseUrl,
        [string]$Path
    )

    $baseValue = $BaseUrl.TrimEnd('/')
    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $baseValue
    }

    if ($Path.StartsWith('/')) {
        return ($baseValue + $Path)
    }

    return ($baseValue + '/' + $Path)
}

function Invoke-OperatorProbe {
    param(
        [string]$Category,
        [string]$Name,
        [string]$Method,
        [string]$Url,
        [int]$TimeoutSeconds
    )

    $statusCode = $null
    $outcome = 'Unknown'
    $message = ''

    try {
        $response = Invoke-WebRequest -Uri $Url -Method $Method -UseBasicParsing -TimeoutSec $TimeoutSeconds
        $statusCode = [int]$response.StatusCode
        if ($statusCode -ge 200 -and $statusCode -lt 400) {
            $outcome = 'Success'
        } else {
            $outcome = 'NonSuccess'
        }
        $message = $response.StatusDescription
    } catch {
        $outcome = 'RequestFailed'
        $message = $_.Exception.Message
        if ($_.Exception.Response -ne $null) {
            try {
                $statusCode = [int]$_.Exception.Response.StatusCode
                if ($statusCode -eq 401 -or $statusCode -eq 403) {
                    $outcome = 'AuthRequired'
                } elseif ($statusCode -eq 404) {
                    $outcome = 'NotFound'
                } elseif ($statusCode -eq 405) {
                    $outcome = 'MethodNotAllowed'
                } else {
                    $outcome = 'HttpFailure'
                }
            } catch {
                $statusCode = $null
            }
        }
    }

    return [PSCustomObject]@{
        Category = $Category
        Name = $Name
        Method = $Method
        Url = $Url
        StatusCode = $statusCode
        Outcome = $outcome
        Message = $message
    }
}

$routeSet = New-Object 'System.Collections.Generic.HashSet[string]'
[void]$routeSet.Add('/')

if (Test-Path -LiteralPath $appPath -PathType Leaf) {
    $appLines = Get-Content -LiteralPath $appPath
    foreach ($line in $appLines) {
        if ($null -eq $line) { continue }
        $marker = 'path="'
        $start = $line.IndexOf($marker)
        if ($start -lt 0) { continue }
        $valueStart = $start + $marker.Length
        $valueEnd = $line.IndexOf('"', $valueStart)
        if ($valueEnd -le $valueStart) { continue }
        $pathValue = $line.Substring($valueStart, $valueEnd - $valueStart)
        if ([string]::IsNullOrWhiteSpace($pathValue)) { continue }
        if ($pathValue -eq '*') { continue }
        if (-not $pathValue.StartsWith('/')) { continue }
        [void]$routeSet.Add($pathValue)
    }
}

$routeList = New-Object 'System.Collections.Generic.List[string]'
foreach ($route in $routeSet) {
    [void]$routeList.Add($route)
}
$routeArray = $routeList.ToArray() | Sort-Object

$coreApiPaths = @(
    '/api/connectors',
    '/api/projects',
    '/api/runs',
    '/api/artifacts',
    '/api/credentials',
    '/api/cloud/auth/configuration',
    '/api/cloud/storage/provider'
)

$details = New-Object 'System.Collections.Generic.List[object]'
$routeCount = 0
foreach ($route in $routeArray) {
    if ($routeCount -ge $MaxRoutes) { break }
    $routeCount++
    $url = Join-Url -BaseUrl $AdminWebBaseUrl -Path $route
    $probe = Invoke-OperatorProbe -Category 'AdminWebRoute' -Name $route -Method 'GET' -Url $url -TimeoutSeconds $TimeoutSeconds
    [void]$details.Add($probe)
    Write-Host ('{0} {1} => {2} {3}' -f $probe.Method, $probe.Name, $probe.Outcome, $probe.StatusCode)
}

foreach ($apiPath in $coreApiPaths) {
    $url = Join-Url -BaseUrl $AdminApiBaseUrl -Path $apiPath
    $probe = Invoke-OperatorProbe -Category 'CoreOperatorApi' -Name $apiPath -Method 'GET' -Url $url -TimeoutSeconds $TimeoutSeconds
    [void]$details.Add($probe)
    Write-Host ('{0} {1} => {2} {3}' -f $probe.Method, $probe.Name, $probe.Outcome, $probe.StatusCode)
}

$detailsArray = $details.ToArray()
$detailsArray | Export-Csv -LiteralPath $detailsPath -NoTypeInformation -Encoding UTF8

$successCount = @($detailsArray | Where-Object { $_.Outcome -eq 'Success' }).Count
$authRequiredCount = @($detailsArray | Where-Object { $_.Outcome -eq 'AuthRequired' }).Count
$nonSuccessCount = @($detailsArray | Where-Object { $_.Outcome -ne 'Success' -and $_.Outcome -ne 'AuthRequired' }).Count
$routeProbeCount = @($detailsArray | Where-Object { $_.Category -eq 'AdminWebRoute' }).Count
$apiProbeCount = @($detailsArray | Where-Object { $_.Category -eq 'CoreOperatorApi' }).Count
$finishedUtc = [DateTime]::UtcNow

$summary = New-Object 'System.Collections.Generic.List[string]'
[void]$summary.Add('# P10.3E - Admin Web Operator Workflow Acceptance')
[void]$summary.Add('')
[void]$summary.Add(('Started UTC: `{0}`' -f $startedUtc.ToString('o')))
[void]$summary.Add(('Finished UTC: `{0}`' -f $finishedUtc.ToString('o')))
[void]$summary.Add(('Admin Web base URL: `{0}`' -f $AdminWebBaseUrl))
[void]$summary.Add(('Admin API base URL: `{0}`' -f $AdminApiBaseUrl))
[void]$summary.Add(('Timeout seconds: `{0}`' -f $TimeoutSeconds))
[void]$summary.Add('')
[void]$summary.Add(('Admin Web route probes: `{0}`' -f $routeProbeCount))
[void]$summary.Add(('Core operator API probes: `{0}`' -f $apiProbeCount))
[void]$summary.Add(('Successful probes: `{0}`' -f $successCount))
[void]$summary.Add(('Auth-required probes: `{0}`' -f $authRequiredCount))
[void]$summary.Add(('Non-success probes: `{0}`' -f $nonSuccessCount))
[void]$summary.Add('')
[void]$summary.Add(('Details: `{0}`' -f $detailsPath))
Set-Content -LiteralPath $summaryPath -Value $summary.ToArray() -Encoding UTF8

Write-Host ('Wrote summary: {0}' -f $summaryPath)
Write-Host ('Wrote details: {0}' -f $detailsPath)

if ($nonSuccessCount -gt 0) {
    throw ('Operator workflow acceptance completed with {0} non-success probe(s). Review {1}' -f $nonSuccessCount, $summaryPath)
}

Write-Host 'P10.3E Admin Web operator workflow acceptance passed.'
