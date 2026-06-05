Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    if ($PSScriptRoot) { return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path }
    if ($MyInvocation -and $MyInvocation.MyCommand -and $MyInvocation.MyCommand.Path) {
        return (Resolve-Path (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) '..')).Path
    }
    return (Get-Location).Path
}

function Add-Line {
    param([string]$Text)
    [void]$script:Lines.Add($Text)
}

function Add-FileSummary {
    param([string]$RootPath, [string]$RelativePath, [string[]]$Patterns)
    Add-Line ""
    Add-Line "## $RelativePath"
    Add-Line ""
    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        Add-Line 'Missing.'
        return
    }
    Add-Line 'Present.'
    $content = Get-Content -LiteralPath $path -Raw
    foreach ($pattern in $Patterns) {
        if ($null -ne $content -and $content.Contains($pattern)) {
            Add-Line ("- Contains: {0}" -f $pattern)
        }
        else {
            Add-Line ("- Missing: {0}" -f $pattern)
        }
    }
}

$root = Get-RepositoryRoot
$script:Lines = New-Object System.Collections.Generic.List[string]

Add-Line '# P9E Service Bus Topology Validation Inventory'
Add-Line ''
Add-Line ("GeneratedUtc: {0}" -f ([DateTimeOffset]::UtcNow.ToString('o')))
Add-Line ''
Add-Line 'This inventory verifies repository-side Service Bus topology validation surfaces before cloud queue proof execution.'

Add-FileSummary -RootPath $root -RelativePath 'docs\p9\P9E-Service-Bus-Topology-Validation.md' -Patterns @(
    'Service Bus dispatcher',
    'Service Bus executor',
    'ServiceBusDispatch',
    'ServiceBusWorkItemExecution',
    'Do not invent a new setting name'
)

Add-FileSummary -RootPath $root -RelativePath 'config\templates\p9e-service-bus-topology-settings.template.json' -Patterns @(
    'MigrationOperationalStore',
    'ServiceBusDispatcher',
    'ServiceBusExecutor',
    'OpenTelemetry',
    'AzureMonitorConnectionString'
)

Add-FileSummary -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusDispatcher\Program.cs' -Patterns @(
    'AddOperationalOpenTelemetry',
    'AddHostedService'
)

Add-FileSummary -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Program.cs' -Patterns @(
    'AddOperationalOpenTelemetry',
    'AddHostedService'
)

$outDir = Join-Path $root 'docs\p9'
if (-not (Test-Path -LiteralPath $outDir)) { New-Item -ItemType Directory -Path $outDir | Out-Null }
$out = Join-Path $outDir 'P9E-Service-Bus-Topology-Validation-Inventory.generated.md'
$script:Lines | Set-Content -LiteralPath $out -Encoding UTF8
Write-Host "Wrote $out"
