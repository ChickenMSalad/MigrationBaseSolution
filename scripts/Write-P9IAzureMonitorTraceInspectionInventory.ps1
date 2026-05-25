Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    if ($PSScriptRoot) { return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path }
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

    Add-Line $Lines "## $RelativePath"
    Add-Line $Lines ""
    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        Add-Line $Lines 'Missing.'
        Add-Line $Lines ''
        return
    }

    Add-Line $Lines 'Present.'
    $content = Get-Content -LiteralPath $path -Raw
    foreach ($pattern in $Patterns) {
        if ($null -ne $content -and $content.Contains($pattern)) {
            Add-Line $Lines (('- Contains: {0}' -f $pattern))
        }
        else {
            Add-Line $Lines (('- Missing: {0}' -f $pattern))
        }
    }
    Add-Line $Lines ''
}

$root = Get-RepositoryRoot
$out = Join-Path $root 'docs\p9\P9I-Azure-Monitor-Trace-Inspection-Inventory.generated.md'
$lines = New-Object System.Collections.Generic.List[string]

Add-Line $lines '# P9I Azure Monitor Trace Inspection Inventory'
Add-Line $lines ''
Add-Line $lines ('GeneratedUtc: {0:o}' -f [DateTimeOffset]::UtcNow)
Add-Line $lines ''
Add-Line $lines 'This inventory verifies Azure Monitor trace inspection readiness for first cloud smoke execution.'
Add-Line $lines ''

Add-FileSummary $lines $root 'docs\p9\P9I-Azure-Monitor-Trace-Inspection.md' @('Proof order', 'Migration.Operational.Execution', 'Success criteria')
Add-FileSummary $lines $root 'config\templates\p9i-azure-monitor-trace-inspection-settings.template.json' @('EnableTracing', 'EnableAzureMonitorExporter', 'AzureMonitorConnectionString')
Add-FileSummary $lines $root 'scripts\kql\P9I-OperationalTraceInspection.kql' @('SqlQueueWorkItemExecution', 'ServiceBusDispatch', 'ServiceBusWorkItemExecution', 'migration.run.id', 'migration.work_item.id')
Add-FileSummary $lines $root 'src\Core\Migration.Application\Operational\Telemetry\OperationalExecutionActivitySources.cs' @('Migration.Operational.Execution', 'SqlQueueWorkItemExecution', 'ServiceBusDispatch', 'ServiceBusWorkItemExecution')

Add-Line $lines '## Step reminder'
Add-Line $lines ''
Add-Line $lines 'Run scripts/kql/P9I-OperationalTraceInspection.kql manually in the Azure Monitor / Application Insights query window after a cloud smoke execution has run.'

$parent = Split-Path -Parent $out
if (-not (Test-Path -LiteralPath $parent)) { New-Item -ItemType Directory -Path $parent | Out-Null }
Set-Content -LiteralPath $out -Value $lines -Encoding UTF8
Write-Host "Wrote $out"
