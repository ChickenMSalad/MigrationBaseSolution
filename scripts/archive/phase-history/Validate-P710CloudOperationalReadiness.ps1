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
        throw "Invalid repo-native path should not exist: $RelativePath"
    }
}

function Assert-FileContains {
    param(
        [string]$RootPath,
        [string]$RelativePath,
        [string]$Text
    )

    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required file missing: $RelativePath"
    }

    $content = Get-Content -LiteralPath $path -Raw
    if ($null -eq $content -or -not $content.Contains($Text)) {
        throw "Required text missing from $RelativePath : $Text"
    }
}

function Assert-AnySourceContains {
    param(
        [string]$RootPath,
        [string]$SearchRootRelativePath,
        [string[]]$RequiredTexts,
        [string]$Description
    )

    $searchRoot = Join-Path $RootPath $SearchRootRelativePath
    if (-not (Test-Path -LiteralPath $searchRoot)) {
        throw "Search root missing for $Description : $SearchRootRelativePath"
    }

    $files = Get-ChildItem -Path $searchRoot -Filter '*.cs' -File -Recurse |
        Where-Object { -not (Test-IsIgnoredPath $_.FullName) }

    foreach ($file in $files) {
        $content = Get-Content -LiteralPath $file.FullName -Raw
        $allFound = $true
        foreach ($requiredText in $RequiredTexts) {
            if ($null -eq $content -or -not $content.Contains($requiredText)) {
                $allFound = $false
                break
            }
        }

        if ($allFound) {
            return
        }
    }

    throw "Required source pattern not found for $Description"
}

function Assert-NoInlinePackageVersions {
    param([string]$RootPath)

    $projectFiles = Get-ChildItem -Path $RootPath -Filter '*.csproj' -File -Recurse |
        Where-Object { -not (Test-IsIgnoredPath $_.FullName) }

    foreach ($projectFile in $projectFiles) {
        [xml]$projectXml = Get-Content -LiteralPath $projectFile.FullName -Raw

        if ($null -eq $projectXml.Project -or
            $null -eq $projectXml.Project.PSObject.Properties['ItemGroup']) {
            continue
        }

        $itemGroups = @($projectXml.Project.ItemGroup)
        foreach ($itemGroup in $itemGroups) {
            if ($null -eq $itemGroup -or
                $null -eq $itemGroup.PSObject.Properties['PackageReference']) {
                continue
            }

            $packageReferences = @($itemGroup.PackageReference)
            foreach ($packageReference in $packageReferences) {
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

Assert-PathExists -RootPath $root -RelativePath 'src\Core\Migration.Infrastructure.Sql\Migration.Infrastructure.Sql.csproj'
Assert-PathExists -RootPath $root -RelativePath 'src\Core\Migration.Infrastructure\Migration.Infrastructure.csproj'
Assert-PathExists -RootPath $root -RelativePath 'src\Core\Migration.Application\Migration.Application.csproj'
Assert-PathExists -RootPath $root -RelativePath 'src\Core\Migration.Orchestration\Migration.Orchestration.csproj'
Assert-PathExists -RootPath $root -RelativePath 'src\Core\Migration.GenericRuntime\Migration.GenericRuntime.csproj'
Assert-PathExists -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj'
Assert-PathExists -RootPath $root -RelativePath 'src\Hosts\Migration.Hosts.SqlOperationalWorker\Migration.Hosts.SqlOperationalWorker.csproj'

Assert-PathExists -RootPath $root -RelativePath 'database\sql\p7\006_sql_operational_runtime_bootstrap_compatibility.sql'
Assert-PathExists -RootPath $root -RelativePath 'database\sql\p7\007_sql_operational_execution_history.sql'
Assert-PathExists -RootPath $root -RelativePath 'scripts\Test-P7SqlOperationalRuntimeSchema.ps1'
Assert-PathExists -RootPath $root -RelativePath 'scripts\Test-P7SqlOperationalExecutionHistorySchema.ps1'

Assert-FileContains -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Services\SqlOperationalWorkItemWorker.cs' -Text 'GetRunnableRunIdsAsync'
Assert-FileContains -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Services\SqlOperationalWorkItemWorker.cs' -Text 'RunIdOverride'
Assert-FileContains -RootPath $root -RelativePath 'src\Core\Migration.Infrastructure.Sql\Operational\Runs\SqlOperationalRunCoordinator.cs' -Text 'GetRunnableRunIdsAsync'
Assert-FileContains -RootPath $root -RelativePath 'src\Core\Migration.Infrastructure.Sql\Operational\ExecutionHistory\SqlOperationalExecutionHistoryWriter.cs' -Text 'AttemptsTableName'

Assert-AnySourceContains -RootPath $root -SearchRootRelativePath 'src\Core\Migration.Application' -RequiredTexts @('interface IOperationalWorkItemQueue', 'OperationalWorkItemRecord', 'OperationalWorkItemRunSummary') -Description 'operational work item queue contract'
Assert-AnySourceContains -RootPath $root -SearchRootRelativePath 'src\Core\Migration.Application' -RequiredTexts @('interface IOperationalRunCoordinator', 'GetRunnableRunIdsAsync') -Description 'operational run coordinator contract'

Assert-NoInlinePackageVersions -RootPath $root

Write-Host 'P7.10 cloud operational readiness validation passed.'
