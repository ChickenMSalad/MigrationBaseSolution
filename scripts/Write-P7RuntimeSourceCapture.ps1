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

function Add-Line {
    param(
        [System.Collections.Generic.List[string]]$Lines,
        [string]$Text
    )

    [void]$Lines.Add($Text)
}

function Add-SourceFileSection {
    param(
        [string]$RootPath,
        [System.Collections.Generic.List[string]]$Lines,
        [string]$RelativePath
    )

    $path = Join-Path $RootPath $RelativePath

    Add-Line -Lines $Lines -Text ''
    Add-Line -Lines $Lines -Text ("## {0}" -f $RelativePath)
    Add-Line -Lines $Lines -Text ''

    if (-not (Test-Path -LiteralPath $path)) {
        Add-Line -Lines $Lines -Text '```text'
        Add-Line -Lines $Lines -Text 'FILE MISSING'
        Add-Line -Lines $Lines -Text '```'
        return
    }

    Add-Line -Lines $Lines -Text '```csharp'
    $contentLines = Get-Content -LiteralPath $path
    foreach ($line in $contentLines) {
        Add-Line -Lines $Lines -Text $line
    }
    Add-Line -Lines $Lines -Text '```'
}

function Add-JsonFileSection {
    param(
        [string]$RootPath,
        [System.Collections.Generic.List[string]]$Lines,
        [string]$RelativePath
    )

    $path = Join-Path $RootPath $RelativePath

    Add-Line -Lines $Lines -Text ''
    Add-Line -Lines $Lines -Text ("## {0}" -f $RelativePath)
    Add-Line -Lines $Lines -Text ''

    if (-not (Test-Path -LiteralPath $path)) {
        Add-Line -Lines $Lines -Text '```text'
        Add-Line -Lines $Lines -Text 'FILE MISSING'
        Add-Line -Lines $Lines -Text '```'
        return
    }

    Add-Line -Lines $Lines -Text '```json'
    $contentLines = Get-Content -LiteralPath $path
    foreach ($line in $contentLines) {
        Add-Line -Lines $Lines -Text $line
    }
    Add-Line -Lines $Lines -Text '```'
}

$root = Get-RepositoryRoot
$docsPath = Join-Path $root 'docs\p7'
if (-not (Test-Path -LiteralPath $docsPath)) {
    New-Item -Path $docsPath -ItemType Directory -Force | Out-Null
}

$outputPath = Join-Path $docsPath 'P7.7A-Runtime-Source-Capture.generated.md'
$lines = New-Object 'System.Collections.Generic.List[string]'

Add-Line -Lines $lines -Text '# P7.7A Runtime Source Capture'
Add-Line -Lines $lines -Text ''
Add-Line -Lines $lines -Text ("Generated: {0}" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz'))
Add-Line -Lines $lines -Text ''
Add-Line -Lines $lines -Text 'This file captures exact source surfaces required for repo-native QueueExecutor SQL operational runtime wiring.'
Add-Line -Lines $lines -Text ''
Add-Line -Lines $lines -Text 'It intentionally captures source only from approved repo-native locations.'

$sourceFiles = @(
    'src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj',
    'src\Workers\Migration.Workers.QueueExecutor\Program.cs',
    'src\Workers\Migration.Workers.QueueExecutor\QueueWorkerLoopService.cs',
    'src\Workers\Migration.Workers.QueueExecutor\Services\MigrationRunQueueWorker.cs',
    'src\Workers\Migration.Workers.QueueExecutor\Services\AzureOperationalQueuePublisher.cs',
    'src\Workers\Migration.Workers.QueueExecutor\Services\ProjectCredentialJobSettingsHydrator.cs',
    'src\Workers\Migration.Workers.QueueExecutor\Registration\QueueExecutorServiceCollectionExtensions.cs',
    'src\Workers\Migration.Workers.QueueExecutor\Configuration\QueueExecutorConfigurationExtensions.cs',
    'src\Workers\Migration.Workers.QueueExecutor\Configuration\QueueExecutorHostBuilderExtensions.cs',
    'src\Workers\Migration.Workers.QueueExecutor\QueueExecutorWorkerRegistrationPlan.cs',
    'src\Workers\Migration.Workers.QueueExecutor\Options\QueueExecutorOptions.cs',
    'src\Workers\Migration.Workers.QueueExecutor\Options\OperationalQueuePublisherOptions.cs',
    'src\Core\Migration.Infrastructure.Sql\Migration.Infrastructure.Sql.csproj',
    'src\Core\Migration.Infrastructure.Sql\Connections\ISqlConnectionFactory.cs',
    'src\Core\Migration.Infrastructure.Sql\Connections\SqlConnectionFactory.cs',
    'src\Core\Migration.Infrastructure.Sql\Operational\WorkItems\SqlOperationalWorkItemQueue.cs',
    'src\Core\Migration.Infrastructure.Sql\Operational\WorkItems\SqlOperationalWorkItemQueueOptions.cs',
    'src\Core\Migration.Infrastructure.Sql\Operational\WorkItems\SqlOperationalWorkItemQueueServiceCollectionExtensions.cs',
    'src\Core\Migration.Infrastructure.Sql\Operational\Runs\SqlOperationalRunCoordinator.cs',
    'src\Core\Migration.Infrastructure.Sql\Operational\Runs\SqlOperationalRunCoordinatorOptions.cs',
    'src\Core\Migration.Infrastructure.Sql\Operational\Runs\SqlOperationalRunCoordinatorServiceCollectionExtensions.cs',
    'src\Core\Migration.Infrastructure.Sql\Operational\Leases\SqlOperationalWorkItemLeaseCoordinator.cs',
    'src\Core\Migration.Infrastructure.Sql\Operational\Leases\SqlOperationalWorkItemLeaseCoordinatorOptions.cs',
    'src\Core\Migration.Infrastructure.Sql\Operational\Leases\SqlOperationalWorkItemLeaseCoordinatorServiceCollectionExtensions.cs',
    'src\Core\Migration.Infrastructure.Sql\Operational\Readiness\SqlOperationalRuntimeReadinessService.cs',
    'src\Core\Migration.Infrastructure.Sql\Operational\Readiness\SqlOperationalRuntimeReadinessServiceCollectionExtensions.cs',
    'src\Core\Migration.Infrastructure.Sql\Registration\SqlOperationalStoreServiceCollectionExtensions.cs',
    'src\Core\Migration.Infrastructure.Sql\Stores\ISqlOperationalBackboneStore.cs',
    'src\Core\Migration.Infrastructure.Sql\Stores\SqlOperationalBackboneStore.cs',
    'src\Core\Migration.Infrastructure.Sql\Records\SqlOperationalRecords.cs',
    'src\Core\Migration.Application\Operational\WorkItems\OperationalWorkItemQueueContracts.cs',
    'src\Core\Migration.Application\Operational\Runs\OperationalRunCoordinatorContracts.cs',
    'src\Core\Migration.Application\Operational\Leases\OperationalWorkItemLeaseContracts.cs',
    'src\Core\Migration.Application\Operational\Readiness\OperationalRuntimeReadinessContracts.cs',
    'src\Core\Migration.Orchestration\Abstractions\IMigrationJobRunner.cs',
    'src\Core\Migration.Orchestration\Execution\GenericMigrationJobRunner.cs',
    'src\Core\Migration.Orchestration\Extensions\ServiceCollectionExtensions.cs',
    'src\Core\Migration.Orchestration\Options\MigrationExecutionOptions.cs',
    'src\Core\Migration.GenericRuntime\Registration\MigrationRuntimeServiceCollectionExtensions.cs',
    'src\Core\Migration.GenericRuntime\Registration\GenericMigrationRuntimeServiceCollectionExtensions.cs',
    'src\Hosts\Migration.Hosts.SqlOperationalWorker\Migration.Hosts.SqlOperationalWorker.csproj',
    'src\Hosts\Migration.Hosts.SqlOperationalWorker\Program.cs'
)

foreach ($relativePath in $sourceFiles) {
    if ($relativePath.EndsWith('.json')) {
        Add-JsonFileSection -RootPath $root -Lines $lines -RelativePath $relativePath
    }
    else {
        Add-SourceFileSection -RootPath $root -Lines $lines -RelativePath $relativePath
    }
}

$jsonFiles = @(
    'src\Workers\Migration.Workers.QueueExecutor\appsettings.json',
    'src\Workers\Migration.Workers.QueueExecutor\appsettings.Development.json',
    'src\Workers\Migration.Workers.QueueExecutor\appsettings.Local.json',
    'src\Hosts\Migration.Hosts.SqlOperationalWorker\appsettings.json',
    'src\Hosts\Migration.Hosts.SqlOperationalWorker\appsettings.Development.json'
)

foreach ($relativePath in $jsonFiles) {
    Add-JsonFileSection -RootPath $root -Lines $lines -RelativePath $relativePath
}

Set-Content -LiteralPath $outputPath -Value $lines -Encoding UTF8

Write-Host "Runtime source capture written to: $outputPath"
