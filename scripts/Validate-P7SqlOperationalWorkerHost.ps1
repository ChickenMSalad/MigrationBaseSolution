Set-StrictMode -Version Latest
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
    return $normalized.Contains('\bin\') -or $normalized.Contains('\obj\')
}

function Assert-FileExists {
    param(
        [string]$RootPath,
        [string]$RelativePath
    )

    $fullPath = Join-Path $RootPath $RelativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Required file missing: $RelativePath"
    }
}

function Assert-DirectoryDoesNotExist {
    param(
        [string]$RootPath,
        [string]$RelativePath
    )

    $fullPath = Join-Path $RootPath $RelativePath
    if (Test-Path -LiteralPath $fullPath) {
        throw "Invalid misplaced directory exists and must be removed: $RelativePath"
    }
}

function Test-NoInlinePackageVersions {
    param([string]$RootPath)

    $projectFiles = Get-ChildItem -Path $RootPath -Filter '*.csproj' -Recurse |
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

$repositoryRoot = Get-RepositoryRoot
Write-Host "Repository root: $repositoryRoot"

Assert-DirectoryDoesNotExist -RootPath $repositoryRoot -RelativePath 'src\Migration.Infrastructure'
Assert-DirectoryDoesNotExist -RootPath $repositoryRoot -RelativePath 'src\Migration.Worker'

Assert-FileExists -RootPath $repositoryRoot -RelativePath 'src\Core\Migration.Infrastructure\Migration.Infrastructure.csproj'
Assert-FileExists -RootPath $repositoryRoot -RelativePath 'src\Core\Migration.Infrastructure.Sql\Migration.Infrastructure.Sql.csproj'
Assert-FileExists -RootPath $repositoryRoot -RelativePath 'src\Hosts\Migration.Hosts.SqlOperationalWorker\Migration.Hosts.SqlOperationalWorker.csproj'
Assert-FileExists -RootPath $repositoryRoot -RelativePath 'src\Hosts\Migration.Hosts.SqlOperationalWorker\Program.cs'
Assert-FileExists -RootPath $repositoryRoot -RelativePath 'src\Hosts\Migration.Hosts.SqlOperationalWorker\appsettings.json'

Test-NoInlinePackageVersions -RootPath $repositoryRoot

Write-Host 'P7.5A SQL operational worker host validation passed.'
