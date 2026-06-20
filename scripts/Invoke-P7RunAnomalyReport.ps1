[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$AdminApiBaseUrl,

    [int]$WorkerStaleMinutes = 5,

    [int]$RunningStaleMinutes = 10,

    [int]$Take = 100
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$baseUrl = $AdminApiBaseUrl.TrimEnd('/')
$reportRoot = Join-Path (Get-Location) 'artifacts\p7-validation'
if (-not (Test-Path -LiteralPath $reportRoot)) {
    New-Item -ItemType Directory -Path $reportRoot -Force | Out-Null
}

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$csvPath = Join-Path $reportRoot ('p7-run-anomaly-report-' + $stamp + '.csv')
$jsonlPath = Join-Path $reportRoot ('p7-run-anomaly-snapshot-' + $stamp + '.jsonl')

$findings = New-Object System.Collections.Generic.List[object]
$snapshots = New-Object System.Collections.Generic.List[object]

function Add-Finding {
    param(
        [string]$Severity,
        [string]$Area,
        [string]$Key,
        [string]$Message,
        [string]$Recommendation
    )

    $findings.Add([pscustomobject]@{
        EvaluatedUtc = [DateTimeOffset]::UtcNow.ToString('o')
        Severity = $Severity
        Area = $Area
        Key = $Key
        Message = $Message
        Recommendation = $Recommendation
    }) | Out-Null
}

function Get-JsonEndpoint {
    param(
        [string]$Path,
        [string]$Name
    )

    $url = $baseUrl + $Path
    try {
        $response = Invoke-WebRequest -Uri $url -UseBasicParsing -Headers @{ Accept = 'application/json' }
        if ($response.StatusCode -lt 200 -or $response.StatusCode -gt 299) {
            Add-Finding -Severity 'Error' -Area 'API' -Key $Name -Message ('HTTP ' + $response.StatusCode + ' from ' + $url) -Recommendation 'Check Admin API route mapping and deployment.'
            return $null
        }

        $contentType = ''
        if ($response.Headers.ContainsKey('Content-Type')) {
            $contentType = [string]$response.Headers['Content-Type']
        }

        if ($contentType.IndexOf('application/json', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
            Add-Finding -Severity 'Error' -Area 'API' -Key $Name -Message ('Non-JSON response from ' + $url + ': ' + $contentType) -Recommendation 'Check API base URL and Static Web App routing.'
            return $null
        }

        return ($response.Content | ConvertFrom-Json)
    }
    catch {
        Add-Finding -Severity 'Error' -Area 'API' -Key $Name -Message $_.Exception.Message -Recommendation 'Check Admin API availability and route mapping.'
        return $null
    }
}

function Get-PropValue {
    param(
        [object]$Object,
        [string[]]$Names
    )

    if ($null -eq $Object) { return $null }
    foreach ($name in $Names) {
        $property = $Object.PSObject.Properties[$name]
        if ($null -ne $property) { return $property.Value }
    }
    return $null
}

function To-Array {
    param([object]$Value)
    if ($null -eq $Value) { return @() }
    if ($Value -is [System.Array]) { return @($Value) }
    return @($Value)
}

$summary = Get-JsonEndpoint -Path '/api/runtime/dashboard/summary' -Name 'Runtime dashboard summary'
$runs = Get-JsonEndpoint -Path ('/api/runtime/dashboard/runs?take=' + $Take) -Name 'Runtime dashboard runs'
$failures = Get-JsonEndpoint -Path ('/api/runtime/dashboard/failures?take=' + $Take) -Name 'Runtime failures'
$workers = Get-JsonEndpoint -Path '/api/operational/workers/telemetry' -Name 'Operational worker telemetry'
$events = Get-JsonEndpoint -Path ('/api/operational/events/recent?take=' + $Take) -Name 'Operational events recent'
$sessions = Get-JsonEndpoint -Path ('/api/operational/execution-sessions/recent?take=' + $Take) -Name 'Execution sessions recent'

$snapshot = [pscustomobject]@{
    evaluatedUtc = [DateTimeOffset]::UtcNow.ToString('o')
    summary = $summary
    runs = $runs
    failures = $failures
    workers = $workers
    events = $events
    sessions = $sessions
}
$snapshots.Add($snapshot) | Out-Null

if ($null -ne $summary) {
    $failedWorkItems = Get-PropValue -Object $summary -Names @('failedWorkItemCount', 'failed')
    $activeWorkItems = Get-PropValue -Object $summary -Names @('runningWorkItemCount', 'activeWorkItemCount', 'active')
    $queuedWorkItems = Get-PropValue -Object $summary -Names @('queuedWorkItemCount', 'queued')

    if ($null -ne $failedWorkItems -and [int]$failedWorkItems -gt 0) {
        Add-Finding -Severity 'Warning' -Area 'Failures' -Key 'failedWorkItems' -Message ('Failed work items: ' + $failedWorkItems) -Recommendation 'Open Failure Retry and export failures before resetting/retrying.'
    }

    if ($null -ne $queuedWorkItems -and [int]$queuedWorkItems -gt 0 -and ($null -eq $activeWorkItems -or [int]$activeWorkItems -eq 0)) {
        Add-Finding -Severity 'Warning' -Area 'Queue' -Key 'queuedWithoutActiveWorker' -Message ('Queued work items exist but active work count is zero. Queued=' + $queuedWorkItems) -Recommendation 'Check dispatcher polling and worker Always On settings.'
    }
}

foreach ($run in (To-Array -Value $runs)) {
    $runId = [string](Get-PropValue -Object $run -Names @('runId', 'id'))
    $status = [string](Get-PropValue -Object $run -Names @('status', 'runStatus'))
    $updatedRaw = Get-PropValue -Object $run -Names @('updatedUtc', 'lastUpdatedUtc', 'completedUtc', 'startedUtc')
    $failedCount = Get-PropValue -Object $run -Names @('failedWorkItemCount', 'failedCount', 'failures')

    if ($status -match 'Running|Dispatching|Leased') {
        if ($null -ne $updatedRaw) {
            $updated = [DateTimeOffset]::MinValue
            if ([DateTimeOffset]::TryParse([string]$updatedRaw, [ref]$updated)) {
                $age = [DateTimeOffset]::UtcNow - $updated.ToUniversalTime()
                if ($age.TotalMinutes -gt $RunningStaleMinutes) {
                    Add-Finding -Severity 'Warning' -Area 'Run' -Key $runId -Message ('Run appears active but has not updated for ' + [math]::Round($age.TotalMinutes, 1) + ' minutes. Status=' + $status) -Recommendation 'Inspect run detail timeline and worker telemetry.'
                }
            }
        }
    }

    if ($null -ne $failedCount -and [int]$failedCount -gt 0) {
        Add-Finding -Severity 'Warning' -Area 'Run' -Key $runId -Message ('Run has failed work items. Failed=' + $failedCount) -Recommendation 'Export failure report and retry only failed rows if appropriate.'
    }
}

foreach ($worker in (To-Array -Value $workers)) {
    $workerId = [string](Get-PropValue -Object $worker -Names @('workerId', 'id', 'name'))
    $lastSeenRaw = Get-PropValue -Object $worker -Names @('lastSeenUtc', 'heartbeatUtc', 'updatedUtc')
    $state = [string](Get-PropValue -Object $worker -Names @('status', 'state', 'health'))

    if ($null -ne $lastSeenRaw) {
        $lastSeen = [DateTimeOffset]::MinValue
        if ([DateTimeOffset]::TryParse([string]$lastSeenRaw, [ref]$lastSeen)) {
            $age = [DateTimeOffset]::UtcNow - $lastSeen.ToUniversalTime()
            if ($age.TotalMinutes -gt $WorkerStaleMinutes) {
                Add-Finding -Severity 'Warning' -Area 'Worker' -Key $workerId -Message ('Worker heartbeat is stale. AgeMinutes=' + [math]::Round($age.TotalMinutes, 1) + '; State=' + $state) -Recommendation 'Check App Service Always On, worker logs, and latest heartbeat writer.'
            }
        }
    }
}

if ($findings.Count -eq 0) {
    Add-Finding -Severity 'Info' -Area 'Overall' -Key 'healthy' -Message 'No runtime anomalies detected by this report.' -Recommendation 'Continue monitoring during the next active run.'
}

$findings | Export-Csv -LiteralPath $csvPath -NoTypeInformation
foreach ($item in $snapshots) {
    ($item | ConvertTo-Json -Depth 20 -Compress) | Add-Content -LiteralPath $jsonlPath
}

Write-Host 'P7 run anomaly report completed.'
Write-Host ('Findings: ' + $findings.Count)
Write-Host ('CSV: ' + $csvPath)
Write-Host ('JSONL: ' + $jsonlPath)

foreach ($finding in $findings) {
    Write-Host ($finding.Severity + ' [' + $finding.Area + '] ' + $finding.Message)
}
