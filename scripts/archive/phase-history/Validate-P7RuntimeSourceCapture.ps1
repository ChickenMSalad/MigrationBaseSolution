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

function Test-IsIgnoredPath {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $false
    }

    $normalized = $Path.Replace('/', '\').ToLowerInvariant()
    return ($normalized.Contains('\bin\') -or $normalized.Contains('\obj\'))
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

function Assert-PathMissing {
    param(
        [string]$RootPath,
        [string]$RelativePath
    )

    $path = Join-Path $RootPath $RelativePath
    if (Test-Path -LiteralPath $path) {
        throw "Invalid path should not exist: $RelativePath"
    }
}

function Assert-NoInlinePackageVersions {
    param([string]$RootPath)

    $projectFiles = Get-ChildItem -Path $RootPath -Filter '*.csproj' -File -Recurse |
        Where-Object { -not (Test-IsIgnoredPath $_.FullName) }

    foreach ($projectFile in $projectFiles) {
        [xml]$projectXml = Get-Content -Path $projectFile.FullName -Raw

        if ($null -eq $projectXml.Project -or
            $null -eq $projectXml.Project.PSObject.Properties['ItemGroup']) {
            continue
        }

        foreach ($itemGroup in @($projectXml.Project.ItemGroup)) {
            if ($null -eq $itemGroup -or
                $null -eq $itemGroup.PSObject.Properties['PackageReference']) {
                continue
            }

            foreach ($packageReference in @($itemGroup.PackageReference)) {
                if ($null -ne $packageReference -and
                    $null -ne $packageReference.PSObject.Properties['Version']) {
                    throw "Inline PackageReference Version found in $($projectFile.FullName)"
                }
            }
        }
    }
}

$root = Get-RepositoryRoot
Write-Host "Repository root: $root"

Assert-PathMissing -RootPath $root -RelativePath 'src\Migration.Infrastructure'
Assert-PathMissing -RootPath $root -RelativePath 'src\Migration.Worker'

Assert-PathExists -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj'
Assert-PathExists -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\QueueWorkerLoopService.cs'
Assert-PathExists -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Services\MigrationRunQueueWorker.cs'
Assert-PathExists -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Registration\QueueExecutorServiceCollectionExtensions.cs'
Assert-PathExists -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Configuration\QueueExecutorHostBuilderExtensions.cs'
Assert-PathExists -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Options\QueueExecutorOptions.cs'
Assert-PathExists -RootPath $root -RelativePath 'src\Core\Migration.Infrastructure.Sql\Migration.Infrastructure.Sql.csproj'
Assert-PathExists -RootPath $root -RelativePath 'src\Core\Migration.Infrastructure.Sql\Operational\WorkItems\SqlOperationalWorkItemQueue.cs'
Assert-PathExists -RootPath $root -RelativePath 'src\Core\Migration.Infrastructure.Sql\Operational\Runs\SqlOperationalRunCoordinator.cs'
Assert-PathExists -RootPath $root -RelativePath 'src\Core\Migration.Infrastructure.Sql\Operational\Leases\SqlOperationalWorkItemLeaseCoordinator.cs'
Assert-PathExists -RootPath $root -RelativePath 'src\Core\Migration.Infrastructure.Sql\Registration\SqlOperationalStoreServiceCollectionExtensions.cs'
Assert-PathExists -RootPath $root -RelativePath 'src\Core\Migration.Infrastructure.Sql\Stores\ISqlOperationalBackboneStore.cs'
Assert-PathExists -RootPath $root -RelativePath 'src\Core\Migration.Infrastructure.Sql\Stores\SqlOperationalBackboneStore.cs'
Assert-PathExists -RootPath $root -RelativePath 'src\Core\Migration.Orchestration\Abstractions\IMigrationJobRunner.cs'
Assert-PathExists -RootPath $root -RelativePath 'src\Core\Migration.Orchestration\Execution\GenericMigrationJobRunner.cs'
Assert-PathExists -RootPath $root -RelativePath 'src\Core\Migration.Orchestration\Extensions\ServiceCollectionExtensions.cs'
Assert-PathExists -RootPath $root -RelativePath 'src\Core\Migration.GenericRuntime\Registration\MigrationRuntimeServiceCollectionExtensions.cs'
Assert-PathExists -RootPath $root -RelativePath 'src\Hosts\Migration.Hosts.SqlOperationalWorker\Migration.Hosts.SqlOperationalWorker.csproj'
Assert-PathExists -RootPath $root -RelativePath 'src\Hosts\Migration.Hosts.SqlOperationalWorker\Program.cs'

Assert-NoInlinePackageVersions -RootPath $root

Write-Host 'P7.7A runtime source capture validation passed.'
