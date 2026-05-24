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

function Assert-PathExists {
    param([string]$RootPath, [string]$RelativePath)
    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) { throw "Required path missing: $RelativePath" }
}

function Assert-NoPathExists {
    param([string]$RootPath, [string]$RelativePath)
    $path = Join-Path $RootPath $RelativePath
    if (Test-Path -LiteralPath $path) { throw "Invalid path should not exist: $RelativePath" }
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

Assert-NoPathExists -RootPath $root -RelativePath 'src\Migration.Infrastructure'
Assert-NoPathExists -RootPath $root -RelativePath 'src\Migration.Worker'

Assert-PathExists -RootPath $root -RelativePath 'docs\p8\P8.1H-Container-Deployment-Topology.md'
Assert-PathExists -RootPath $root -RelativePath 'deploy\docker\Dockerfile.admin-api.template'
Assert-PathExists -RootPath $root -RelativePath 'deploy\docker\Dockerfile.sql-operational-worker.template'
Assert-PathExists -RootPath $root -RelativePath 'deploy\docker\Dockerfile.servicebus-dispatcher.template'
Assert-PathExists -RootPath $root -RelativePath 'deploy\docker\Dockerfile.servicebus-executor.template'
Assert-PathExists -RootPath $root -RelativePath 'deploy\azure\container-apps\p8.1h-container-apps-settings.template.json'

Assert-FileContains -RootPath $root -RelativePath 'deploy\docker\Dockerfile.admin-api.template' -Text 'Migration.Admin.Api.csproj'
Assert-FileContains -RootPath $root -RelativePath 'deploy\docker\Dockerfile.sql-operational-worker.template' -Text 'Migration.Hosts.SqlOperationalWorker.csproj'
Assert-FileContains -RootPath $root -RelativePath 'deploy\azure\container-apps\p8.1h-container-apps-settings.template.json' -Text 'MIGRATION_SqlOperationalQueueExecutor__RunUntilIdleAndStop'
Assert-FileContains -RootPath $root -RelativePath 'deploy\azure\container-apps\p8.1h-container-apps-settings.template.json' -Text 'MIGRATION_SqlOperationalQueueExecutor__RunId'
Assert-FileContains -RootPath $root -RelativePath 'docs\p8\P8.1H-Container-Deployment-Topology.md' -Text '/health/ready'

Write-Host 'P8.1H container deployment topology validation passed.'
