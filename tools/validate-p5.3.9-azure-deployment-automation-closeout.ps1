Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDirectory = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $scriptPath = $MyInvocation.MyCommand.Path
    if ([string]::IsNullOrWhiteSpace($scriptPath)) { throw 'Unable to resolve script path.' }
    $scriptDirectory = Split-Path -Parent $scriptPath
}
$repoRoot = Split-Path -Parent $scriptDirectory

function Assert-FileExists {
    param([Parameter(Mandatory=$true)][string]$RelativePath)
    $path = Join-Path $repoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Missing expected P5.3.9 file: $RelativePath"
    }
}

function Get-ProjectPackageReferences {
    param([Parameter(Mandatory=$true)][string]$ProjectPath)

    if (-not (Test-Path -LiteralPath $ProjectPath -PathType Leaf)) { return @() }

    [xml]$xml = Get-Content -LiteralPath $ProjectPath -Raw
    $projectNode = $xml.Project
    if ($null -eq $projectNode) { return @() }

    $itemGroups = @()
    if ($projectNode.PSObject.Properties.Match('ItemGroup').Count -gt 0 -and $null -ne $projectNode.ItemGroup) {
        $itemGroups = @($projectNode.ItemGroup)
    }

    $refs = @()
    foreach ($itemGroup in $itemGroups) {
        if ($null -eq $itemGroup) { continue }
        if ($itemGroup.PSObject.Properties.Match('PackageReference').Count -eq 0) { continue }
        foreach ($packageReference in @($itemGroup.PackageReference)) {
            if ($null -ne $packageReference) { $refs += $packageReference }
        }
    }

    return @($refs)
}

Assert-FileExists 'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\AzureDeploymentAutomationCloseoutDescriptor.cs'
Assert-FileExists 'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\AzureDeploymentAutomationCloseoutResult.cs'
Assert-FileExists 'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\AzureDeploymentAutomationCloseoutValidator.cs'
Assert-FileExists 'config\azure-runtime\deployment\deployment-automation-closeout.sample.json'

$projectFiles = @(Get-ChildItem -LiteralPath $repoRoot -Recurse -Filter '*.csproj' -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' })

foreach ($project in $projectFiles) {
    foreach ($packageReference in @(Get-ProjectPackageReferences -ProjectPath $project.FullName)) {
        $versionProperty = $packageReference.PSObject.Properties.Match('Version')
        if ($versionProperty.Count -gt 0) {
            throw "Inline PackageReference Version attribute detected in $($project.FullName)."
        }
    }
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

Write-Host 'P5.3.9 Azure deployment automation closeout validation passed.'
