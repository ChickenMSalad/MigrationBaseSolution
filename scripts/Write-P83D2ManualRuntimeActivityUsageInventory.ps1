Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    if ($PSScriptRoot) { return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path }
    if ($MyInvocation -and $MyInvocation.MyCommand -and $MyInvocation.MyCommand.Path) {
        return (Resolve-Path (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) '..')).Path
    }
    return (Get-Location).Path
}

function Test-IsIgnoredPath {
    param([string]$Path)
    if ([string]::IsNullOrWhiteSpace($Path)) { return $false }
    $normalized = $Path.Replace('/', '\').ToLowerInvariant()
    return ($normalized.Contains('\bin\') -or $normalized.Contains('\obj\'))
}

function Add-FileSummary {
    param(
        [System.Collections.Generic.List[string]]$Lines,
        [string]$RootPath,
        [string]$RelativePath,
        [string[]]$Patterns
    )

    $Lines.Add('') | Out-Null
    $Lines.Add(('## {0}' -f $RelativePath)) | Out-Null
    $Lines.Add('') | Out-Null

    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        $Lines.Add('Missing.') | Out-Null
        return
    }

    $Lines.Add('Present.') | Out-Null
    $content = Get-Content -LiteralPath $path -Raw
    foreach ($pattern in $Patterns) {
        if ($null -ne $content -and $content.Contains($pattern)) {
            $Lines.Add(('- Contains: `{0}`' -f $pattern)) | Out-Null
        }
        else {
            $Lines.Add(('- Missing: `{0}`' -f $pattern)) | Out-Null
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

    $Lines.Add('') | Out-Null
    $Lines.Add(('## Matching lines: {0}' -f $RelativePath)) | Out-Null
    $Lines.Add('') | Out-Null

    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        $Lines.Add('Missing file.') | Out-Null
        return
    }

    $contentLines = Get-Content -LiteralPath $path
    $found = $false
    for ($i = 0; $i -lt $contentLines.Length; $i++) {
        $line = $contentLines[$i]
        foreach ($pattern in $Patterns) {
            if ($line -like ('*' + $pattern + '*')) {
                $found = $true
                $lineNumber = $i + 1
                $Lines.Add(('- {0}:{1}: {2}' -f $RelativePath, $lineNumber, $line.Trim())) | Out-Null
                break
            }
        }
    }

    if (-not $found) {
        $Lines.Add('No matching lines found.') | Out-Null
    }
}

$root = Get-RepositoryRoot
$outDir = Join-Path $root 'docs\p8'
if (-not (Test-Path -LiteralPath $outDir)) { New-Item -ItemType Directory -Path $outDir | Out-Null }
$out = Join-Path $outDir 'P8.3D.2-Manual-Runtime-Activity-Usage-Inventory.generated.md'

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('# P8.3D.2 Manual Runtime Activity Usage Inventory') | Out-Null
$lines.Add('') | Out-Null
$lines.Add(('GeneratedUtc: {0:O}' -f [DateTimeOffset]::UtcNow)) | Out-Null
$lines.Add('') | Out-Null
$lines.Add('This inventory checks the manual Activity usage patch surfaces without mutating source files.') | Out-Null

Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalExecutionActivity.cs' -Patterns @('StartSqlQueueWorkItemExecution', 'StartServiceBusWorkItemExecution', 'StartServiceBusDispatch', 'SetExecutionDuration', 'SetExecutionResult')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Services\SqlOperationalWorkItemWorker.cs' -Patterns @('OperationalExecutionActivity', 'StartSqlQueueWorkItemExecution', 'SetExecutionDuration', 'SetExecutionResult', 'BeginScope')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Runtime\SqlServiceBusExecutorWorker.cs' -Patterns @('OperationalExecutionActivity', 'StartServiceBusWorkItemExecution', 'SetExecutionDuration', 'SetExecutionResult', 'BeginScope')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusDispatcher\Dispatching\SqlWorkItemDispatcher.cs' -Patterns @('OperationalExecutionActivity', 'StartServiceBusDispatch', 'SetExecutionDuration', 'SetExecutionResult', 'SendMessageAsync')

Add-MatchingLines -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Services\SqlOperationalWorkItemWorker.cs' -Patterns @('startedAtUtc', 'BeginScope', '_executor.ExecuteAsync', 'CompleteOperationalWorkItemRequest', 'FailOperationalWorkItemRequest', 'OperationalExecutionActivity')
Add-MatchingLines -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Runtime\SqlServiceBusExecutorWorker.cs' -Patterns @('BeginScope', '_executor.ExecuteAsync', 'CompleteMessageAsync', 'DeadLetterMessageAsync', 'AbandonMessageAsync', 'OperationalExecutionActivity')
Add-MatchingLines -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusDispatcher\Dispatching\SqlWorkItemDispatcher.cs' -Patterns @('ServiceBusMessage', 'CorrelationId', 'SendMessageAsync', 'MarkDispatchedAsync', 'OperationalExecutionActivity')

$lines.Add('') | Out-Null
$lines.Add('## Recommendation') | Out-Null
$lines.Add('') | Out-Null
$lines.Add('- Apply runtime Activity usage manually from docs/p8/P8.3D.2-Manual-Runtime-Activity-Usage-Patch.md.') | Out-Null
$lines.Add('- Do not use broad repair scripts for this patch.') | Out-Null
$lines.Add('- Preserve existing queue settlement and SQL completion/failure behavior.') | Out-Null

Set-Content -LiteralPath $out -Value $lines -Encoding UTF8
Write-Host "Wrote $out"
