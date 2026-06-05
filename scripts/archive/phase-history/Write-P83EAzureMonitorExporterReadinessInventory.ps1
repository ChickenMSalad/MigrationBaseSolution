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
    param([string] $Path)
    if ([string]::IsNullOrWhiteSpace($Path)) { return $false }
    $normalized = $Path.Replace('/', '\').ToLowerInvariant()
    return ($normalized.Contains('\bin\') -or $normalized.Contains('\obj\') -or $normalized.Contains('\payload\'))
}

function Add-FileSummary {
    param([System.Collections.Generic.List[string]] $Lines, [string] $RootPath, [string] $RelativePath, [string[]] $Patterns)
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

function Add-SearchSection {
    param([System.Collections.Generic.List[string]] $Lines, [string] $RootPath, [string] $Title, [string] $SearchRootRelativePath, [string] $Pattern)
    $Lines.Add('') | Out-Null
    $Lines.Add(('## {0}' -f $Title)) | Out-Null
    $Lines.Add('') | Out-Null
    $searchRoot = Join-Path $RootPath $SearchRootRelativePath
    if (-not (Test-Path -LiteralPath $searchRoot)) {
        $Lines.Add(('Missing search root: {0}' -f $SearchRootRelativePath)) | Out-Null
        return
    }
    $matches = New-Object System.Collections.Generic.List[string]
    $files = Get-ChildItem -Path $searchRoot -Filter '*.cs' -File -Recurse | Where-Object { -not (Test-IsIgnoredPath $_.FullName) }
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
        return
    }
    foreach ($match in $matches) {
        $Lines.Add($match) | Out-Null
    }
}

$root = Get-RepositoryRoot
$outDir = Join-Path $root 'docs\p8'
if (-not (Test-Path -LiteralPath $outDir)) { New-Item -ItemType Directory -Path $outDir | Out-Null }
$out = Join-Path $outDir 'P8.3E-Azure-Monitor-Exporter-Readiness-Inventory.generated.md'

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('# P8.3E Azure Monitor Exporter Readiness Inventory') | Out-Null
$lines.Add('') | Out-Null
$lines.Add(('GeneratedUtc: {0:o}' -f [DateTimeOffset]::UtcNow)) | Out-Null
$lines.Add('') | Out-Null
$lines.Add('This inventory captures observability/exporter readiness before adding OpenTelemetry or Azure Monitor runtime registration.') | Out-Null

Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalExecutionActivitySources.cs' -Patterns @('ActivitySource', 'Migration.Operational.Execution')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalExecutionActivity.cs' -Patterns @('StartSqlQueueWorkItemExecution', 'StartServiceBusWorkItemExecution', 'StartServiceBusDispatch', 'SetExecutionDuration', 'SetExecutionResult')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Services\SqlOperationalWorkItemWorker.cs' -Patterns @('OperationalExecutionActivity', 'StartSqlQueueWorkItemExecution', 'BeginScope')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Runtime\SqlServiceBusExecutorWorker.cs' -Patterns @('OperationalExecutionActivity', 'StartServiceBusWorkItemExecution', 'BeginScope')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusDispatcher\Dispatching\SqlWorkItemDispatcher.cs' -Patterns @('OperationalExecutionActivity', 'StartServiceBusDispatch', 'SendMessageAsync')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'config\templates\p8.3e-observability-settings.template.json' -Patterns @('OpenTelemetry', 'AzureMonitor', 'ApplicationInsights')

Add-SearchSection -Lines $lines -RootPath $root -Title 'Application Insights references' -SearchRootRelativePath 'src' -Pattern 'ApplicationInsights'
Add-SearchSection -Lines $lines -RootPath $root -Title 'OpenTelemetry references' -SearchRootRelativePath 'src' -Pattern 'OpenTelemetry'
Add-SearchSection -Lines $lines -RootPath $root -Title 'Azure Monitor references' -SearchRootRelativePath 'src' -Pattern 'AzureMonitor'
Add-SearchSection -Lines $lines -RootPath $root -Title 'ActivitySource references' -SearchRootRelativePath 'src' -Pattern 'ActivitySource'

Set-Content -LiteralPath $out -Value $lines -Encoding UTF8
Write-Host "Wrote $out"
