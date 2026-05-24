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
            $Lines.Add(("- Contains: ``{0}``" -f $pattern)) | Out-Null
        }
        else {
            $Lines.Add(("- Missing: ``{0}``" -f $pattern)) | Out-Null
        }
    }
    $Lines.Add('') | Out-Null
}

function Add-SearchSection {
    param(
        [System.Collections.Generic.List[string]]$Lines,
        [string]$RootPath,
        [string]$Title,
        [string]$UnderRelativePath,
        [string]$Pattern
    )

    $Lines.Add("## $Title") | Out-Null
    $Lines.Add('') | Out-Null

    $folder = Join-Path $RootPath $UnderRelativePath
    if (-not (Test-Path -LiteralPath $folder)) {
        $Lines.Add("- Search root missing: ``$UnderRelativePath``") | Out-Null
        $Lines.Add('') | Out-Null
        return
    }

    $matches = Get-ChildItem -Path $folder -Filter '*.cs' -File -Recurse |
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
$outputPath = Join-Path $root 'docs\p8\P8.2B-ServiceBus-Execution-Binding-Contract-Inventory.generated.md'
$outputFolder = Split-Path -Parent $outputPath
if (-not (Test-Path -LiteralPath $outputFolder)) { New-Item -ItemType Directory -Path $outputFolder | Out-Null }

$lines = New-Object 'System.Collections.Generic.List[string]'
$lines.Add('# P8.2B Service Bus Execution Binding Contract Inventory') | Out-Null
$lines.Add('') | Out-Null
$lines.Add(('GeneratedUtc: {0:O}' -f [DateTime]::UtcNow)) | Out-Null
$lines.Add('') | Out-Null
$lines.Add('This inventory captures exact Service Bus executor/dispatcher contracts before replacing placeholder Service Bus execution with real operational runtime execution.') | Out-Null
$lines.Add('') | Out-Null

Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Processing\IServiceBusWorkItemExecutor.cs' -Patterns @('IServiceBusWorkItemExecutor', 'ExecuteAsync', 'ServiceBusWorkItemExecutionResult')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Processing\ServiceBusWorkItemMessage.cs' -Patterns @('ServiceBusWorkItemMessage', 'WorkItemId', 'RunId')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Processing\PlaceholderServiceBusWorkItemExecutor.cs' -Patterns @('PlaceholderServiceBusWorkItemExecutor', 'ExecuteAsync', 'ServiceBusWorkItemExecutionResult')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Runtime\SqlServiceBusExecutorWorker.cs' -Patterns @('ServiceBusProcessor', 'CompleteOperationalWorkItemRequest', 'FailOperationalWorkItemRequest', 'DeadLetterMessageAsync')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Program.cs' -Patterns @('PlaceholderServiceBusWorkItemExecutor', 'SqlServiceBusExecutorOptions', 'AddHostedService')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusDispatcher\Dispatching\ServiceBusWorkItemMessage.cs' -Patterns @('ServiceBusWorkItemMessage', 'WorkItemId', 'RunId')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusDispatcher\Dispatching\SqlWorkItemDispatcher.cs' -Patterns @('ServiceBusMessage', 'ServiceBusWorkItemMessage', 'SendMessageAsync')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Core\Migration.Infrastructure.Sql\Operational\WorkItems\SqlOperationalWorkItemQueue.cs' -Patterns @('GetAsync', 'CompleteAsync', 'FailAsync')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Services\ISqlOperationalWorkItemExecutor.cs' -Patterns @('ISqlOperationalWorkItemExecutor', 'ExecuteAsync', 'SqlOperationalWorkItemExecutionResult')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Core\Migration.Infrastructure.Sql\Operational\ExecutionHistory\SqlOperationalExecutionHistoryWriter.cs' -Patterns @('IOperationalExecutionHistoryWriter', 'ExecuteAsync', 'AttemptsTableName')

Add-SearchSection -Lines $lines -RootPath $root -Title 'Service Bus executor message/result constructors' -UnderRelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor' -Pattern 'record ServiceBus'
Add-SearchSection -Lines $lines -RootPath $root -Title 'Service Bus executor settlement calls' -UnderRelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor' -Pattern 'DeadLetterMessageAsync'
Add-SearchSection -Lines $lines -RootPath $root -Title 'Service Bus executor SQL completion calls' -UnderRelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor' -Pattern 'CompleteOperationalWorkItemRequest'
Add-SearchSection -Lines $lines -RootPath $root -Title 'Service Bus executor SQL failure calls' -UnderRelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor' -Pattern 'FailOperationalWorkItemRequest'
Add-SearchSection -Lines $lines -RootPath $root -Title 'SQL operational executor boundary' -UnderRelativePath 'src\Workers\Migration.Workers.QueueExecutor\Services' -Pattern 'ISqlOperationalWorkItemExecutor'
Add-SearchSection -Lines $lines -RootPath $root -Title 'Execution history writer boundary' -UnderRelativePath 'src\Core\Migration.Infrastructure.Sql\Operational\ExecutionHistory' -Pattern 'IOperationalExecutionHistoryWriter'

$lines.Add('## Recommended P8.2C implementation target') | Out-Null
$lines.Add('') | Out-Null
$lines.Add('- Replace the placeholder Service Bus work item executor registration with a real SQL operational runtime executor adapter.') | Out-Null
$lines.Add('- The adapter should fetch the SQL operational work item by WorkItemId, invoke the existing SQL operational execution boundary, and return the Service Bus execution result shape expected by SqlServiceBusExecutorWorker.') | Out-Null
$lines.Add('- Preserve existing SqlServiceBusExecutorWorker completion, failure, and dead-letter settlement behavior.') | Out-Null
$lines.Add('- Do not create a parallel Service Bus message contract if dispatcher and executor message shapes already align.') | Out-Null
$lines.Add('- Do not add package references unless a compile error proves they are required.') | Out-Null
$lines.Add('') | Out-Null

Set-Content -LiteralPath $outputPath -Value $lines -Encoding UTF8
Write-Host "Wrote $outputPath"
