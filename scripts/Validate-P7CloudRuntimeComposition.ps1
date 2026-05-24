Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    if (-not [string]::IsNullOrWhiteSpace($PSScriptRoot)) {
        return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
    }

    return (Resolve-Path '.').Path
}

function Test-IsIgnoredPath {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $false
    }

    $normalized = $Path.Replace('/', '\').ToLowerInvariant()

    if ($normalized.Contains('\bin\')) {
        return $true
    }

    if ($normalized.Contains('\obj\')) {
        return $true
    }

    return $false
}

function Assert-FileExists {
    param(
        [string]$RepositoryRoot,
        [string]$RelativePath
    )

    $fullPath = Join-Path $RepositoryRoot $RelativePath

    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Required file missing: $RelativePath"
    }
}

function Assert-NoInlinePackageVersions {
    param([string]$RepositoryRoot)

    $projectFiles = @(Get-ChildItem -Path $RepositoryRoot -Filter '*.csproj' -Recurse -File | Where-Object {
        -not (Test-IsIgnoredPath $_.FullName)
    })

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

function Assert-P7CloudRuntimeCompositionFiles {
    param([string]$RepositoryRoot)

    $requiredFiles = @(
        'src\Migration.Infrastructure\Runtime\Composition\DelegateSqlOperationalConnectionFactory.cs',
        'src\Migration.Infrastructure\Runtime\Composition\SqlOperationalRuntimeCompositionOptions.cs',
        'src\Migration.Infrastructure\Runtime\Composition\SqlOperationalRuntimeReadinessProbe.cs',
        'src\Migration.Infrastructure\Runtime\Composition\SqlOperationalRuntimeServiceCollectionExtensions.cs',
        'docs\P7.4-CloudRuntimeComposition-Notes.md'
    )

    foreach ($relativePath in $requiredFiles) {
        Assert-FileExists -RepositoryRoot $RepositoryRoot -RelativePath $relativePath
    }
}

function Assert-NoGeneratedFilesUnderBinObj {
    param([string]$RepositoryRoot)

    $generatedFileNames = @(
        'DelegateSqlOperationalConnectionFactory.cs',
        'SqlOperationalRuntimeCompositionOptions.cs',
        'SqlOperationalRuntimeReadinessProbe.cs',
        'SqlOperationalRuntimeServiceCollectionExtensions.cs'
    )

    $foundFiles = @(Get-ChildItem -Path $RepositoryRoot -Recurse -File | Where-Object {
        -not (Test-IsIgnoredPath $_.FullName) -and
        ($generatedFileNames -contains $_.Name)
    })

    if ($foundFiles.Count -lt 4) {
        throw 'Expected P7.4 cloud runtime composition files were not found outside bin/obj.'
    }
}

$repositoryRoot = Get-RepositoryRoot
Write-Host "Repository root: $repositoryRoot"

Assert-P7CloudRuntimeCompositionFiles -RepositoryRoot $repositoryRoot
Assert-NoGeneratedFilesUnderBinObj -RepositoryRoot $repositoryRoot
Assert-NoInlinePackageVersions -RepositoryRoot $repositoryRoot

Write-Host 'P7.4 cloud runtime composition validation passed.'
