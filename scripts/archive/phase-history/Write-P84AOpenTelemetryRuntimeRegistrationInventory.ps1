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
    return ($normalized.Contains('\bin\') -or $normalized.Contains('\obj\'))
}

function Add-FileSummary {
    param([System.Collections.Generic.List[string]] $Lines, [string] $RootPath, [string] $RelativePath, [string[]] $Patterns)
    $Lines.Add('') | Out-Null
    $Lines.Add(('## {0}' -f $RelativePath)) | Out-Null
    $Lines.Add('') | Out-Null
    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) { $Lines.Add('Missing.') | Out-Null; return }
    $Lines.Add('Present.') | Out-Null
    $content = Get-Content -LiteralPath $path -Raw
    foreach ($pattern in $Patterns) {
        if ($null -ne $content -and $content.Contains($pattern)) { $Lines.Add(('- Contains: `{0}`' -f $pattern)) | Out-Null }
        else { $Lines.Add(('- Missing: `{0}`' -f $pattern)) | Out-Null }
    }
}

function Add-SearchSection {
    param([System.Collections.Generic.List[string]] $Lines, [string] $RootPath, [string] $Title, [string] $UnderRelativePath, [string] $Pattern)
    $Lines.Add('') | Out-Null
    $Lines.Add(('## {0}' -f $Title)) | Out-Null
    $Lines.Add('') | Out-Null
    $searchRoot = Join-Path $RootPath $UnderRelativePath
    if (-not (Test-Path -LiteralPath $searchRoot)) { $Lines.Add(('Missing search root: {0}' -f $UnderRelativePath)) | Out-Null; return }
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
    if ($matches.Count -eq 0) { $Lines.Add('No matches found.') | Out-Null; return }
    foreach ($match in $matches) { $Lines.Add($match) | Out-Null }
}

$root = Get-RepositoryRoot
$out = Join-Path $root 'docs\p8\P8.4A-OpenTelemetry-Runtime-Registration-Inventory.generated.md'
$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('# P8.4A OpenTelemetry Runtime Registration Inventory') | Out-Null
$lines.Add('') | Out-Null
$lines.Add(('GeneratedUtc: {0}' -f [DateTimeOffset]::UtcNow.ToString('O'))) | Out-Null
$lines.Add('') | Out-Null
$lines.Add('This inventory captures runtime observability readiness before adding OpenTelemetry/Azure Monitor registration code or package references.') | Out-Null

Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalExecutionActivitySources.cs' -Patterns @('Migration.Operational.Execution', 'ActivitySource')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalExecutionActivity.cs' -Patterns @('StartSqlQueueWorkItemExecution', 'StartServiceBusWorkItemExecution', 'StartServiceBusDispatch', 'SetExecutionDuration', 'SetExecutionResult')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Services\SqlOperationalWorkItemWorker.cs' -Patterns @('StartSqlQueueWorkItemExecution', 'BeginScope')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Runtime\SqlServiceBusExecutorWorker.cs' -Patterns @('StartServiceBusWorkItemExecution', 'BeginScope')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusDispatcher\Dispatching\SqlWorkItemDispatcher.cs' -Patterns @('StartServiceBusDispatch', 'SendMessageAsync')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'config\templates\p8.4a-opentelemetry-runtime-registration.template.json' -Patterns @('OpenTelemetry', 'AzureMonitor', 'ApplicationInsights')
Add-SearchSection -Lines $lines -RootPath $root -Title 'OpenTelemetry references' -UnderRelativePath 'src' -Pattern 'OpenTelemetry'
Add-SearchSection -Lines $lines -RootPath $root -Title 'Azure Monitor references' -UnderRelativePath 'src' -Pattern 'AzureMonitor'
Add-SearchSection -Lines $lines -RootPath $root -Title 'Application Insights references' -UnderRelativePath 'src' -Pattern 'ApplicationInsights'
Add-SearchSection -Lines $lines -RootPath $root -Title 'MIGRATION_ environment provider references' -UnderRelativePath 'src' -Pattern 'AddEnvironmentVariables(prefix: "MIGRATION_")'
Add-SearchSection -Lines $lines -RootPath $root -Title 'ActivitySource references' -UnderRelativePath 'src' -Pattern 'ActivitySource'

$directory = Split-Path -Parent $out
if (-not (Test-Path -LiteralPath $directory)) { New-Item -ItemType Directory -Path $directory -Force | Out-Null }
Set-Content -LiteralPath $out -Value $lines -Encoding UTF8
Write-Host "Wrote $out"
