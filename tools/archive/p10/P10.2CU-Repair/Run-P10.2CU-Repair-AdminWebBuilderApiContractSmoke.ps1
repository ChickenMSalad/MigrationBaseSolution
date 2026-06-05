param(
    [string]$AdminApiBaseUrl = 'https://localhost:55436',
    [int]$TimeoutSeconds = 5,
    [int]$MaxEndpoints = 25
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..')).Path
$artifactRoot = Join-Path $repoRoot 'artifacts\p10\P10.2CU-Repair'
New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null

$summaryPath = Join-Path $artifactRoot 'builder-api-contract-smoke.summary.md'
$detailsPath = Join-Path $artifactRoot 'builder-api-contract-smoke.details.csv'

$summary = New-Object 'System.Collections.Generic.List[string]'
[void]$summary.Add('# P10.2CU Repair - Admin Web Builder API Contract Smoke')
[void]$summary.Add('')
[void]$summary.Add(('Started UTC: `{0}`' -f ([DateTime]::UtcNow.ToString('o'))))
[void]$summary.Add(('Admin API base URL: `{0}`' -f $AdminApiBaseUrl))
[void]$summary.Add(('Timeout seconds: `{0}`' -f $TimeoutSeconds))
[void]$summary.Add(('Max endpoints: `{0}`' -f $MaxEndpoints))
[void]$summary.Add('')
Set-Content -Path $summaryPath -Value $summary.ToArray() -Encoding UTF8

$baseUrl = $AdminApiBaseUrl.TrimEnd('/')
$endpointCandidates = @(
    '/api/manifest-builder/build',
    '/api/manifest-builder/validate',
    '/api/manifest-builder/preview',
    '/api/taxonomy-builder',
    '/api/taxonomy-builder/preview',
    '/api/taxonomy-builder/validate',
    '/api/mapping-builder',
    '/api/mapping-builder/preview',
    '/api/mapping-builder/validate',
    '/api/mapping-profiles',
    '/api/artifacts',
    '/api/projects'
)

$limit = $MaxEndpoints
if ($limit -lt 1) {
    $limit = 1
}

$details = New-Object 'System.Collections.Generic.List[object]'
$index = 0
foreach ($endpoint in $endpointCandidates) {
    if ($index -ge $limit) {
        break
    }

    $index = $index + 1
    $url = ('{0}{1}' -f $baseUrl, $endpoint)
    $statusCode = ''
    $status = 'Unknown'
    $message = ''

    try {
        $response = Invoke-WebRequest -Uri $url -Method Get -TimeoutSec $TimeoutSeconds -UseBasicParsing
        $statusCode = [string]$response.StatusCode
        if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 500) {
            $status = 'Reachable'
        }
        else {
            $status = 'UnexpectedStatus'
        }
        $message = $response.StatusDescription
    }
    catch [System.Net.WebException] {
        $webResponse = $_.Exception.Response
        if ($null -ne $webResponse) {
            $statusCode = [string][int]$webResponse.StatusCode
            $message = $webResponse.StatusDescription
            if ($statusCode -eq '401' -or $statusCode -eq '403') {
                $status = 'AuthRequired'
            }
            elseif ($statusCode -eq '404') {
                $status = 'NotFound'
            }
            elseif ($statusCode -eq '405') {
                $status = 'MethodNotAllowed'
            }
            elseif ([int]$webResponse.StatusCode -ge 200 -and [int]$webResponse.StatusCode -lt 500) {
                $status = 'Reachable'
            }
            else {
                $status = 'RequestFailed'
            }
        }
        else {
            $status = 'RequestFailed'
            $message = $_.Exception.Message
        }
    }
    catch {
        $status = 'RequestFailed'
        $message = $_.Exception.Message
    }

    $details.Add([pscustomobject]@{
        Endpoint = $endpoint
        Url = $url
        Status = $status
        StatusCode = $statusCode
        Message = $message
    }) | Out-Null
}

$details | Export-Csv -Path $detailsPath -NoTypeInformation -Encoding UTF8

$finished = New-Object 'System.Collections.Generic.List[string]'
[void]$finished.Add('# P10.2CU Repair - Admin Web Builder API Contract Smoke')
[void]$finished.Add('')
[void]$finished.Add(('Started UTC: see initial report timestamp'))
[void]$finished.Add(('Finished UTC: `{0}`' -f ([DateTime]::UtcNow.ToString('o'))))
[void]$finished.Add(('Admin API base URL: `{0}`' -f $AdminApiBaseUrl))
[void]$finished.Add(('Endpoint candidates: `{0}`' -f $endpointCandidates.Count))
[void]$finished.Add(('Probed endpoints: `{0}`' -f $details.Count))
[void]$finished.Add('')
[void]$finished.Add(('Details: `{0}`' -f $detailsPath))
Set-Content -Path $summaryPath -Value $finished.ToArray() -Encoding UTF8

Write-Host ('Wrote summary: {0}' -f $summaryPath)
Write-Host ('Wrote details: {0}' -f $detailsPath)
