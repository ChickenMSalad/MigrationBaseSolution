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

    $path = Join-Path $RootPath $RelativePath
    $Lines.Add("## $RelativePath") | Out-Null
    $Lines.Add('') | Out-Null

    if (-not (Test-Path -LiteralPath $path)) {
        $Lines.Add('Missing.') | Out-Null
        $Lines.Add('') | Out-Null
        return
    }

    $Lines.Add('Present.') | Out-Null
    $content = Get-Content -LiteralPath $path -Raw
    foreach ($pattern in $Patterns) {
        if ($null -ne $content -and $content.Contains($pattern)) {
            $Lines.Add("- Contains: ``$pattern``") | Out-Null
        }
        else {
            $Lines.Add("- Missing: ``$pattern``") | Out-Null
        }
    }
    $Lines.Add('') | Out-Null
}

function Add-SearchSection {
    param(
        [System.Collections.Generic.List[string]]$Lines,
        [string]$RootPath,
        [string]$Title,
        [string]$Pattern
    )

    $Lines.Add("## $Title") | Out-Null
    $Lines.Add('') | Out-Null

    $matches = Get-ChildItem -Path (Join-Path $RootPath 'src') -Filter '*.cs' -File -Recurse |
        Where-Object { -not (Test-IsIgnoredPath $_.FullName) } |
        Select-String -Pattern $Pattern -SimpleMatch

    $count = 0
    foreach ($match in $matches) {
        $relative = $match.Path.Substring($RootPath.Length).TrimStart('\', '/')
        $Lines.Add(("- {0}:{1}: {2}" -f $relative, $match.LineNumber, $match.Line.Trim())) | Out-Null
        $count++
    }

    if ($count -eq 0) { $Lines.Add('- No matches found.') | Out-Null }
    $Lines.Add('') | Out-Null
}

$root = Get-RepositoryRoot
$outputPath = Join-Path $root 'docs\p8\P8.2D-Distributed-Execution-Telemetry-Inventory.generated.md'
$outputFolder = Split-Path -Parent $outputPath
if (-not (Test-Path -LiteralPath $outputFolder)) {
    New-Item -ItemType Directory -Path $outputFolder | Out-Null
}

$lines = New-Object 'System.Collections.Generic.List[string]'
$lines.Add('# P8.2D Distributed Execution Telemetry Inventory') | Out-Null
$lines.Add('') | Out-Null
$lines.Add(('GeneratedUtc: {0:O}' -f [DateTime]::UtcNow)) | Out-Null
$lines.Add('') | Out-Null
$lines.Add('This inventory captures distributed Service Bus execution, settlement, completion/failure, and telemetry-relevant surfaces after P8.2C.') | Out-Null
$lines.Add('') | Out-Null

Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Processing\SqlOperationalServiceBusWorkItemExecutor.cs' -Patterns @('ISqlOperationalWorkItemExecutor', 'ExecuteAsync', 'ServiceBusWorkItemExecutionResult')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Runtime\SqlServiceBusExecutorWorker.cs' -Patterns @('ServiceBusProcessor', 'CompleteOperationalWorkItemRequest', 'FailOperationalWorkItemRequest', 'DeadLetterMessageAsync')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Program.cs' -Patterns @('SqlOperationalServiceBusWorkItemExecutor', 'AddHostedService', 'SqlServiceBusExecutorOptions')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusDispatcher\Dispatching\SqlWorkItemDispatcher.cs' -Patterns @('ServiceBusMessage', 'SendMessageAsync', 'ServiceBusWorkItemMessage')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusDispatcher\Runtime\SqlServiceBusDispatcherWorker.cs' -Patterns @('BackgroundService', 'ExecuteAsync')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Core\Migration.Infrastructure.Sql\Operational\ExecutionHistory\SqlOperationalExecutionHistoryWriter.cs' -Patterns @('IOperationalExecutionHistoryWriter', 'AttemptsTableName', 'ExecuteAsync')

Add-SearchSection -Lines $lines -RootPath $root -Title 'Service Bus message settlement calls' -Pattern 'CompleteMessageAsync'
Add-SearchSection -Lines $lines -RootPath $root -Title 'Service Bus dead-letter calls' -Pattern 'DeadLetterMessageAsync'
Add-SearchSection -Lines $lines -RootPath $root -Title 'Service Bus abandon calls' -Pattern 'AbandonMessageAsync'
Add-SearchSection -Lines $lines -RootPath $root -Title 'Operational completion calls' -Pattern 'CompleteOperationalWorkItemRequest'
Add-SearchSection -Lines $lines -RootPath $root -Title 'Operational failure calls' -Pattern 'FailOperationalWorkItemRequest'
Add-SearchSection -Lines $lines -RootPath $root -Title 'Execution history writer references' -Pattern 'IOperationalExecutionHistoryWriter'
Add-SearchSection -Lines $lines -RootPath $root -Title 'Worker logging references' -Pattern '_logger.Log'

$lines.Add('## Recommended P8.2E target') | Out-Null
$lines.Add('') | Out-Null
$lines.Add('- Add explicit distributed execution telemetry around Service Bus receive, SQL work-item execution, settlement, dead-letter, and retry paths.') | Out-Null
$lines.Add('- Keep settlement semantics inside `SqlServiceBusExecutorWorker`.') | Out-Null
$lines.Add('- Keep real execution delegated through `SqlOperationalServiceBusWorkItemExecutor` and `ISqlOperationalWorkItemExecutor`.') | Out-Null
$lines.Add('- Do not create a second execution-history mechanism; use the existing SQL operational execution-history path.') | Out-Null
$lines.Add('') | Out-Null

Set-Content -LiteralPath $outputPath -Value $lines -Encoding UTF8
Write-Host "Wrote $outputPath"
