Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    if ($PSScriptRoot) {
        return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
    }

    return (Get-Location).Path
}

function Test-IsIgnoredPath {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $false
    }

    $normalized = $Path.Replace('/', '\').ToLowerInvariant()
    return ($normalized.Contains('\bin\') -or $normalized.Contains('\obj\') -or $normalized.Contains('\node_modules\'))
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

function Assert-NoForbiddenReferencesInSrc {
    param([string]$RootPath)

    $srcPath = Join-Path $RootPath 'src'
    $files = Get-ChildItem -Path $srcPath -File -Recurse |
        Where-Object {
            -not (Test-IsIgnoredPath $_.FullName) -and
            $_.Extension -in @('.cs', '.csproj', '.json')
        }

    foreach ($file in $files) {
        $content = Get-Content -Path $file.FullName -Raw
        if ($null -eq $content) {
            continue
        }

        if ($content.Contains('Migration.Infrastructure.Runtime')) {
            throw "Forbidden namespace reference found in $($file.FullName)"
        }

        if ($content.Contains('src\Migration.Infrastructure') -or $content.Contains('src/Migration.Infrastructure')) {
            throw "Forbidden top-level infrastructure path reference found in $($file.FullName)"
        }

        if ($content.Contains('src\Migration.Worker') -or $content.Contains('src/Migration.Worker')) {
            throw "Forbidden top-level worker path reference found in $($file.FullName)"
        }
    }
}

$root = Get-RepositoryRoot
Write-Host "Repository root: $root"

Assert-PathMissing -RootPath $root -RelativePath 'src\Migration.Infrastructure'
Assert-PathMissing -RootPath $root -RelativePath 'src\Migration.Worker'

Assert-PathExists -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Services\SqlOperationalWorkItemWorker.cs'
Assert-PathExists -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Services\ISqlOperationalWorkItemExecutor.cs'
Assert-PathExists -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Services\LoggingSqlOperationalWorkItemExecutor.cs'
Assert-PathExists -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Registration\SqlOperationalQueueExecutorServiceCollectionExtensions.cs'
Assert-PathExists -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Options\SqlOperationalQueueExecutorOptions.cs'
Assert-PathExists -RootPath $root -RelativePath 'src\Core\Migration.Infrastructure.Sql\Migration.Infrastructure.Sql.csproj'
Assert-PathExists -RootPath $root -RelativePath 'src\Hosts\Migration.Hosts.SqlOperationalWorker\Program.cs'

$queueExecutorProject = Join-Path $root 'src\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj'
$queueExecutorProjectText = Get-Content -Path $queueExecutorProject -Raw
if (-not $queueExecutorProjectText.Contains('..\..\Core\Migration.Infrastructure.Sql\Migration.Infrastructure.Sql.csproj')) {
    throw 'QueueExecutor project must reference src\Core\Migration.Infrastructure.Sql.'
}

$hostProject = Join-Path $root 'src\Hosts\Migration.Hosts.SqlOperationalWorker\Migration.Hosts.SqlOperationalWorker.csproj'
$hostProjectText = Get-Content -Path $hostProject -Raw
if (-not $hostProjectText.Contains('..\..\Workers\Migration.Workers.QueueExecutor\Migration.Workers.QueueExecutor.csproj')) {
    throw 'SQL operational worker host must reference Migration.Workers.QueueExecutor.'
}

Assert-NoForbiddenReferencesInSrc -RootPath $root
Assert-NoInlinePackageVersions -RootPath $root

Write-Host 'P7.7B SQL operational queue adapter validation passed.'
