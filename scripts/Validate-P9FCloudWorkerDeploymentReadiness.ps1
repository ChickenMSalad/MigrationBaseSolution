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

Assert-PathExists -RootPath $root -RelativePath 'docs\p9\P9F-Cloud-Worker-Deployment-Readiness.md'
Assert-PathExists -RootPath $root -RelativePath 'config\templates\p9f-cloud-worker-deployment-settings.template.json'

Assert-FileContains -RootPath $root -RelativePath 'docs\p9\P9F-Cloud-Worker-Deployment-Readiness.md' -Text 'SQL Operational Worker'
Assert-FileContains -RootPath $root -RelativePath 'docs\p9\P9F-Cloud-Worker-Deployment-Readiness.md' -Text 'Service Bus Dispatcher'
Assert-FileContains -RootPath $root -RelativePath 'docs\p9\P9F-Cloud-Worker-Deployment-Readiness.md' -Text 'Service Bus Executor'
Assert-FileContains -RootPath $root -RelativePath 'docs\p9\P9F-Cloud-Worker-Deployment-Readiness.md' -Text 'Do not configure a production RunId override'

Assert-FileContains -RootPath $root -RelativePath 'config\templates\p9f-cloud-worker-deployment-settings.template.json' -Text 'MigrationOperationalStore'
Assert-FileContains -RootPath $root -RelativePath 'config\templates\p9f-cloud-worker-deployment-settings.template.json' -Text 'OpenTelemetry'
Assert-FileContains -RootPath $root -RelativePath 'config\templates\p9f-cloud-worker-deployment-settings.template.json' -Text 'SqlOperationalWorker'
Assert-FileContains -RootPath $root -RelativePath 'config\templates\p9f-cloud-worker-deployment-settings.template.json' -Text 'ServiceBusDispatcher'
Assert-FileContains -RootPath $root -RelativePath 'config\templates\p9f-cloud-worker-deployment-settings.template.json' -Text 'ServiceBusExecutor'

Assert-PathExists -RootPath $root -RelativePath 'src\Hosts\Migration.Hosts.SqlOperationalWorker\Program.cs'
Assert-PathExists -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusDispatcher\Program.cs'
Assert-PathExists -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Program.cs'

Assert-FileContains -RootPath $root -RelativePath 'src\Hosts\Migration.Hosts.SqlOperationalWorker\Program.cs' -Text 'AddOperationalOpenTelemetry'
Assert-FileContains -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusDispatcher\Program.cs' -Text 'AddOperationalOpenTelemetry'
Assert-FileContains -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Program.cs' -Text 'AddOperationalOpenTelemetry'
Assert-FileContains -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusDispatcher\Program.cs' -Text 'AddHostedService'
Assert-FileContains -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Program.cs' -Text 'AddHostedService'

Write-Host 'P9F cloud worker deployment readiness validation passed.'
