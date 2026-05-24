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

function Assert-PathExists { param([string]$RootPath, [string]$RelativePath)
    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) { throw "Required path missing: $RelativePath" }
}

function Assert-NoPathExists { param([string]$RootPath, [string]$RelativePath)
    $path = Join-Path $RootPath $RelativePath
    if (Test-Path -LiteralPath $path) { throw "Invalid path should not exist: $RelativePath" }
}

function Assert-FileContains { param([string]$RootPath, [string]$RelativePath, [string]$Text)
    $path = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) { throw "Required file missing: $RelativePath" }
    $content = Get-Content -LiteralPath $path -Raw
    if ($null -eq $content -or -not $content.Contains($Text)) { throw "Required text missing from $RelativePath : $Text" }
}

function Assert-NoInlinePackageVersions { param([string]$RootPath)
    $projectFiles = Get-ChildItem -Path $RootPath -Filter '*.csproj' -File -Recurse | Where-Object { -not (Test-IsIgnoredPath $_.FullName) }
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

$root = Get-RepositoryRoot
Write-Host "Repository root: $root"

Assert-NoPathExists -RootPath $root -RelativePath 'src\Migration.Infrastructure'
Assert-NoPathExists -RootPath $root -RelativePath 'src\Migration.Worker'

Assert-PathExists -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Processing\SqlOperationalServiceBusWorkItemExecutor.cs'
Assert-FileContains -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Processing\SqlOperationalServiceBusWorkItemExecutor.cs' -Text 'ISqlOperationalWorkItemExecutor'
Assert-FileContains -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Processing\SqlOperationalServiceBusWorkItemExecutor.cs' -Text 'IOperationalWorkItemQueue'
Assert-FileContains -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Processing\SqlOperationalServiceBusWorkItemExecutor.cs' -Text 'ServiceBusWorkItemExecutionResult'

Assert-FileContains -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Program.cs' -Text 'SqlOperationalServiceBusWorkItemExecutor'
Assert-FileContains -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Program.cs' -Text 'AddSqlOperationalMigrationJobWorkItemExecutor'
Assert-FileContains -RootPath $root -RelativePath 'src\Workers\Migration.Workers.ServiceBusExecutor\Migration.Workers.ServiceBusExecutor.csproj' -Text '..\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj'
Assert-PathExists -RootPath $root -RelativePath 'docs\p8\P8.2C-ServiceBus-Real-Execution-Adapter.md'
Assert-NoInlinePackageVersions -RootPath $root

Write-Host 'P8.2C Service Bus real execution adapter validation passed.'
