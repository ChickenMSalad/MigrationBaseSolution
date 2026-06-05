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

function Assert-NoInvalidTopLevelRuntimeProjects {
    param([string]$RootPath)

    $invalidPaths = @(
        'src\Migration.Infrastructure',
        'src\Migration.Worker'
    )

    foreach ($relativePath in $invalidPaths) {
        $path = Join-Path $RootPath $relativePath
        if (Test-Path -LiteralPath $path) {
            throw "Invalid top-level runtime path found: $relativePath"
        }
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

Assert-NoInvalidTopLevelRuntimeProjects -RootPath $root
Assert-NoInlinePackageVersions -RootPath $root

Assert-PathExists -RootPath $root -RelativePath 'src\Hosts\Migration.Hosts.SqlOperationalWorker\Program.cs'
Assert-PathExists -RootPath $root -RelativePath 'src\Hosts\Migration.Hosts.SqlOperationalWorker\appsettings.Production.template.json'
Assert-PathExists -RootPath $root -RelativePath 'docs\p8\P8.1B-Azure-Hosting-Baseline.md'

Assert-FileContains -RootPath $root -RelativePath 'src\Hosts\Migration.Hosts.SqlOperationalWorker\Program.cs' -Text 'AddEnvironmentVariables(prefix: "MIGRATION_")'
Assert-FileContains -RootPath $root -RelativePath 'src\Hosts\Migration.Hosts.SqlOperationalWorker\Program.cs' -Text 'AddSqlOperationalQueueExecutor'
Assert-FileContains -RootPath $root -RelativePath 'src\Hosts\Migration.Hosts.SqlOperationalWorker\appsettings.Production.template.json' -Text 'SqlOperationalQueueExecutor'
Assert-FileContains -RootPath $root -RelativePath 'src\Hosts\Migration.Hosts.SqlOperationalWorker\appsettings.Production.template.json' -Text 'MigrationOperationalStore'
Assert-FileContains -RootPath $root -RelativePath 'docs\p8\P8.1B-Azure-Hosting-Baseline.md' -Text 'Do not configure `SqlOperationalQueueExecutor:RunId`'

Write-Host 'P8.1B Azure hosting baseline validation passed.'
