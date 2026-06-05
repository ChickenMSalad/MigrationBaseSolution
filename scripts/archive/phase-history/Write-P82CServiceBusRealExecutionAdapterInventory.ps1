Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    if ($PSScriptRoot) { return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path }
    if ($MyInvocation -and $MyInvocation.MyCommand -and $MyInvocation.MyCommand.Path) {
        return (Resolve-Path (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) '..')).Path
    }
    return (Get-Location).Path
}

function Add-FileSummary {
    param([System.Collections.Generic.List[string]]$Lines, [string]$RootPath, [string]$RelativePath, [string[]]$Patterns)
    $path = Join-Path $RootPath $RelativePath
    $Lines.Add("## $RelativePath") | Out-Null
    $Lines.Add('') | Out-Null
    if (-not (Test-Path -LiteralPath $path)) { $Lines.Add('Missing.') | Out-Null; $Lines.Add('') | Out-Null; return }
    $Lines.Add('Present.') | Out-Null
    $content = Get-Content -LiteralPath $path -Raw
    foreach ($pattern in $Patterns) {
        if ($null -ne $content -and $content.Contains($pattern)) { $Lines.Add("- Contains: ``$pattern``") | Out-Null }
        else { $Lines.Add("- Missing: ``$pattern``") | Out-Null }
    }
    $Lines.Add('') | Out-Null
}

$root = Get-RepositoryRoot
$outputPath = Join-Path $root 'docs\p8\P8.2C-ServiceBus-Real-Execution-Adapter-Inventory.generated.md'
$outputFolder = Split-Path -Parent $outputPath
if (-not (Test-Path -LiteralPath $outputFolder)) { New-Item -ItemType Directory -Path $outputFolder | Out-Null }

$lines = New-Object 'System.Collections.Generic.List[string]'
$lines.Add('# P8.2C Service Bus Real Execution Adapter Inventory') | Out-Null
$lines.Add('') | Out-Null
$lines.Add(('GeneratedUtc: {0:O}' -f [DateTime]::UtcNow)) | Out-Null
$lines.Add('') | Out-Null
$lines.Add('This inventory confirms Service Bus execution is wired through the SQL operational execution boundary instead of the placeholder executor.') | Out-Null
$lines.Add('') | Out-Null

Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Processing\SqlOperationalServiceBusWorkItemExecutor.cs' -Patterns @('ISqlOperationalWorkItemExecutor', 'IOperationalWorkItemQueue', 'ExecuteAsync', 'ServiceBusWorkItemExecutionResult')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Program.cs' -Patterns @('AddSqlOperationalMigrationJobWorkItemExecutor', 'SqlOperationalServiceBusWorkItemExecutor', 'PlaceholderServiceBusWorkItemExecutor')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Runtime\SqlServiceBusExecutorWorker.cs' -Patterns @('CompleteOperationalWorkItemRequest', 'FailOperationalWorkItemRequest', 'DeadLetterMessageAsync')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Migration.Workers.ServiceBusExecutor.csproj' -Patterns @('Migration.Workers.QueueExecutor.csproj', 'Azure.Messaging.ServiceBus')

Set-Content -LiteralPath $outputPath -Value $lines -Encoding UTF8
Write-Host "Wrote $outputPath"
