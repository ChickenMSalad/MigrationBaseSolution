[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$AdminApiBaseUrl,

    [Parameter(Mandatory = $false)]
    [string]$RunId,

    [Parameter(Mandatory = $false)]
    [int]$IntervalSeconds = 5,

    [Parameter(Mandatory = $false)]
    [int]$MaxSamples = 0,

    [Parameter(Mandatory = $false)]
    [string]$OutputCsv
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Join-Url {
    param(
        [Parameter(Mandatory = $true)][string]$Base,
        [Parameter(Mandatory = $true)][string]$Path
    )

    $left = $Base.TrimEnd('/')
    if ($Path.StartsWith('/')) {
        return $left + $Path
    }

    return $left + '/' + $Path
}

function Invoke-JsonGet {
    param(
        [Parameter(Mandatory = $true)][string]$Url
    )

    $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -Headers @{ Accept = 'application/json' }
    if ($response.StatusCode -lt 200 -or $response.StatusCode -ge 300) {
        throw ('GET failed: ' + $Url + ' HTTP ' + $response.StatusCode)
    }

    if ([string]::IsNullOrWhiteSpace([string]$response.Content)) {
        return $null
    }

    return $response.Content | ConvertFrom-Json
}

function Get-PropertyValue {
    param(
        [Parameter(Mandatory = $true)]$Object,
        [Parameter(Mandatory = $true)][string[]]$Names,
        $DefaultValue = $null
    )

    if ($null -eq $Object) {
        return $DefaultValue
    }

    foreach ($name in $Names) {
        $property = $Object.PSObject.Properties[$name]
        if ($null -ne $property) {
            return $property.Value
        }
    }

    return $DefaultValue
}

function Is-TerminalStatus {
    param([string]$Status)

    $value = ([string]$Status).Trim().ToLowerInvariant()
    return $value -eq 'completed' -or $value -eq 'failed' -or $value -eq 'completedwithfailures' -or $value -eq 'cancelled' -or $value -eq 'canceled'
}

function Select-RunFromList {
    param(
        [Parameter(Mandatory = $true)]$Runs,
        [Parameter(Mandatory = $false)][string]$RequestedRunId
    )

    $items = @($Runs)
    if ($items.Count -eq 0) {
        return $null
    }

    if (-not [string]::IsNullOrWhiteSpace($RequestedRunId)) {
        foreach ($item in $items) {
            $candidateRunId = [string](Get-PropertyValue -Object $item -Names @('runId','RunId') -DefaultValue '')
            $candidateRunKey = [string](Get-PropertyValue -Object $item -Names @('runKey','RunKey') -DefaultValue '')
            if ($candidateRunId -eq $RequestedRunId -or $candidateRunKey -eq $RequestedRunId) {
                return $item
            }
        }

        return $null
    }

    return $items | Sort-Object -Property @{ Expression = { [string](Get-PropertyValue -Object $_ -Names @('updatedUtc','UpdatedUtc','startedUtc','StartedUtc','createdUtc','CreatedUtc') -DefaultValue '') }; Descending = $true } | Select-Object -First 1
}

if ($IntervalSeconds -lt 1) {
    $IntervalSeconds = 1
}

$baseUrl = $AdminApiBaseUrl.TrimEnd('/')
$sample = 0
$rows = New-Object System.Collections.Generic.List[object]

if ([string]::IsNullOrWhiteSpace($OutputCsv)) {
    $outputDir = Join-Path (Get-Location) 'artifacts\p7-validation'
    if (-not (Test-Path -LiteralPath $outputDir)) {
        New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    }
    $OutputCsv = Join-Path $outputDir ('run-progress-watch-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.csv')
}

Write-Host ('Watching run progress from ' + $baseUrl)
if (-not [string]::IsNullOrWhiteSpace($RunId)) {
    Write-Host ('Requested run: ' + $RunId)
}
Write-Host ('Interval seconds: ' + $IntervalSeconds)
Write-Host ('Output CSV: ' + $OutputCsv)
Write-Host ''

while ($true) {
    $sample++
    $sampleUtc = [DateTimeOffset]::UtcNow.ToString('o')

    $runsUrl = Join-Url -Base $baseUrl -Path '/api/runtime/dashboard/runs'
    $runs = Invoke-JsonGet -Url $runsUrl
    $run = Select-RunFromList -Runs $runs -RequestedRunId $RunId

    if ($null -eq $run) {
        $message = 'No run matched the requested id/key.'
        if ([string]::IsNullOrWhiteSpace($RunId)) {
            $message = 'No runs were returned by runtime dashboard.'
        }

        Write-Host ($sampleUtc + ' ' + $message)
        $rows.Add([pscustomobject]@{
            Sample = $sample
            SampleUtc = $sampleUtc
            RunId = $RunId
            RunKey = ''
            RunName = ''
            Status = 'NotFound'
            Total = 0
            Completed = 0
            Failed = 0
            Skipped = 0
            ProgressPercent = 0
            Active = 0
            QueueState = ''
            Message = $message
        }) | Out-Null
    }
    else {
        $effectiveRunId = [string](Get-PropertyValue -Object $run -Names @('runId','RunId') -DefaultValue '')
        $effectiveRunKey = [string](Get-PropertyValue -Object $run -Names @('runKey','RunKey') -DefaultValue '')
        $runName = [string](Get-PropertyValue -Object $run -Names @('runName','jobName','RunName','JobName') -DefaultValue '')
        $status = [string](Get-PropertyValue -Object $run -Names @('status','Status') -DefaultValue '')
        $total = [int](Get-PropertyValue -Object $run -Names @('totalWorkItems','workItemCount','total','Total') -DefaultValue 0)
        $completed = [int](Get-PropertyValue -Object $run -Names @('completedWorkItems','completed','Completed','succeeded','Succeeded') -DefaultValue 0)
        $failed = [int](Get-PropertyValue -Object $run -Names @('failedWorkItems','failed','Failed') -DefaultValue 0)
        $skipped = [int](Get-PropertyValue -Object $run -Names @('skippedWorkItems','skipped','Skipped') -DefaultValue 0)
        $active = [int](Get-PropertyValue -Object $run -Names @('runningWorkItems','activeWorkItems','active','Active') -DefaultValue 0)
        $percent = [decimal](Get-PropertyValue -Object $run -Names @('percentComplete','progressPercent','PercentComplete','ProgressPercent') -DefaultValue 0)

        $detailMessage = ''
        if (-not [string]::IsNullOrWhiteSpace($effectiveRunId)) {
            try {
                $detailUrl = Join-Url -Base $baseUrl -Path ('/api/runtime/dashboard/runs/' + [uri]::EscapeDataString($effectiveRunId))
                $detail = Invoke-JsonGet -Url $detailUrl
                $detailMessage = [string](Get-PropertyValue -Object $detail -Names @('message','Message','latestMessage','LatestMessage') -DefaultValue '')
            }
            catch {
                $detailMessage = $_.Exception.Message
            }
        }

        $line = ('{0} {1} {2}% total={3} completed={4} failed={5} skipped={6} active={7} {8}' -f $sampleUtc, $status, $percent, $total, $completed, $failed, $skipped, $active, $runName)
        Write-Host $line

        $rows.Add([pscustomobject]@{
            Sample = $sample
            SampleUtc = $sampleUtc
            RunId = $effectiveRunId
            RunKey = $effectiveRunKey
            RunName = $runName
            Status = $status
            Total = $total
            Completed = $completed
            Failed = $failed
            Skipped = $skipped
            ProgressPercent = $percent
            Active = $active
            QueueState = ''
            Message = $detailMessage
        }) | Out-Null

        if (Is-TerminalStatus -Status $status) {
            break
        }
    }

    if ($MaxSamples -gt 0 -and $sample -ge $MaxSamples) {
        break
    }

    Start-Sleep -Seconds $IntervalSeconds
}

$rows | Export-Csv -LiteralPath $OutputCsv -NoTypeInformation
Write-Host ''
Write-Host ('Run progress watch completed. Samples: ' + $rows.Count)
Write-Host ('Report: ' + $OutputCsv)
