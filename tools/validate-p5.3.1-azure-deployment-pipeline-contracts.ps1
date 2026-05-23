Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptDirectory = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $scriptPath = $MyInvocation.MyCommand.Path
    if ([string]::IsNullOrWhiteSpace($scriptPath)) {
        $scriptDirectory = (Get-Location).Path
    }
    else {
        $scriptDirectory = Split-Path -Parent $scriptPath
    }
}

$repoRoot = Split-Path -Parent $scriptDirectory

function Assert-FileExists {
    param([Parameter(Mandatory=$true)][string]$RelativePath)

    $path = Join-Path $repoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Missing expected file: $RelativePath"
    }
}

function Get-XmlAttributeValue {
    param(
        [Parameter(Mandatory=$false)]$Node,
        [Parameter(Mandatory=$true)][string]$Name
    )

    if ($null -eq $Node) { return $null }
    if ($null -eq $Node.Attributes) { return $null }

    $attribute = $Node.Attributes[$Name]
    if ($null -eq $attribute) { return $null }

    return [string]$attribute.Value
}

function Get-ProjectPackageReferences {
    param([Parameter(Mandatory=$true)][string]$ProjectPath)

    if (-not (Test-Path -LiteralPath $ProjectPath -PathType Leaf)) {
        return @()
    }

    [xml]$projectXml = Get-Content -LiteralPath $ProjectPath -Raw

    if ($null -eq $projectXml.Project) {
        return @()
    }

    $itemGroups = @()
    if ($projectXml.Project.PSObject.Properties.Name -contains 'ItemGroup') {
        $itemGroups = @($projectXml.Project.ItemGroup)
    }

    $results = @()
    foreach ($itemGroup in $itemGroups) {
        if ($null -eq $itemGroup) { continue }
        if (-not ($itemGroup.PSObject.Properties.Name -contains 'PackageReference')) { continue }

        foreach ($packageRef in @($itemGroup.PackageReference)) {
            if ($null -eq $packageRef) { continue }

            $include = Get-XmlAttributeValue -Node $packageRef -Name 'Include'
            $version = Get-XmlAttributeValue -Node $packageRef -Name 'Version'
            $update = Get-XmlAttributeValue -Node $packageRef -Name 'Update'

            $results += [pscustomobject]@{
                Include = $include
                Version = $version
                Update = $update
                Project = $ProjectPath
            }
        }
    }

    return @($results)
}

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\Pipeline\AzureDeploymentPipelineStage.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\Pipeline\AzureDeploymentPipelineProfile.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\Pipeline\IAzureDeploymentPipelineProfileRegistry.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\Pipeline\AzureDeploymentPipelineProfileRegistry.cs',
    'config\azure-runtime\deployment\pipeline-profiles.sample.json'
)

foreach ($file in $expectedFiles) {
    Assert-FileExists -RelativePath $file
}

$projectFiles = @(Get-ChildItem -LiteralPath $repoRoot -Recurse -Filter '*.csproj' -File |
    Where-Object {
        $_.FullName -notmatch '\\bin\\' -and
        $_.FullName -notmatch '\\obj\\'
    })

$inlineVersionViolations = @()
foreach ($project in $projectFiles) {
    foreach ($reference in @(Get-ProjectPackageReferences -ProjectPath $project.FullName)) {
        if ($null -ne $reference -and -not [string]::IsNullOrWhiteSpace($reference.Version)) {
            $inlineVersionViolations += ('{0} -> {1}' -f $project.FullName, $reference.Include)
        }
    }
}

if (@($inlineVersionViolations).Length -gt 0) {
    Write-Host 'Inline PackageReference Version attributes detected:'
    foreach ($violation in @($inlineVersionViolations)) {
        Write-Host " - $violation"
    }
    throw 'Central package management convention may be violated.'
}

$coreProject = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (Test-Path -LiteralPath $coreProject -PathType Leaf) {
    Write-Host 'Restoring MigrationBase.Core...'
    dotnet restore $coreProject
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

    Write-Host 'Building MigrationBase.Core...'
    dotnet build $coreProject --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }
}

Write-Host 'P5.3.1 Azure deployment pipeline contract validation passed.'
