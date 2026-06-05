Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    if ($PSScriptRoot) { return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path }
    if ($MyInvocation -and $MyInvocation.MyCommand -and $MyInvocation.MyCommand.Path) {
        return (Resolve-Path (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) '..')).Path
    }
    return (Get-Location).Path
}

function Add-Line {
    param([System.Collections.Generic.List[string]]$Lines, [string]$Text)
    $Lines.Add($Text) | Out-Null
}

function Add-FileSummary {
    param(
        [System.Collections.Generic.List[string]]$Lines,
        [string]$RootPath,
        [string]$RelativePath,
        [string[]]$Patterns
    )

    Add-Line -Lines $Lines -Text ''
    Add-Line -Lines $Lines -Text ("## {0}" -f $RelativePath)
    Add-Line -Lines $Lines -Text ''

    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        Add-Line -Lines $Lines -Text 'Missing.'
        return
    }

    Add-Line -Lines $Lines -Text 'Present.'
    $content = Get-Content -LiteralPath $path -Raw
    foreach ($pattern in $Patterns) {
        if ($null -ne $content -and $content.Contains($pattern)) {
            Add-Line -Lines $Lines -Text ("- Contains: ``{0}``" -f $pattern)
        }
        else {
            Add-Line -Lines $Lines -Text ("- Missing: ``{0}``" -f $pattern)
        }
    }
}

function Add-MatchingLines {
    param(
        [System.Collections.Generic.List[string]]$Lines,
        [string]$RootPath,
        [string]$RelativePath,
        [string[]]$Patterns
    )

    Add-Line -Lines $Lines -Text ''
    Add-Line -Lines $Lines -Text ("## Matching lines: {0}" -f $RelativePath)
    Add-Line -Lines $Lines -Text ''

    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        Add-Line -Lines $Lines -Text 'Missing.'
        return
    }

    $fileLines = Get-Content -LiteralPath $path
    $matches = New-Object System.Collections.Generic.List[string]

    for ($i = 0; $i -lt $fileLines.Length; $i++) {
        $line = $fileLines[$i]
        foreach ($pattern in $Patterns) {
            if ($line -like ("*{0}*" -f $pattern)) {
                $lineNumber = $i + 1
                $matches.Add(("- {0}:{1}: {2}" -f $RelativePath, $lineNumber, $line.Trim())) | Out-Null
                break
            }
        }
    }

    if ($matches.Count -eq 0) {
        Add-Line -Lines $Lines -Text 'No matches found.'
    }
    else {
        foreach ($match in $matches) { Add-Line -Lines $Lines -Text $match }
    }
}

$root = Get-RepositoryRoot
$outDir = Join-Path $root 'docs\p8'
if (-not (Test-Path -LiteralPath $outDir)) { New-Item -ItemType Directory -Path $outDir | Out-Null }
$out = Join-Path $outDir 'P8.3D.1-Runtime-Activity-Usage-Patch-Inventory.generated.md'

$lines = New-Object System.Collections.Generic.List[string]
Add-Line -Lines $lines -Text '# P8.3D.1 Runtime Activity Usage Patch Inventory'
Add-Line -Lines $lines -Text ''
Add-Line -Lines $lines -Text ("GeneratedUtc: {0:o}" -f [DateTimeOffset]::UtcNow)
Add-Line -Lines $lines -Text ''
Add-Line -Lines $lines -Text 'This inventory is generated before runtime Activity usage is applied. It is intended to support a manual, repo-specific patch instead of brittle repair scripts.'

Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalExecutionActivity.cs' -Patterns @('StartSqlQueueWorkItemExecution','StartServiceBusWorkItemExecution','StartServiceBusDispatch','SetExecutionDuration','SetExecutionResult')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Services\SqlOperationalWorkItemWorker.cs' -Patterns @('BeginScope','ExecuteClaimedItemAsync','OperationalExecutionActivity','CompleteOperationalWorkItemRequest','FailOperationalWorkItemRequest')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Runtime\SqlServiceBusExecutorWorker.cs' -Patterns @('BeginScope','OperationalExecutionActivity','CompleteMessageAsync','DeadLetterMessageAsync','AbandonMessageAsync','ServiceBusCorrelationId')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusDispatcher\Dispatching\SqlWorkItemDispatcher.cs' -Patterns @('ServiceBusMessage','SendMessageAsync','CorrelationId','OperationalExecutionActivity')

Add-MatchingLines -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Services\SqlOperationalWorkItemWorker.cs' -Patterns @('BeginScope','ExecuteAsync','CompleteAsync','FailAsync','DateTimeOffset.UtcNow')
Add-MatchingLines -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Runtime\SqlServiceBusExecutorWorker.cs' -Patterns @('BeginScope','_executor.ExecuteAsync','CompleteMessageAsync','DeadLetterMessageAsync','AbandonMessageAsync')
Add-MatchingLines -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusDispatcher\Dispatching\SqlWorkItemDispatcher.cs' -Patterns @('ServiceBusMessage','SendMessageAsync','CorrelationId','WorkItemId','RunId')

Add-Line -Lines $lines -Text ''
Add-Line -Lines $lines -Text '## P8.3D.1 recommendation'
Add-Line -Lines $lines -Text ''
Add-Line -Lines $lines -Text '- Do not use broad source mutation scripts for Activity usage.'
Add-Line -Lines $lines -Text '- Use this inventory to apply a small manual patch around existing execution/send boundaries.'
Add-Line -Lines $lines -Text '- Preserve existing BeginScope correlation and queue settlement behavior.'

Set-Content -LiteralPath $out -Value $lines -Encoding UTF8
Write-Host ("Wrote {0}" -f $out)
