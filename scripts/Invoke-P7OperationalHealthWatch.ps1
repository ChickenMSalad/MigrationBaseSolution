[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$AdminApiBaseUrl,

    [int]$IntervalSeconds = 10,

    [int]$Samples = 12,

    [string]$OutputDirectory = '',

    [int]$StaleWorkerSeconds = 120
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($IntervalSeconds -lt 1) {
    throw 'IntervalSeconds must be at least 1.'
}

if ($Samples -lt 1) {
    throw 'Samples must be at least 1.'
}

$baseUrl = $AdminApiBaseUrl.TrimEnd('/')

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path (Get-Location) 'artifacts\p7-validation'
}

if (-not (Test-Path -LiteralPath $OutputDirectory)) {
    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
}

$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$reportPath = Join-Path $OutputDirectory ('p7-operational-health-watch-' + $timestamp + '.csv')
$detailPath = Join-Path $OutputDirectory ('p7-operational-health-watch-details-' + $timestamp + '.jsonl')

$rows = New-Object System.Collections.Generic.List[object]

function Get-PropertyValue {
    param(
        [object]$Object,
        [string]$Name,
        [object]$DefaultValue
    )

    if ($null -eq $Object) {
        return $DefaultValue
    }

    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) {
        return $DefaultValue
    }

    if ($null -eq $property.Value) {
        return $DefaultValue
    }

    return $property.Value
}

function Get-JsonEndpoint {
    param([string]$Path)

    $url = $baseUrl + $Path
    try {
        $response = Invoke-WebRequest -Uri $url -UseBasicParsing -Headers @{ Accept = 'application/json' }
        $content = [string]$response.Content
        $json = $null
        if (-not [string]::IsNullOrWhiteSpace($content)) {
            $json = $content | ConvertFrom-Json
        }

        return [pscustomobject]@{
            Url = $url
            Success = $true
            StatusCode = [int]$response.StatusCode
            Content = $content
            Json = $json
            Error = ''
        }
    }
    catch {
        return [pscustomobject]@{
            Url = $url
            Success = $false
            StatusCode = 0
            Content = ''
            Json = $null
            Error = $_.Exception.Message
        }
    }
}

function Get-ArrayCount {
    param([object]$Value)

    if ($null -eq $Value) {
        return 0
    }

    if ($Value -is [System.Array]) {
        return $Value.Length
    }

    return 1
}

function Join-Warnings {
    param([System.Collections.Generic.List[string]]$Warnings)

    if ($null -eq $Warnings -or $Warnings.Count -eq 0) {
        return ''
    }

    return [string]::Join('; ', $Warnings.ToArray())
}

for ($sample = 1; $sample -le $Samples; $sample++) {
    $now = [DateTimeOffset]::UtcNow

    $summaryResult = Get-JsonEndpoint -Path '/api/runtime/dashboard/summary'
    $runsResult = Get-JsonEndpoint -Path '/api/runtime/dashboard/runs'
    $failuresResult = Get-JsonEndpoint -Path '/api/runtime/dashboard/failures'
    $workersResult = Get-JsonEndpoint -Path '/api/operational/workers/telemetry'
    $eventsResult = Get-JsonEndpoint -Path '/api/operational/events/recent'

    $summary = $summaryResult.Json
    $runs = $runsResult.Json
    $failures = $failuresResult.Json
    $workers = $workersResult.Json
    $events = $eventsResult.Json

    $runCount = Get-PropertyValue -Object $summary -Name 'runCount' -DefaultValue 0
    $queued = Get-PropertyValue -Object $summary -Name 'queuedWorkItemCount' -DefaultValue 0
    $dispatched = Get-PropertyValue -Object $summary -Name 'dispatchedWorkItemCount' -DefaultValue 0
    $running = Get-PropertyValue -Object $summary -Name 'runningWorkItemCount' -DefaultValue 0
    $completed = Get-PropertyValue -Object $summary -Name 'completedWorkItemCount' -DefaultValue 0
    $failed = Get-PropertyValue -Object $summary -Name 'failedWorkItemCount' -DefaultValue 0
    $retryable = Get-PropertyValue -Object $summary -Name 'retryableWorkItemCount' -DefaultValue 0
    $percent = Get-PropertyValue -Object $summary -Name 'percentComplete' -DefaultValue 0

    $failureSummary = Get-PropertyValue -Object $failures -Name 'summary' -DefaultValue $null
    $failureWorkItems = Get-PropertyValue -Object $failures -Name 'workItems' -DefaultValue $null
    $failureWorkItemCount = Get-ArrayCount -Value $failureWorkItems

    $workersCount = Get-ArrayCount -Value $workers
    $eventsCount = Get-ArrayCount -Value $events
    $runsCount = Get-ArrayCount -Value $runs

    $warnings = New-Object System.Collections.Generic.List[string]

    foreach ($result in @($summaryResult, $runsResult, $failuresResult, $workersResult, $eventsResult)) {
        if (-not $result.Success) {
            $warnings.Add(('endpoint failed: ' + $result.Url + ' :: ' + $result.Error)) | Out-Null
        }
    }

    if ([int]$queued -gt 0 -and [int]$dispatched -eq 0 -and [int]$running -eq 0) {
        $warnings.Add('queue has queued work but no dispatched/running work') | Out-Null
    }

    if ([int]$running -gt 0 -and [decimal]$percent -le 0) {
        $warnings.Add('running work has zero percent progress') | Out-Null
    }

    if ([int]$failed -gt 0 -and $failureWorkItemCount -eq 0) {
        $warnings.Add('summary shows failed work but failure list is empty') | Out-Null
    }

    $row = [pscustomobject]@{
        Sample = $sample
        Utc = $now.ToString('o')
        RunCount = $runCount
        RuntimeRunsReturned = $runsCount
        QueuedWorkItems = $queued
        DispatchedWorkItems = $dispatched
        RunningWorkItems = $running
        CompletedWorkItems = $completed
        FailedWorkItems = $failed
        RetryableWorkItems = $retryable
        PercentComplete = $percent
        FailureRowsReturned = $failureWorkItemCount
        WorkerRowsReturned = $workersCount
        EventRowsReturned = $eventsCount
        Warnings = (Join-Warnings -Warnings $warnings)
    }

    $rows.Add($row) | Out-Null

    $detail = [pscustomobject]@{
        sample = $sample
        utc = $now.ToString('o')
        summary = $summary
        runs = $runs
        failures = $failures
        workers = $workers
        events = $events
        warnings = $warnings.ToArray()
    }

    ($detail | ConvertTo-Json -Depth 20 -Compress) | Add-Content -LiteralPath $detailPath

    Write-Host ('Sample ' + $sample + '/' + $Samples + ': queued=' + $queued + ' dispatched=' + $dispatched + ' running=' + $running + ' completed=' + $completed + ' failed=' + $failed + ' warnings=' + $warnings.Count)

    if ($sample -lt $Samples) {
        Start-Sleep -Seconds $IntervalSeconds
    }
}

$rows | Export-Csv -LiteralPath $reportPath -NoTypeInformation

Write-Host ('Operational health watch report: ' + $reportPath)
Write-Host ('Operational health watch details: ' + $detailPath)

$warningRows = @($rows | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_.Warnings) })
if ($warningRows.Count -gt 0) {
    Write-Host ('Warnings found: ' + $warningRows.Count)
} else {
    Write-Host 'No warnings found.'
}
