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
        [string]$Pattern
    )

    $Lines.Add("## $Title") | Out-Null
    $Lines.Add('') | Out-Null

    $searchRoots = @('src', 'deploy', 'docs')
    $matches = @()
    foreach ($searchRoot in $searchRoots) {
        $folder = Join-Path $RootPath $searchRoot
        if (-not (Test-Path -LiteralPath $folder)) { continue }
        $matches += Get-ChildItem -Path $folder -File -Recurse |
            Where-Object { -not (Test-IsIgnoredPath $_.FullName) -and $_.Extension -in @('.cs', '.csproj', '.json', '.md', '.ps1', '.template') } |
            Select-String -Pattern $Pattern -SimpleMatch
    }

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
$outputPath = Join-Path $root 'docs\p8\P8.2A-ServiceBus-Execution-Inventory.generated.md'
$outputFolder = Split-Path -Parent $outputPath
if (-not (Test-Path -LiteralPath $outputFolder)) { New-Item -ItemType Directory -Path $outputFolder | Out-Null }

$lines = New-Object 'System.Collections.Generic.List[string]'
$lines.Add('# P8.2A Service Bus Execution Inventory') | Out-Null
$lines.Add('') | Out-Null
$lines.Add(('GeneratedUtc: {0:O}' -f [DateTime]::UtcNow)) | Out-Null
$lines.Add('') | Out-Null
$lines.Add('This inventory captures existing Service Bus dispatcher/executor surfaces before P8.2 distributed execution implementation work.') | Out-Null
$lines.Add('') | Out-Null

Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusDispatcher\Program.cs' -Patterns @('SqlServiceBusDispatcherWorker', 'AddHostedService', 'Configuration')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Program.cs' -Patterns @('SqlServiceBusExecutorOptions', 'SqlServiceBusExecutorWorker', 'AddHostedService')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Runtime\SqlServiceBusExecutorWorker.cs' -Patterns @('BackgroundService', 'ServiceBusConnectionString', 'QueueName')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Processing\IServiceBusWorkItemExecutor.cs' -Patterns @('IServiceBusWorkItemExecutor', 'ExecuteAsync')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Processing\ServiceBusWorkItemMessage.cs' -Patterns @('ServiceBusWorkItemMessage')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Processing\PlaceholderServiceBusWorkItemExecutor.cs' -Patterns @('PlaceholderServiceBusWorkItemExecutor', 'ExecuteAsync')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Options\SqlServiceBusExecutorOptions.cs' -Patterns @('SectionName', 'ServiceBusConnectionString', 'QueueName')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Core\MigrationBase.Core\Cloud\Azure\Hosting\AzureHostRoleDefaults.cs' -Patterns @('ServiceBusExecutor', 'ServiceBusDispatcher')

Add-SearchSection -Lines $lines -RootPath $root -Title 'Service Bus worker references' -Pattern 'ServiceBus'
Add-SearchSection -Lines $lines -RootPath $root -Title 'Service Bus executor option references' -Pattern 'SqlServiceBusExecutor'
Add-SearchSection -Lines $lines -RootPath $root -Title 'Service Bus dispatcher option references' -Pattern 'SqlServiceBusDispatcher'
Add-SearchSection -Lines $lines -RootPath $root -Title 'Service Bus message contract references' -Pattern 'ServiceBusWorkItemMessage'
Add-SearchSection -Lines $lines -RootPath $root -Title 'Queue settlement / dead-letter references' -Pattern 'DeadLetter'

$lines.Add('## P8.2A recommendation') | Out-Null
$lines.Add('') | Out-Null
$lines.Add('- Keep SQL operational tables as the durable source of truth.') | Out-Null
$lines.Add('- Use Service Bus as a distributed transport/fanout layer, not as the operational state store.') | Out-Null
$lines.Add('- Next implementation should harden dispatcher/executor message contracts, settlement behavior, retry behavior, and poison-message handling.') | Out-Null
$lines.Add('') | Out-Null

Set-Content -LiteralPath $outputPath -Value $lines -Encoding UTF8
Write-Host "Wrote $outputPath"
