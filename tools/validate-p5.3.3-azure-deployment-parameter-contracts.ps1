Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDirectory = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $invocationPath = $MyInvocation.MyCommand.Path
    if ([string]::IsNullOrWhiteSpace($invocationPath)) { $scriptDirectory = (Get-Location).Path }
    else { $scriptDirectory = Split-Path -Parent $invocationPath }
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
        [Parameter(Mandatory=$true)]$Node,
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
    if (-not $projectXml.Project.PSObject.Properties['ItemGroup']) { return @() }

    $results = New-Object System.Collections.Generic.List[object]
    foreach ($itemGroup in @($projectXml.Project.ItemGroup)) {
        if ($null -eq $itemGroup) { continue }
        if (-not $itemGroup.PSObject.Properties['PackageReference']) { continue }
        foreach ($packageRef in @($itemGroup.PackageReference)) {
            if ($null -eq $packageRef) { continue }
            $include = Get-XmlAttributeValue -Node $packageRef -Name 'Include'
            $version = Get-XmlAttributeValue -Node $packageRef -Name 'Version'
            $results.Add([pscustomobject]@{ Include = $include; Version = $version }) | Out-Null
        }
    }
    return @($results.ToArray())
}

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\AzureDeploymentParameterDescriptor.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\AzureDeploymentParameterRegistry.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\IAzureDeploymentParameterRegistry.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\AzureDeploymentParameterRequirement.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\AzureDeploymentParameterValidationResult.cs',
    'config\azure-runtime\deployment\parameters.registry.sample.json'
)

foreach ($relativePath in $expectedFiles) {
    Assert-FileExists -RelativePath $relativePath
}

$projectFiles = @(Get-ChildItem -LiteralPath $repoRoot -Recurse -Filter '*.csproj' -File |
    Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\' })

$inlineVersionViolations = New-Object System.Collections.Generic.List[string]
foreach ($project in $projectFiles) {
    foreach ($packageReference in @(Get-ProjectPackageReferences -ProjectPath $project.FullName)) {
        if ($null -ne $packageReference.Version -and -not [string]::IsNullOrWhiteSpace([string]$packageReference.Version)) {
            $inlineVersionViolations.Add($project.FullName) | Out-Null
            break
        }
    }
}

if (@($inlineVersionViolations).Length -gt 0) {
    Write-Host 'Inline PackageReference Version attributes detected:' -ForegroundColor Red
    foreach ($violation in @($inlineVersionViolations)) { Write-Host " - $violation" -ForegroundColor Red }
    throw 'Central package management convention violated.'
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
else {
    Write-Host 'MigrationBase.Core project not found; file-level validation only.' -ForegroundColor Yellow
}

Write-Host 'P5.3.3 Azure deployment parameter contract validation passed.' -ForegroundColor Green
