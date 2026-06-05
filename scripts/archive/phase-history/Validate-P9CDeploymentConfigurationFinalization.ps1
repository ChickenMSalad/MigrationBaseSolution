Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    if ($PSScriptRoot) { return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path }
    if ($MyInvocation -and $MyInvocation.MyCommand -and $MyInvocation.MyCommand.Path) {
        return (Resolve-Path (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) '..')).Path
    }
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

function Assert-FileDoesNotContain {
    param([string]$RootPath, [string]$RelativePath, [string]$Text)
    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) { return }
    $content = Get-Content -LiteralPath $path -Raw
    if ($null -ne $content -and $content.Contains($Text)) {
        throw "Unexpected text found in $RelativePath : $Text"
    }
}

$root = Get-RepositoryRoot
Write-Host "Repository root: $root"

Assert-PathExists -RootPath $root -RelativePath 'docs\p9\P9C-Deployment-Configuration-Finalization.md'
Assert-PathExists -RootPath $root -RelativePath 'config\templates\p9c-deployment-configuration.template.json'

Assert-FileContains -RootPath $root -RelativePath 'docs\p9\P9C-Deployment-Configuration-Finalization.md' -Text 'ConnectionStrings:MigrationOperationalStore'
Assert-FileContains -RootPath $root -RelativePath 'docs\p9\P9C-Deployment-Configuration-Finalization.md' -Text 'MIGRATION_OpenTelemetry__EnableTracing'
Assert-FileContains -RootPath $root -RelativePath 'docs\p9\P9C-Deployment-Configuration-Finalization.md' -Text 'Do not configure a production RunId override'

Assert-FileContains -RootPath $root -RelativePath 'config\templates\p9c-deployment-configuration.template.json' -Text 'MigrationOperationalStore'
Assert-FileContains -RootPath $root -RelativePath 'config\templates\p9c-deployment-configuration.template.json' -Text 'OpenTelemetry'
Assert-FileContains -RootPath $root -RelativePath 'config\templates\p9c-deployment-configuration.template.json' -Text 'EnableAzureMonitorExporter'

Assert-FileContains -RootPath $root -RelativePath 'src\Hosts\Migration.Hosts.SqlOperationalWorker\Program.cs' -Text 'AddEnvironmentVariables(prefix: "MIGRATION_")'
Assert-FileContains -RootPath $root -RelativePath 'src\Hosts\Migration.Hosts.SqlOperationalWorker\Program.cs' -Text 'AddOperationalOpenTelemetry'

Assert-FileContains -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusDispatcher\Program.cs' -Text 'AddOperationalOpenTelemetry'
Assert-FileContains -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Program.cs' -Text 'AddOperationalOpenTelemetry'

Assert-FileDoesNotContain -RootPath $root -RelativePath 'config\templates\p9c-deployment-configuration.template.json' -Text 'RunId'

Write-Host 'P9C deployment configuration finalization validation passed.'
