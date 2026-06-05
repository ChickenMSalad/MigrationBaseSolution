param(
    [string]$AdminWebBaseUrl = 'http://localhost:5173',
    [string]$AdminApiBaseUrl = 'https://localhost:55436',
    [int]$TimeoutSeconds = 5
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$artifactRoot = Join-Path $repoRoot 'artifacts\p10\P10.3A'

if (-not (Test-Path -LiteralPath $artifactRoot)) {
    New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
}

$startedUtc = [DateTime]::UtcNow.ToString('o')
$summaryPath = Join-Path $artifactRoot 'runtime-acceptance.summary.md'
$detailsPath = Join-Path $artifactRoot 'runtime-acceptance.details.csv'

$summary = New-Object 'System.Collections.Generic.List[string]'
[void]$summary.Add('# P10.3A - Admin Web Runtime Acceptance')
[void]$summary.Add('')
[void]$summary.Add(('Started UTC: `{0}`' -f $startedUtc))
[void]$summary.Add(('Admin Web base URL: `{0}`' -f $AdminWebBaseUrl))
[void]$summary.Add(('Admin API base URL: `{0}`' -f $AdminApiBaseUrl))
[void]$summary.Add(('Timeout seconds: `{0}`' -f $TimeoutSeconds))
[void]$summary.Add('')
Set-Content -LiteralPath $summaryPath -Value $summary.ToArray() -Encoding UTF8

$rows = New-Object 'System.Collections.Generic.List[object]'

function Invoke-AcceptanceProbe {
    param(
        [string]$Name,
        [string]$Url
    )

    $status = 'Unknown'
    $statusCode = ''
    $message = ''

    try {
        $response = Invoke-WebRequest -Uri $Url -Method Get -TimeoutSec $TimeoutSeconds -UseBasicParsing
        $status = 'Success'
        $statusCode = [string][int]$response.StatusCode
        $message = $response.StatusDescription
    }
    catch {
        $status = 'RequestFailed'
        $message = $_.Exception.Message
        if ($null -ne $_.Exception.Response) {
            try {
                $statusCode = [string][int]$_.Exception.Response.StatusCode
                if ($statusCode -eq '404') { $status = 'NotFound' }
                elseif ($statusCode -eq '401') { $status = 'Unauthorized' }
                elseif ($statusCode -eq '403') { $status = 'Forbidden' }
                elseif ($statusCode -eq '405') { $status = 'MethodNotAllowed' }
            }
            catch {
                $statusCode = ''
            }
        }
    }

    $row = [PSCustomObject]@{
        Name = $Name
        Url = $Url
        Status = $status
        StatusCode = $statusCode
        Message = $message
    }
    [void]$rows.Add($row)
    Write-Host ('{0} => {1} {2}' -f $Name, $status, $statusCode)
}

$adminWeb = $AdminWebBaseUrl.TrimEnd('/')
$adminApi = $AdminApiBaseUrl.TrimEnd('/')

Invoke-AcceptanceProbe -Name 'Admin Web root' -Url $adminWeb
Invoke-AcceptanceProbe -Name 'Admin Web projects route' -Url ($adminWeb + '/projects')
Invoke-AcceptanceProbe -Name 'Admin Web runs route' -Url ($adminWeb + '/runs')
Invoke-AcceptanceProbe -Name 'Admin Web artifacts route' -Url ($adminWeb + '/artifacts')
Invoke-AcceptanceProbe -Name 'Admin API projects' -Url ($adminApi + '/api/projects')
Invoke-AcceptanceProbe -Name 'Admin API runs' -Url ($adminApi + '/api/runs')
Invoke-AcceptanceProbe -Name 'Admin API artifacts' -Url ($adminApi + '/api/artifacts')
Invoke-AcceptanceProbe -Name 'Admin API connectors' -Url ($adminApi + '/api/connectors')

$rows | Export-Csv -LiteralPath $detailsPath -NoTypeInformation -Encoding UTF8

$successCount = 0
$failureCount = 0
foreach ($row in $rows) {
    if ($row.Status -eq 'Success') { $successCount++ } else { $failureCount++ }
}

$finishedUtc = [DateTime]::UtcNow.ToString('o')
[void]$summary.Add(('Finished UTC: `{0}`' -f $finishedUtc))
[void]$summary.Add(('Total probes: `{0}`' -f $rows.Count))
[void]$summary.Add(('Successful probes: `{0}`' -f $successCount))
[void]$summary.Add(('Non-success probes: `{0}`' -f $failureCount))
[void]$summary.Add('')
[void]$summary.Add(('Details: `{0}`' -f $detailsPath))
Set-Content -LiteralPath $summaryPath -Value $summary.ToArray() -Encoding UTF8

Write-Host ('Wrote summary: {0}' -f $summaryPath)
Write-Host ('Wrote details: {0}' -f $detailsPath)

if ($failureCount -gt 0) {
    throw ('Runtime acceptance completed with {0} non-success probe(s). Review {1}' -f $failureCount, $summaryPath)
}

Write-Host 'Runtime acceptance completed successfully.'
