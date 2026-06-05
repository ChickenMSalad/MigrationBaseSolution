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
    param([System.Collections.Generic.List[string]]$Lines, [string]$RootPath, [string]$RelativePath, [string[]]$Patterns)
    $path = Join-Path $RootPath $RelativePath
    $Lines.Add("## $RelativePath")
    $Lines.Add('')
    if (-not (Test-Path -LiteralPath $path)) {
        $Lines.Add('Missing.')
        $Lines.Add('')
        return
    }
    $content = Get-Content -LiteralPath $path -Raw
    $Lines.Add('Present.')
    foreach ($pattern in $Patterns) {
        if ($null -ne $content -and $content.Contains($pattern)) { $Lines.Add(("- Contains: ``{0}``" -f $pattern)) | Out-Null }
        else { $Lines.Add(("- Missing: ``{0}``" -f $pattern)) | Out-Null }
    }
    $Lines.Add('')
}

function Add-SearchSection {
    param(
        [string]$Title,
        [string]$SearchRoot,
        [string]$Pattern
    )

    $Lines.Add("")
    $Lines.Add("## $Title")
    $Lines.Add("")

    $searchRoot = Join-Path $root $SearchRoot

    if (-not (Test-Path -LiteralPath $searchRoot)) {
        $Lines.Add("Missing search root: $SearchRoot")
        return
    }

    $matches = New-Object System.Collections.Generic.List[string]

    $files = Get-ChildItem -Path $searchRoot -Filter '*.cs' -File -Recurse |
        Where-Object { -not (Test-IsIgnoredPath $_.FullName) }

    foreach ($file in $files) {
        $relative = $file.FullName.Substring($root.Length + 1)

        $contentLines = Get-Content -LiteralPath $file.FullName

        for ($i = 0; $i -lt $contentLines.Length; $i++) {
            $line = $contentLines[$i]

            if ($line -like "*$Pattern*") {
                $lineNumber = $i + 1
                $matches.Add(("- {0}:{1}: {2}" -f $relative, $lineNumber, $line.Trim())) | Out-Null
            }
        }
    }

    if ($matches.Count -eq 0) {
        $Lines.Add("No matches found.")
    }
    else {
        foreach ($match in $matches) {
            $Lines.Add($match)
        }
    }
}

$root = Get-RepositoryRoot
$out = Join-Path $root 'docs\p8\P8.3A-OpenTelemetry-Azure-Monitor-Inventory.generated.md'
$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('# P8.3A OpenTelemetry / Azure Monitor Inventory')
$lines.Add('')
$lines.Add("GeneratedUtc: $([DateTimeOffset]::UtcNow.ToString('o'))")
$lines.Add('')
$lines.Add('This inventory captures repo-native telemetry, correlation, Azure observability, and distributed execution surfaces before adding OpenTelemetry or Azure Monitor runtime wiring.')
$lines.Add('')

Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalExecutionTelemetryFields.cs' -Patterns @('RunId', 'WorkItemId', 'ServiceBusCorrelationId', 'ExecutionDurationMs')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Core\Migration.Application\Operational\Telemetry\OperationalExecutionTelemetryScope.cs' -Patterns @('OperationalExecutionTelemetryScope', 'ServiceBusCorrelationId')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Services\SqlOperationalWorkItemWorker.cs' -Patterns @('BeginScope', 'OperationalExecutionTelemetryFields', 'IOperationalExecutionHistoryWriter')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Runtime\SqlServiceBusExecutorWorker.cs' -Patterns @('BeginScope', 'ServiceBusCorrelationId', 'CompleteMessageAsync', 'DeadLetterMessageAsync', 'AbandonMessageAsync')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Core\MigrationBase.Core\Cloud\Azure\Observability\AzureCorrelationContext.cs' -Patterns @('correlation.id', 'ParentCorrelationId')
Add-FileSummary -Lines $lines -RootPath $root -RelativePath 'src\Core\MigrationBase.Core\Cloud\Azure\Observability\AzureTelemetryDimensionNames.cs' -Patterns @('CorrelationId', 'ParentCorrelationId')

Add-SearchSection -Lines $lines -RootPath $root -Title 'OpenTelemetry package/reference surfaces' -Pattern 'OpenTelemetry' -UnderRelativePath 'src'
Add-SearchSection -Lines $lines -RootPath $root -Title 'Application Insights package/reference surfaces' -Pattern 'ApplicationInsights' -UnderRelativePath 'src'
Add-SearchSection -Lines $lines -RootPath $root -Title 'Azure Monitor package/reference surfaces' -Pattern 'Azure.Monitor' -UnderRelativePath 'src'
Add-SearchSection -Lines $lines -RootPath $root -Title 'ActivitySource references' -Pattern 'ActivitySource' -UnderRelativePath 'src'
Add-SearchSection -Lines $lines -RootPath $root -Title 'BeginScope references' -Pattern 'BeginScope' -UnderRelativePath 'src'
Add-SearchSection -Lines $lines -RootPath $root -Title 'CorrelationId references' -Pattern 'CorrelationId' -UnderRelativePath 'src'

$directory = Split-Path -Parent $out
if (-not (Test-Path -LiteralPath $directory)) { New-Item -ItemType Directory -Path $directory | Out-Null }
Set-Content -LiteralPath $out -Value $lines -Encoding UTF8
Write-Host "Wrote $out"
