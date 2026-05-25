Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    if ($PSScriptRoot) { return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path }
    return (Get-Location).Path
}

function Assert-PathExists {
    param([string]$RootPath, [string]$RelativePath)
    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) { throw "Required path missing: $RelativePath" }
}

function Assert-FileContains {
    param([string]$RootPath, [string]$RelativePath, [string]$Text)
    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) { throw "Required file missing: $RelativePath" }
    $content = Get-Content -LiteralPath $path -Raw
    if ($null -eq $content -or -not $content.Contains($Text)) {
        throw "Required text missing from $RelativePath : $Text"
    }
}

$root = Get-RepositoryRoot
Write-Host "Repository root: $root"

Assert-PathExists -RootPath $root -RelativePath 'docs\p9\P9I-Azure-Monitor-Trace-Inspection.md'
Assert-PathExists -RootPath $root -RelativePath 'config\templates\p9i-azure-monitor-trace-inspection-settings.template.json'
Assert-PathExists -RootPath $root -RelativePath 'scripts\kql\P9I-OperationalTraceInspection.kql'

Assert-FileContains -RootPath $root -RelativePath 'docs\p9\P9I-Azure-Monitor-Trace-Inspection.md' -Text 'Proof order'
Assert-FileContains -RootPath $root -RelativePath 'docs\p9\P9I-Azure-Monitor-Trace-Inspection.md' -Text 'Migration.Operational.Execution'
Assert-FileContains -RootPath $root -RelativePath 'scripts\kql\P9I-OperationalTraceInspection.kql' -Text 'SqlQueueWorkItemExecution'
Assert-FileContains -RootPath $root -RelativePath 'scripts\kql\P9I-OperationalTraceInspection.kql' -Text 'ServiceBusDispatch'
Assert-FileContains -RootPath $root -RelativePath 'scripts\kql\P9I-OperationalTraceInspection.kql' -Text 'ServiceBusWorkItemExecution'
Assert-FileContains -RootPath $root -RelativePath 'config\templates\p9i-azure-monitor-trace-inspection-settings.template.json' -Text 'AzureMonitorConnectionString'

Write-Host 'P9I Azure Monitor trace inspection validation passed.'
