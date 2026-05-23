Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptDirectory = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $scriptPath = $MyInvocation.MyCommand.Path
    if ([string]::IsNullOrWhiteSpace($scriptPath)) { $scriptPath = $MyInvocation.InvocationName }
    if ([string]::IsNullOrWhiteSpace($scriptPath)) { throw 'Unable to resolve script path.' }
    $scriptDirectory = Split-Path -Parent $scriptPath
}

$repoRoot = Split-Path -Parent $scriptDirectory

function Assert-FileExists {
    param([Parameter(Mandatory=$true)][string]$RelativePath)
    $fullPath = Join-Path $repoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
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
    return $attribute.Value
}

function Get-ProjectPackageReferences {
    param([Parameter(Mandatory=$true)][string]$ProjectPath)

    [xml]$projectXml = Get-Content -LiteralPath $ProjectPath -Raw
    if ($null -eq $projectXml.Project) { return @() }
    if (-not $projectXml.Project.PSObject.Properties.Match('ItemGroup').Count) { return @() }

    $results = New-Object System.Collections.Generic.List[object]
    foreach ($itemGroup in @($projectXml.Project.ItemGroup)) {
        if ($null -eq $itemGroup) { continue }
        if (-not $itemGroup.PSObject.Properties.Match('PackageReference').Count) { continue }
        foreach ($packageReference in @($itemGroup.PackageReference)) {
            if ($null -eq $packageReference) { continue }
            $results.Add($packageReference)
        }
    }

    return @($results.ToArray())
}

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\Rollback\AzureDeploymentRollbackPlan.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\Rollback\AzureDeploymentRollbackStep.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\Rollback\AzureDeploymentRollbackStrategy.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\Rollback\AzureDeploymentRollbackApprovalRequirement.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\Rollback\AzureDeploymentRollbackValidationResult.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\Rollback\AzureDeploymentRollbackPlanValidator.cs',
    'config\azure-runtime\deployment\rollback-plans.sample.json'
)

foreach ($expectedFile in $expectedFiles) {
    Assert-FileExists -RelativePath $expectedFile
}

$projectFiles = @(Get-ChildItem -LiteralPath $repoRoot -Recurse -Filter '*.csproj' -File | Where-Object {
    $_.FullName -notmatch '\\bin\\' -and $_.FullName -notmatch '\\obj\\'
})

$inlineVersionViolations = New-Object System.Collections.Generic.List[string]
foreach ($project in $projectFiles) {
    foreach ($packageReference in @(Get-ProjectPackageReferences -ProjectPath $project.FullName)) {
        $versionAttribute = Get-XmlAttributeValue -Node $packageReference -Name 'Version'
        if (-not [string]::IsNullOrWhiteSpace($versionAttribute)) {
            $inlineVersionViolations.Add($project.FullName)
            break
        }
    }
}

if (@($inlineVersionViolations).Length -gt 0) {
    throw ("Inline PackageReference Version attributes detected:`n - " + (($inlineVersionViolations | Sort-Object -Unique) -join "`n - "))
}

$migrationBaseCoreProject = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (Test-Path -LiteralPath $migrationBaseCoreProject -PathType Leaf) {
    Write-Host 'Restoring MigrationBase.Core...'
    dotnet restore $migrationBaseCoreProject
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

    Write-Host 'Building MigrationBase.Core...'
    dotnet build $migrationBaseCoreProject --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }
}

Write-Host 'P5.3.6 Azure deployment rollback contract validation passed.'
