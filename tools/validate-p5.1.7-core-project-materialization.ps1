Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

function Assert-FileExists {
    param([Parameter(Mandatory=$true)][string]$RelativePath)
    $fullPath = Join-Path $RepoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Missing expected file: $RelativePath"
    }
}

function Get-XmlElementProperty {
    param(
        [Parameter(Mandatory=$true)]$Element,
        [Parameter(Mandatory=$true)][string]$Name
    )

    if ($null -eq $Element) { return $null }
    if ($null -eq $Element.PSObject) { return $null }
    if ($null -eq $Element.PSObject.Properties[$Name]) { return $null }
    return $Element.PSObject.Properties[$Name].Value
}

function Get-PackageReferences {
    param([Parameter(Mandatory=$true)][xml]$ProjectXml)

    $refs = @()
    $project = Get-XmlElementProperty -Element $ProjectXml -Name 'Project'
    if ($null -eq $project) { return $refs }

    $itemGroupsRaw = Get-XmlElementProperty -Element $project -Name 'ItemGroup'
    $itemGroups = @($itemGroupsRaw)

    foreach ($itemGroup in $itemGroups) {
        if ($null -eq $itemGroup) { continue }
        $packageRefsRaw = Get-XmlElementProperty -Element $itemGroup -Name 'PackageReference'
        foreach ($packageRef in @($packageRefsRaw)) {
            if ($null -ne $packageRef) { $refs += $packageRef }
        }
    }

    return $refs
}

function Assert-PackageReference {
    param(
        [Parameter(Mandatory=$true)][string]$ProjectRelativePath,
        [Parameter(Mandatory=$true)][string]$Include
    )

    $projectPath = Join-Path $RepoRoot $ProjectRelativePath
    [xml]$projectXml = Get-Content -LiteralPath $projectPath -Raw
    $packageRefs = @(Get-PackageReferences -ProjectXml $projectXml)

    $matches = @($packageRefs | Where-Object {
        $includeValue = Get-XmlElementProperty -Element $_ -Name 'Include'
        $includeValue -eq $Include
    })

    if ($matches.Count -eq 0) {
        throw "Missing PackageReference in ${ProjectRelativePath}: ${Include}"
    }

    foreach ($match in $matches) {
        $versionValue = Get-XmlElementProperty -Element $match -Name 'Version'
        if (-not [string]::IsNullOrWhiteSpace([string]$versionValue)) {
            throw "Inline PackageReference Version detected in ${ProjectRelativePath}: ${Include}"
        }
    }
}

function Assert-NoInlinePackageVersions {
    $projectFiles = @(Get-ChildItem -LiteralPath $RepoRoot -Recurse -Filter '*.csproj' -File |
        Where-Object {
            $_.FullName -notmatch '\\bin\\' -and
            $_.FullName -notmatch '\\obj\\'
        })

    $badProjectFiles = @()

    foreach ($projectFile in $projectFiles) {
        [xml]$projectXml = Get-Content -LiteralPath $projectFile.FullName -Raw
        $packageRefs = @(Get-PackageReferences -ProjectXml $projectXml)
        foreach ($packageRef in $packageRefs) {
            $versionValue = Get-XmlElementProperty -Element $packageRef -Name 'Version'
            if (-not [string]::IsNullOrWhiteSpace([string]$versionValue)) {
                $badProjectFiles += $projectFile.FullName
                break
            }
        }
    }

    if (@($badProjectFiles).Count -gt 0) {
        Write-Host 'Inline PackageReference Version attributes detected:' -ForegroundColor Red
        foreach ($bad in @($badProjectFiles | Sort-Object -Unique)) {
            Write-Host " - $bad" -ForegroundColor Red
        }
        throw 'Central package management convention may be violated.'
    }
}

$MigrationBaseCoreProject = 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
$MigrationCoreAzureProject = 'src\Core\Migration.Core.Azure\Migration.Core.Azure.csproj'

Assert-FileExists $MigrationBaseCoreProject
Assert-FileExists $MigrationCoreAzureProject

Assert-PackageReference -ProjectRelativePath $MigrationBaseCoreProject -Include 'Microsoft.Extensions.Configuration.Abstractions'
Assert-PackageReference -ProjectRelativePath $MigrationBaseCoreProject -Include 'Microsoft.Extensions.Configuration.Binder'
Assert-PackageReference -ProjectRelativePath $MigrationBaseCoreProject -Include 'Microsoft.Extensions.Hosting.Abstractions'

Assert-PackageReference -ProjectRelativePath $MigrationCoreAzureProject -Include 'Microsoft.Extensions.Configuration.Abstractions'
Assert-PackageReference -ProjectRelativePath $MigrationCoreAzureProject -Include 'Microsoft.Extensions.Configuration.Binder'
Assert-PackageReference -ProjectRelativePath $MigrationCoreAzureProject -Include 'Microsoft.Extensions.DependencyInjection.Abstractions'

Assert-NoInlinePackageVersions

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore (Join-Path $RepoRoot $MigrationBaseCoreProject)
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

Write-Host 'Building MigrationBase.Core...'
dotnet build (Join-Path $RepoRoot $MigrationBaseCoreProject) --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }

Write-Host 'Restoring Migration.Core.Azure...'
dotnet restore (Join-Path $RepoRoot $MigrationCoreAzureProject)
if ($LASTEXITCODE -ne 0) { throw 'Migration.Core.Azure restore failed.' }

Write-Host 'Building Migration.Core.Azure...'
dotnet build (Join-Path $RepoRoot $MigrationCoreAzureProject) --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Migration.Core.Azure build failed.' }

Write-Host 'P5.1.7 core project materialization validation passed.' -ForegroundColor Green
