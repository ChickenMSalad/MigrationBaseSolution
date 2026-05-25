Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    if ($PSScriptRoot) {
        return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
    }
    if ($MyInvocation -and $MyInvocation.MyCommand -and $MyInvocation.MyCommand.Path) {
        return (Resolve-Path (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) '..')).Path
    }
    return (Get-Location).Path
}

function Assert-PathExists {
    param(
        [string]$RootPath,
        [string]$RelativePath
    )

    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required path missing: $RelativePath"
    }
}

function Assert-FileContains {
    param(
        [string]$RootPath,
        [string]$RelativePath,
        [string[]]$Texts
    )

    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required file missing: $RelativePath"
    }

    $content = Get-Content -LiteralPath $path -Raw
    foreach ($text in $Texts) {
        if ($null -eq $content -or -not $content.Contains($text)) {
            throw "Required text missing from $RelativePath : $text"
        }
    }
}

function Assert-NoPathExists {
    param(
        [string]$RootPath,
        [string]$RelativePath
    )

    $path = Join-Path $RootPath $RelativePath
    if (Test-Path -LiteralPath $path) {
        throw "Invalid path should not exist: $RelativePath"
    }
}

$root = Get-RepositoryRoot
Write-Host "Repository root: $root"

Assert-NoPathExists -RootPath $root -RelativePath 'src\Migration.Infrastructure'
Assert-NoPathExists -RootPath $root -RelativePath 'src\Migration.Worker'

Assert-PathExists -RootPath $root -RelativePath 'docs\p9\P9D-Sql-Operational-Store-Cloud-Validation.md'
Assert-PathExists -RootPath $root -RelativePath 'scripts\sql\P9D-InspectOperationalStore.sql'
Assert-PathExists -RootPath $root -RelativePath 'config\templates\p9d-sql-operational-store-cloud-settings.template.json'

Assert-FileContains -RootPath $root -RelativePath 'docs\p9\P9D-Sql-Operational-Store-Cloud-Validation.md' -Texts @(
    'ConnectionStrings:MigrationOperationalStore',
    'MIGRATION_ConnectionStrings__MigrationOperationalStore',
    'RunId is uniqueidentifier / Guid',
    'WorkItemId is bigint / long'
)

Assert-FileContains -RootPath $root -RelativePath 'scripts\sql\P9D-InspectOperationalStore.sql' -Texts @(
    'sys.tables',
    'sys.columns',
    'sys.foreign_keys',
    'sys.indexes',
    'ApproximateRows'
)

Assert-FileContains -RootPath $root -RelativePath 'config\templates\p9d-sql-operational-store-cloud-settings.template.json' -Texts @(
    'MigrationOperationalStore',
    'OpenTelemetry',
    'SqlOperationalWorker',
    'SqlOperationalQueueExecutor'
)

Assert-PathExists -RootPath $root -RelativePath 'src\Core\Migration.Infrastructure.Sql'
Assert-PathExists -RootPath $root -RelativePath 'src\Hosts\Migration.Hosts.SqlOperationalWorker'
Assert-PathExists -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusDispatcher'
Assert-PathExists -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor'

Write-Host 'P9D SQL operational store cloud validation passed.'
