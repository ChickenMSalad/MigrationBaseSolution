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

$root = Get-RepositoryRoot
Write-Host "Repository root: $root"

Assert-PathExists -RootPath $root -RelativePath 'docs\p9\P9E-Service-Bus-Topology-Validation.md'
Assert-PathExists -RootPath $root -RelativePath 'config\templates\p9e-service-bus-topology-settings.template.json'

Assert-FileContains -RootPath $root -RelativePath 'docs\p9\P9E-Service-Bus-Topology-Validation.md' -Text 'Service Bus dispatcher'
Assert-FileContains -RootPath $root -RelativePath 'docs\p9\P9E-Service-Bus-Topology-Validation.md' -Text 'Service Bus executor'
Assert-FileContains -RootPath $root -RelativePath 'docs\p9\P9E-Service-Bus-Topology-Validation.md' -Text 'Do not invent a new setting name'
Assert-FileContains -RootPath $root -RelativePath 'config\templates\p9e-service-bus-topology-settings.template.json' -Text 'ServiceBusDispatcher'
Assert-FileContains -RootPath $root -RelativePath 'config\templates\p9e-service-bus-topology-settings.template.json' -Text 'ServiceBusExecutor'
Assert-FileContains -RootPath $root -RelativePath 'config\templates\p9e-service-bus-topology-settings.template.json' -Text 'OpenTelemetry'

$dispatcherProgram = 'src\Workers\Migration.Workers.ServiceBusDispatcher\Program.cs'
$executorProgram = 'src\Workers\Migration.Workers.ServiceBusExecutor\Program.cs'
if (Test-Path -LiteralPath (Join-Path $root $dispatcherProgram)) {
    Assert-FileContains -RootPath $root -RelativePath $dispatcherProgram -Text 'AddOperationalOpenTelemetry'
}
if (Test-Path -LiteralPath (Join-Path $root $executorProgram)) {
    Assert-FileContains -RootPath $root -RelativePath $executorProgram -Text 'AddOperationalOpenTelemetry'
}

Write-Host 'P9E Service Bus topology validation passed.'
