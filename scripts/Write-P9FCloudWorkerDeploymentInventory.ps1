Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    if ($PSScriptRoot) { return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path }
    return (Get-Location).Path
}

function Add-Line {
    param([string]$Text)
    $script:Lines.Add($Text) | Out-Null
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
            Add-Line (('- Contains: {0}' -f $pattern))
        }
        else {
            Add-Line (('- Missing: {0}' -f $pattern))
        }
    }
}

$root = Get-RepositoryRoot
$out = Join-Path $root 'docs\p9\P9F-Cloud-Worker-Deployment-Inventory.generated.md'
$script:Lines = New-Object System.Collections.Generic.List[string]

Add-Line '# P9F Cloud Worker Deployment Inventory'
Add-Line ''
Add-Line (('GeneratedUtc: {0}' -f [DateTimeOffset]::UtcNow.ToString('o')))
Add-Line ''
Add-Line 'This inventory verifies repository-side worker deployment readiness before deploying cloud worker roles.'

Add-FileSummary -RootPath $root -RelativePath 'docs\p9\P9F-Cloud-Worker-Deployment-Readiness.md' -Patterns @(
    'SQL Operational Worker',
    'Service Bus Dispatcher',
    'Service Bus Executor',
    'Do not configure a production RunId override'
)

Add-FileSummary -RootPath $root -RelativePath 'config\templates\p9f-cloud-worker-deployment-settings.template.json' -Patterns @(
    'MigrationOperationalStore',
    'OpenTelemetry',
    'SqlOperationalWorker',
    'ServiceBusDispatcher',
    'ServiceBusExecutor'
)

Add-FileSummary -RootPath $root -RelativePath 'src\Hosts\Migration.Hosts.SqlOperationalWorker\Program.cs' -Patterns @(
    'AddOperationalOpenTelemetry',
    'AddEnvironmentVariables(prefix: "MIGRATION_")'
)

Add-FileSummary -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusDispatcher\Program.cs' -Patterns @(
    'AddOperationalOpenTelemetry',
    'AddHostedService'
)

Add-FileSummary -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Program.cs' -Patterns @(
    'AddOperationalOpenTelemetry',
    'AddHostedService'
)

Add-Line ''
Add-Line '## Recommended next checks'
Add-Line ''
Add-Line '- Build all worker projects.'
Add-Line '- Deploy workers disabled first if the host settings support enabled flags.'
Add-Line '- Enable dispatcher and executor only after SQL and Service Bus validation are complete.'
Add-Line '- Verify Azure Monitor traces for Migration.Operational.Execution after the first smoke run.'

$directory = Split-Path -Parent $out
if (-not (Test-Path -LiteralPath $directory)) { New-Item -ItemType Directory -Path $directory -Force | Out-Null }
[System.IO.File]::WriteAllLines($out, $script:Lines)
Write-Host "Wrote $out"
