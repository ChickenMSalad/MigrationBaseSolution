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

function Assert-NoTextReferenceInSrc {
    param([string]$RootPath, [string]$Text)

    $srcPath = Join-Path $RootPath 'src'
    if (-not (Test-Path -LiteralPath $srcPath)) {
        throw 'Required path missing: src'
    }

    $files = Get-ChildItem -Path $srcPath -File -Recurse |
        Where-Object {
            -not (Test-IsIgnoredPath $_.FullName) -and
            $_.Extension -in @('.cs', '.csproj', '.props', '.targets', '.json')
        }

    foreach ($file in $files) {
        $content = Get-Content -Path $file.FullName -Raw
        if ($null -ne $content -and $content.Contains($Text)) {
            throw "Invalid reference '$Text' found in $($file.FullName)"
        }
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

Assert-PathExists -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Services\ISqlOperationalWorkItemExecutor.cs'
Assert-PathExists -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Services\SqlOperationalWorkItemWorker.cs'
Assert-PathExists -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Services\SqlOperationalMigrationJobWorkItemExecutor.cs'
Assert-PathExists -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Services\SqlOperationalMigrationJobPayloadReader.cs'
Assert-PathExists -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Options\SqlOperationalMigrationJobExecutorOptions.cs'
Assert-PathExists -RootPath $root -RelativePath 'src\Workers\Migration.Workers.QueueExecutor\Registration\SqlOperationalMigrationJobExecutorServiceCollectionExtensions.cs'
Assert-PathExists -RootPath $root -RelativePath 'src\Hosts\Migration.Hosts.SqlOperationalWorker\Program.cs'

Assert-NoTextReferenceInSrc -RootPath $root -Text 'Migration.Infrastructure.Runtime'
Assert-NoInlinePackageVersions -RootPath $root

Write-Host 'P7.7C SQL operational migration job executor validation passed.'
