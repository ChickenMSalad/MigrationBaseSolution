Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-ScriptRoot {
    if ($PSScriptRoot -and $PSScriptRoot.Trim().Length -gt 0) {
        return $PSScriptRoot
    }

    $invocationPath = $MyInvocation.MyCommand.Path
    if ($invocationPath -and $invocationPath.Trim().Length -gt 0) {
        return Split-Path -Parent $invocationPath
    }

    return (Get-Location).Path
}

function Get-RepoRoot {
    $scriptRoot = Get-ScriptRoot
    return (Resolve-Path (Join-Path $scriptRoot '..')).Path
}

function Test-PathRequired {
    param(
        [Parameter(Mandatory=$true)][string]$RepoRoot,
        [Parameter(Mandatory=$true)][string[]]$RelativePaths
    )

    $missing = @()
    foreach ($relativePath in $RelativePaths) {
        $fullPath = Join-Path $RepoRoot $relativePath
        if (-not (Test-Path -LiteralPath $fullPath)) {
            $missing += $relativePath
        }
    }

    if (@($missing).Length -gt 0) {
        Write-Host 'Missing expected P5.2.1 files:'
        foreach ($item in $missing) {
            Write-Host " - $item"
        }

        throw 'P5.2.1 validation failed: expected files are missing.'
    }
}

function Get-ProjectFiles {
    param([Parameter(Mandatory=$true)][string]$RepoRoot)

    $allProjects = @(Get-ChildItem -LiteralPath $RepoRoot -Recurse -Filter '*.csproj' -File)
    $filtered = @()
    foreach ($project in $allProjects) {
        $path = $project.FullName
        if ($path -match '[\\/](bin|obj)[\\/]') {
            continue
        }

        $filtered += $project
    }

    return @($filtered)
}

function Get-XmlAttributeValue {
    param(
        [Parameter(Mandatory=$false)]$Node,
        [Parameter(Mandatory=$true)][string]$Name
    )

    if ($null -eq $Node) {
        return $null
    }

    $attributes = $Node.Attributes
    if ($null -eq $attributes) {
        return $null
    }

    $attribute = $attributes[$Name]
    if ($null -eq $attribute) {
        return $null
    }

    return $attribute.Value
}

function Assert-NoInlinePackageVersions {
    param([Parameter(Mandatory=$true)][string]$RepoRoot)

    $violations = @()

    foreach ($project in @(Get-ProjectFiles -RepoRoot $RepoRoot)) {
        [xml]$xml = Get-Content -LiteralPath $project.FullName -Raw

        $projectNode = $xml.Project
        if ($null -eq $projectNode) {
            continue
        }

        $itemGroups = @()
        if ($projectNode.PSObject.Properties['ItemGroup']) {
            $itemGroups = @($projectNode.ItemGroup)
        }

        foreach ($itemGroup in $itemGroups) {
            if ($null -eq $itemGroup) {
                continue
            }

            $packageRefs = @()
            if ($itemGroup.PSObject.Properties['PackageReference']) {
                $packageRefs = @($itemGroup.PackageReference)
            }

            foreach ($packageRef in $packageRefs) {
                if ($null -eq $packageRef) {
                    continue
                }

                $versionAttribute = Get-XmlAttributeValue -Node $packageRef -Name 'Version'
                $hasVersionElement = $false
                if ($packageRef.PSObject.Properties['Version']) {
                    $hasVersionElement = $true
                }

                if ($versionAttribute -or $hasVersionElement) {
                    $violations += $project.FullName
                }
            }
        }
    }

    if (@($violations).Length -gt 0) {
        Write-Host 'Inline PackageReference Version attributes detected:'
        foreach ($violation in @($violations | Sort-Object -Unique)) {
            Write-Host " - $violation"
        }

        throw 'Central package management convention may be violated.'
    }
}

$repoRoot = Get-RepoRoot

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\AzureWorkerLifecyclePhase.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\AzureWorkerLifecycleState.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\AzureWorkerDrainOptions.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\IAzureWorkerLifecycleReporter.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\AzureWorkerLifecycleReportResult.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\AzureWorkerLifecycleValidationResult.cs',
    'config\azure-runtime\workers\lifecycle.sample.json'
)

Test-PathRequired -RepoRoot $repoRoot -RelativePaths $expectedFiles
Assert-NoInlinePackageVersions -RepoRoot $repoRoot

$coreProject = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (Test-Path -LiteralPath $coreProject) {
    Write-Host 'Restoring MigrationBase.Core...'
    dotnet restore $coreProject
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

    Write-Host 'Building MigrationBase.Core...'
    dotnet build $coreProject --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }
}

Write-Host 'P5.2.1 Azure worker lifecycle contract validation passed.'
