[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$AdminApiBaseUrl,

    [Parameter(Mandatory = $false)]
    [string]$RunId,

    [Parameter(Mandatory = $false)]
    [string]$OutputRoot = '',

    [Parameter(Mandatory = $false)]
    [int]$RecentEventLimit = 250
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function New-SafeFileNamePart {
    param([string]$Value)

    $text = $Value
    if ([string]::IsNullOrWhiteSpace($text)) {
        return 'all-runs'
    }

    foreach ($bad in [System.IO.Path]::GetInvalidFileNameChars()) {
        $text = $text.Replace([string]$bad, '_')
    }

    return $text
}

function Get-JsonEndpoint {
    param(
        [string]$BaseUrl,
        [string]$Path
    )

    $url = $BaseUrl.TrimEnd('/') + $Path
    $response = Invoke-WebRequest -Uri $url -UseBasicParsing -Headers @{ Accept = 'application/json' }

    if ($response.StatusCode -lt 200 -or $response.StatusCode -ge 300) {
        throw ('Unexpected HTTP status from ' + $url + ': ' + $response.StatusCode)
    }

    $text = [string]$response.Content
    if ($text.TrimStart().StartsWith('<')) {
        throw ('Endpoint returned HTML instead of JSON: ' + $url)
    }

    return $text | ConvertFrom-Json
}

function Write-JsonFile {
    param(
        [object]$Value,
        [string]$Path
    )

    $json = $Value | ConvertTo-Json -Depth 20
    Set-Content -LiteralPath $Path -Value $json -Encoding UTF8
}

function Write-CsvFile {
    param(
        [object[]]$Rows,
        [string]$Path
    )

    $safeRows = @($Rows)
    if ($safeRows.Count -eq 0) {
        $safeRows = @([pscustomobject]@{ Message = 'No rows returned.' })
    }

    $safeRows | Export-Csv -LiteralPath $Path -NoTypeInformation
}

$baseUrl = $AdminApiBaseUrl.TrimEnd('/')

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path (Get-Location) 'artifacts\p7-validation'
}

if (-not (Test-Path -LiteralPath $OutputRoot)) {
    New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null
}

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$runPart = New-SafeFileNamePart -Value $RunId
$outDir = Join-Path $OutputRoot ('run-evidence-' + $runPart + '-' + $stamp)
New-Item -ItemType Directory -Path $outDir -Force | Out-Null

Write-Host ('Collecting run evidence from ' + $baseUrl)
Write-Host ('Output: ' + $outDir)

$summary = Get-JsonEndpoint -BaseUrl $baseUrl -Path '/api/runtime/dashboard/summary'
$runs = Get-JsonEndpoint -BaseUrl $baseUrl -Path '/api/runtime/dashboard/runs'
$failures = Get-JsonEndpoint -BaseUrl $baseUrl -Path '/api/runtime/dashboard/failures'
$events = Get-JsonEndpoint -BaseUrl $baseUrl -Path '/api/operational/events/recent'
$workers = Get-JsonEndpoint -BaseUrl $baseUrl -Path '/api/operational/workers/telemetry'
$sessions = Get-JsonEndpoint -BaseUrl $baseUrl -Path '/api/operational/execution-sessions/recent'

$selectedRuns = @($runs)
if (-not [string]::IsNullOrWhiteSpace($RunId)) {
    $selectedRuns = @($runs | Where-Object {
        ([string]$_.runId) -eq $RunId -or
        ([string]$_.runKey) -eq $RunId -or
        ([string]$_.id) -eq $RunId
    })
}

$selectedEvents = @($events)
if (-not [string]::IsNullOrWhiteSpace($RunId)) {
    $selectedEvents = @($events | Where-Object {
        $text = ($_ | ConvertTo-Json -Depth 8)
        $text.IndexOf($RunId, [System.StringComparison]::OrdinalIgnoreCase) -ge 0
    })
}

$selectedSessions = @($sessions)
if (-not [string]::IsNullOrWhiteSpace($RunId)) {
    $selectedSessions = @($sessions | Where-Object {
        $text = ($_ | ConvertTo-Json -Depth 8)
        $text.IndexOf($RunId, [System.StringComparison]::OrdinalIgnoreCase) -ge 0
    })
}

Write-JsonFile -Value $summary -Path (Join-Path $outDir 'dashboard-summary.json')
Write-JsonFile -Value $runs -Path (Join-Path $outDir 'dashboard-runs.json')
Write-JsonFile -Value $failures -Path (Join-Path $outDir 'runtime-failures.json')
Write-JsonFile -Value $events -Path (Join-Path $outDir 'operational-events-recent.json')
Write-JsonFile -Value $workers -Path (Join-Path $outDir 'worker-telemetry.json')
Write-JsonFile -Value $sessions -Path (Join-Path $outDir 'execution-sessions.json')

Write-CsvFile -Rows $selectedRuns -Path (Join-Path $outDir 'selected-runs.csv')
Write-CsvFile -Rows $selectedEvents -Path (Join-Path $outDir 'selected-events.csv')
Write-CsvFile -Rows $selectedSessions -Path (Join-Path $outDir 'selected-sessions.csv')

$readme = @()
$readme += 'P7 Run Evidence Bundle'
$readme += '======================'
$readme += ''
$readme += ('CollectedUtc: ' + (Get-Date).ToUniversalTime().ToString('o'))
$readme += ('AdminApiBaseUrl: ' + $baseUrl)
$readme += ('RunId filter: ' + ($(if ([string]::IsNullOrWhiteSpace($RunId)) { '(none)' } else { $RunId })))
$readme += ''
$readme += 'Files:'
$readme += '- dashboard-summary.json'
$readme += '- dashboard-runs.json'
$readme += '- runtime-failures.json'
$readme += '- operational-events-recent.json'
$readme += '- worker-telemetry.json'
$readme += '- execution-sessions.json'
$readme += '- selected-runs.csv'
$readme += '- selected-events.csv'
$readme += '- selected-sessions.csv'

Set-Content -LiteralPath (Join-Path $outDir 'README.txt') -Value $readme -Encoding UTF8

Write-Host 'Run evidence bundle complete.'
Write-Host ('Output: ' + $outDir)
