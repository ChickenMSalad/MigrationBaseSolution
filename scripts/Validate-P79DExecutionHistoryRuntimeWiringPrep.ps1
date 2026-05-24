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

Assert-PathExists -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Services\SqlOperationalWorkItemWorker.cs'
Assert-PathExists -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Services\SqlOperationalMigrationJobWorkItemExecutor.cs'
Assert-PathExists -RootPath $root -RelativePath 'src\Core\Migration.Infrastructure.Sql\Operational\ExecutionHistory\SqlOperationalExecutionHistoryWriter.cs'

$workItemContractFile = Get-ChildItem -Path (Join-Path $root 'src\Core\Migration.Application') -Filter '*.cs' -File -Recurse |
    Where-Object { -not (Test-IsIgnoredPath $_.FullName) } |
    Where-Object {
        $content = Get-Content -LiteralPath $_.FullName -Raw
        $content.Contains('interface IOperationalWorkItemQueue') -and
        $content.Contains('OperationalWorkItemRecord')
    } |
    Select-Object -First 1

if ($null -eq $workItemContractFile) {
    throw "Required work item contract file containing IOperationalWorkItemQueue and OperationalWorkItemRecord was not found under src\Core\Migration.Application"
}

Assert-PathExists -RootPath $root -RelativePath 'scripts\Write-P79DExecutionHistoryRuntimeWiringPatchPlan.ps1'
Assert-PathExists -RootPath $root -RelativePath 'docs\p7\P7.9D-Execution-History-Runtime-Wiring-Prep.md'

Assert-NoInlinePackageVersions -RootPath $root

Write-Host 'P7.9D execution-history runtime wiring prep validation passed.'
