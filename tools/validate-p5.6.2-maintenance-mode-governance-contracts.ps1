Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-ScriptDirectory {
    if ($PSScriptRoot -and $PSScriptRoot.Trim().Length -gt 0) {
        return $PSScriptRoot
    }

    if ($MyInvocation -and $MyInvocation.MyCommand -and $MyInvocation.MyCommand.Path) {
        return Split-Path -Parent $MyInvocation.MyCommand.Path
    }

    return (Get-Location).Path
}

function Get-RepositoryRoot {
    $scriptDirectory = Get-ScriptDirectory
    return (Resolve-Path (Join-Path $scriptDirectory '..')).Path
}

function Assert-FileExists {
    param(
        [Parameter(Mandatory = $true)][string] $RepositoryRoot,
        [Parameter(Mandatory = $true)][string[]] $RelativePaths
    )

    $missing = New-Object System.Collections.Generic.List[string]

    foreach ($relativePath in $RelativePaths) {
        $fullPath = Join-Path $RepositoryRoot $relativePath
        if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
            [void]$missing.Add($relativePath)
        }
    }

    if ($missing.Count -gt 0) {
        Write-Host 'Missing expected P5.6.2 files:'
        foreach ($path in $missing) {
            Write-Host " - $path"
        }

        throw 'P5.6.2 file validation failed.'
    }
}

function Assert-NoInlinePackageVersions {
    param([Parameter(Mandatory = $true)][string] $RepositoryRoot)

    $badProjectFiles = New-Object System.Collections.Generic.List[string]
    $projectFiles = @(Get-ChildItem -LiteralPath $RepositoryRoot -Recurse -Filter '*.csproj' -File |
        Where-Object {
            $_.FullName -notmatch '\\bin\\' -and
            $_.FullName -notmatch '\\obj\\'
        })

    foreach ($projectFile in $projectFiles) {
        [xml]$projectXml = Get-Content -LiteralPath $projectFile.FullName -Raw

        if (-not $projectXml.PSObject.Properties['Project']) {
            continue
        }

        $projectNode = $projectXml.Project
        if (-not $projectNode.PSObject.Properties['ItemGroup']) {
            continue
        }

        $itemGroups = @($projectNode.ItemGroup)
        foreach ($itemGroup in $itemGroups) {
            if ($null -eq $itemGroup) {
                continue
            }

            if (-not $itemGroup.PSObject.Properties['PackageReference']) {
                continue
            }

            $packageReferences = @($itemGroup.PackageReference)
            foreach ($packageReference in $packageReferences) {
                if ($null -eq $packageReference) {
                    continue
                }

                $hasVersionProperty = $packageReference.PSObject.Properties['Version']
                $hasVersionAttribute = $false

                if ($packageReference.Attributes) {
                    $versionAttribute = $packageReference.Attributes.GetNamedItem('Version')
                    $hasVersionAttribute = $null -ne $versionAttribute
                }

                if ($hasVersionProperty -or $hasVersionAttribute) {
                    [void]$badProjectFiles.Add($projectFile.FullName)
                    break
                }
            }
        }
    }

    if ($badProjectFiles.Count -gt 0) {
        Write-Host 'Inline PackageReference Version attributes detected:'
        foreach ($badProjectFile in $badProjectFiles) {
            Write-Host " - $badProjectFile"
        }

        throw 'Central package management validation failed.'
    }
}

$repoRoot = Get-RepositoryRoot

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Governance\AzureMaintenanceModeState.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Governance\AzureOperationalFreezeScope.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Governance\AzureMaintenanceModeDescriptor.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Governance\AzureOperationalFreezeDescriptor.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Governance\AzureMaintenanceModeDecision.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Governance\IAzureMaintenanceModeEvaluator.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Governance\AzureMaintenanceModeEvaluator.cs',
    'config\azure-runtime\governance\maintenance-mode.sample.json'
)

Assert-FileExists -RepositoryRoot $repoRoot -RelativePaths $expectedFiles
Assert-NoInlinePackageVersions -RepositoryRoot $repoRoot

$coreProject = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (-not (Test-Path -LiteralPath $coreProject -PathType Leaf)) {
    throw "Missing expected project file: ${coreProject}"
}

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $coreProject
if ($LASTEXITCODE -ne 0) {
    throw 'MigrationBase.Core restore failed.'
}

Write-Host 'Building MigrationBase.Core...'
dotnet build $coreProject --no-restore
if ($LASTEXITCODE -ne 0) {
    throw 'MigrationBase.Core build failed.'
}

Write-Host 'P5.6.2 maintenance mode governance contract validation passed.'
