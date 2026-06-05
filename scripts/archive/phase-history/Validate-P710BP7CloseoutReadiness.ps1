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
    if ([string]::IsNullOrWhiteSpace($Path)) { return $false }
    $normalized = $Path.Replace('/', '\').ToLowerInvariant()
    return ($normalized.Contains('\bin\') -or $normalized.Contains('\obj\'))
}

function Assert-PathExists {
    param([string]$RootPath, [string]$RelativePath)
    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required path missing: $RelativePath"
    }
}

function Assert-PathMissing {
    param([string]$RootPath, [string]$RelativePath)
    $path = Join-Path $RootPath $RelativePath
    if (Test-Path -LiteralPath $path) {
        throw "Invalid path should not exist: $RelativePath"
    }
}

function Assert-FileContains {
    param([string]$RootPath, [string]$RelativePath, [string]$Text)
    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required file missing: $RelativePath"
    }
    $content = Get-Content -LiteralPath $path -Raw
    if ($null -eq $content -or -not $content.Contains($Text)) {
        throw "Required text missing from $RelativePath : $Text"
    }
}

function Assert-NoInlinePackageVersions {
    param([string]$RootPath)
    $projectFiles = Get-ChildItem -Path $RootPath -Filter '*.csproj' -File -Recurse |
        Where-Object { -not (Test-IsIgnoredPath $_.FullName) }
    foreach ($projectFile in $projectFiles) {
        [xml]$projectXml = Get-Content -LiteralPath $projectFile.FullName -Raw
        if ($null -eq $projectXml.Project -or $null -eq $projectXml.Project.PSObject.Properties['ItemGroup']) { continue }
        foreach ($itemGroup in @($projectXml.Project.ItemGroup)) {
            if ($null -eq $itemGroup -or $null -eq $itemGroup.PSObject.Properties['PackageReference']) { continue }
            foreach ($packageReference in @($itemGroup.PackageReference)) {
                if ($null -ne $packageReference -and $null -ne $packageReference.PSObject.Properties['Version']) {
                    throw "Inline PackageReference Version found in $($projectFile.FullName)"
                }
            }
        }
    }
}

function Find-SourceFileContaining {
    param([string]$RootPath, [string[]]$RequiredText)
    $files = Get-ChildItem -Path (Join-Path $RootPath 'src') -Filter '*.cs' -File -Recurse |
        Where-Object { -not (Test-IsIgnoredPath $_.FullName) }
    foreach ($file in $files) {
        $content = Get-Content -LiteralPath $file.FullName -Raw
        $matched = $true
        foreach ($text in $RequiredText) {
            if ($null -eq $content -or -not $content.Contains($text)) {
                $matched = $false
                break
            }
        }
        if ($matched) { return $file }
    }
    return $null
}

$root = Get-RepositoryRoot
Write-Host "Repository root: $root"

Assert-PathMissing -RootPath $root -RelativePath 'src\Migration.Infrastructure'
Assert-PathMissing -RootPath $root -RelativePath 'src\Migration.Worker'

Assert-PathExists -RootPath $root -RelativePath 'src\Hosts\Migration.Hosts.SqlOperationalWorker\Migration.Hosts.SqlOperationalWorker.csproj'
Assert-PathExists -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj'
Assert-PathExists -RootPath $root -RelativePath 'src\Core\Migration.Infrastructure.Sql\Migration.Infrastructure.Sql.csproj'
Assert-PathExists -RootPath $root -RelativePath 'src\Core\Migration.Application\Migration.Application.csproj'

Assert-FileContains -RootPath $root -RelativePath 'src\Hosts\Migration.Hosts.SqlOperationalWorker\Program.cs' -Text 'AddSqlOperationalQueueExecutor'
Assert-FileContains -RootPath $root -RelativePath 'src\Hosts\Migration.Hosts.SqlOperationalWorker\Program.cs' -Text 'AddSqlOperationalRuntimeReadiness'
Assert-FileContains -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Services\SqlOperationalWorkItemWorker.cs' -Text 'RunIdOverride'
Assert-FileContains -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Services\SqlOperationalWorkItemWorker.cs' -Text 'GetRunnableRunIdsAsync'
Assert-FileContains -RootPath $root -RelativePath 'src\Core\Migration.Infrastructure.Sql\Operational\Runs\SqlOperationalRunCoordinator.cs' -Text 'GetRunnableRunIdsAsync'
Assert-FileContains -RootPath $root -RelativePath 'src\Core\Migration.Infrastructure.Sql\Operational\ExecutionHistory\SqlOperationalExecutionHistoryWriter.cs' -Text 'AttemptsTableName'
Assert-FileContains -RootPath $root -RelativePath 'database\sql\p7\006_sql_operational_runtime_bootstrap_compatibility.sql' -Text 'migration.Runs'
Assert-FileContains -RootPath $root -RelativePath 'database\sql\p7\007_sql_operational_execution_history.sql' -Text 'WorkItem'

$workItemContracts = Find-SourceFileContaining -RootPath $root -RequiredText @('interface IOperationalWorkItemQueue', 'OperationalWorkItemRecord', 'OperationalWorkItemRunSummary')
if ($null -eq $workItemContracts) {
    throw 'Required work item queue contract file was not found under src.'
}
Write-Host "Work item contracts: $($workItemContracts.FullName.Substring($root.Length).TrimStart('\', '/'))"

Assert-NoInlinePackageVersions -RootPath $root
Write-Host 'P7.10B P7 closeout readiness validation passed.'
