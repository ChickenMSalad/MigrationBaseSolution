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
            $Lines.Add(('- Contains: ``{0}``' -f $pattern)) | Out-Null
        }
        else {
            $Lines.Add(('- Missing: ``{0}``' -f $pattern)) | Out-Null
        }
    }
}

function Add-SearchSection {
    param(
        [System.Collections.Generic.List[string]]$Lines,
        [string]$RootPath,
        [string]$Title,
        [string]$SearchRoot,
        [string]$Pattern
    )

    $Lines.Add('') | Out-Null
    $Lines.Add(('## {0}' -f $Title)) | Out-Null
    $Lines.Add('') | Out-Null

    $searchRootPath = Join-Path $RootPath $SearchRoot
    if (-not (Test-Path -LiteralPath $searchRootPath)) {
        $Lines.Add(('Missing search root: {0}' -f $SearchRoot)) | Out-Null
        return
    }

    $matches = New-Object System.Collections.Generic.List[string]
    $files = Get-ChildItem -Path $searchRootPath -Filter '*.cs' -File -Recurse | Where-Object { -not (Test-IsIgnoredPath $_.FullName) }
    foreach ($file in $files) {
        $relative = $file.FullName.Substring($RootPath.Length + 1)
        $contentLines = Get-Content -LiteralPath $file.FullName
        for ($i = 0; $i -lt $contentLines.Length; $i++) {
            $line = $contentLines[$i]
            if ($line -like "*$Pattern*") {
                $lineNumber = $i + 1
                $matches.Add(('- {0}:{1}: {2}' -f $relative, $lineNumber, $line.Trim())) | Out-Null
            }
        }
    }

    if ($matches.Count -eq 0) {
        $Lines.Add('No matches found.') | Out-Null
    }
    else {
        foreach ($match in $matches) { $Lines.Add($match) | Out-Null }
    }
}

$root = Get-RepositoryRoot
$outDir = Join-Path $root 'docs\p8'
if (-not (Test-Path -LiteralPath $outDir)) { New-Item -ItemType Directory -Path $outDir | Out-Null }
$out = Join-Path $outDir 'P8.3D-OpenTelemetry-Runtime-Activity-Inventory.generated.md'

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('# P8.3D OpenTelemetry Runtime Activity Inventory') | Out-Null
$lines.Add('') | Out-Null
$lines.Add(('GeneratedUtc: {0:o}' -f [DateTimeOffset]::UtcNow)) | Out-Null
$lines.Add('') | Out-Null
$lines.Add('This inventory captures runtime Activity usage in SQL queue execution, Service Bus dispatch, and Service Bus execution.') | Out-Null

Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalExecutionActivity.cs' -Patterns @('StartSqlQueueWorkItemExecution','StartServiceBusWorkItemExecution','StartServiceBusDispatch','SetExecutionDuration','SetExecutionResult')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Services\SqlOperationalWorkItemWorker.cs' -Patterns @('OperationalExecutionActivity.StartSqlQueueWorkItemExecution','OperationalExecutionActivity.SetExecutionDuration','OperationalExecutionActivity.SetExecutionResult','BeginScope')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Runtime\SqlServiceBusExecutorWorker.cs' -Patterns @('OperationalExecutionActivity.StartServiceBusWorkItemExecution','OperationalExecutionActivity.SetExecutionDuration','OperationalExecutionActivity.SetExecutionResult','BeginScope','CompleteMessageAsync','DeadLetterMessageAsync')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusDispatcher\Dispatching\SqlWorkItemDispatcher.cs' -Patterns @('OperationalExecutionActivity.StartServiceBusDispatch','OperationalExecutionActivity.SetExecutionResult','ServiceBusMessage','SendMessageAsync')

Add-SearchSection -Lines $lines -RootPath $root -Title 'Activity usage references' -SearchRoot 'src' -Pattern 'OperationalExecutionActivity.'
Add-SearchSection -Lines $lines -RootPath $root -Title 'ActivitySource references' -SearchRoot 'src' -Pattern 'ActivitySource'

Set-Content -LiteralPath $out -Value $lines -Encoding UTF8
Write-Host "Wrote $out"
