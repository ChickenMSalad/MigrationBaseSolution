Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptDirectory = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $scriptPath = $MyInvocation.MyCommand.Path
    if ([string]::IsNullOrWhiteSpace($scriptPath)) { $scriptDirectory = (Get-Location).Path }
    else { $scriptDirectory = Split-Path -Parent $scriptPath }
}
$repoRoot = Split-Path -Parent $scriptDirectory

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\AzureDeploymentEnvironmentManifest.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\AzureDeploymentHostManifest.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\AzureDeploymentEnvironmentManifestValidationResult.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\AzureDeploymentEnvironmentManifestValidator.cs',
    'config\azure-runtime\deployment\environment-manifests.sample.json'
)

$missing = @()
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) { $missing += $relativePath }
}
if (@($missing).Count -gt 0) {
    Write-Host 'Missing expected P5.3.2 files:' -ForegroundColor Red
    foreach ($item in $missing) { Write-Host " - $item" -ForegroundColor Red }
    throw 'P5.3.2 validation failed.'
}

function Get-XmlAttributeValue {
    param(
        [Parameter(Mandatory=$false)] [object] $Node,
        [Parameter(Mandatory=$true)] [string] $Name
    )
    if ($null -eq $Node) { return $null }
    $attributesProperty = $Node.PSObject.Properties['Attributes']
    if ($null -eq $attributesProperty) { return $null }
    $attributes = $attributesProperty.Value
    if ($null -eq $attributes) { return $null }
    $attribute = $attributes[$Name]
    if ($null -eq $attribute) { return $null }
    return $attribute.Value
}

function Get-ChildXmlNodes {
    param(
        [Parameter(Mandatory=$false)] [object] $Node,
        [Parameter(Mandatory=$true)] [string] $ChildName
    )
    if ($null -eq $Node) { return @() }
    $property = $Node.PSObject.Properties[$ChildName]
    if ($null -eq $property) { return @() }
    return @($property.Value) | Where-Object { $null -ne $_ }
}

$projectFiles = @(Get-ChildItem -LiteralPath (Join-Path $repoRoot 'src') -Recurse -Filter '*.csproj' -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' })
$badPackageRefs = @()
foreach ($project in $projectFiles) {
    [xml]$projectXml = Get-Content -LiteralPath $project.FullName -Raw
    foreach ($itemGroup in @(Get-ChildXmlNodes -Node $projectXml.Project -ChildName 'ItemGroup')) {
        foreach ($packageRef in @(Get-ChildXmlNodes -Node $itemGroup -ChildName 'PackageReference')) {
            $versionAttribute = Get-XmlAttributeValue -Node $packageRef -Name 'Version'
            $versionNode = Get-ChildXmlNodes -Node $packageRef -ChildName 'Version'
            if (-not [string]::IsNullOrWhiteSpace($versionAttribute) -or @($versionNode).Count -gt 0) {
                $badPackageRefs += $project.FullName
            }
        }
    }
}
if (@($badPackageRefs).Count -gt 0) {
    Write-Host 'Inline PackageReference Version attributes detected:' -ForegroundColor Red
    foreach ($path in @($badPackageRefs | Sort-Object -Unique)) { Write-Host " - $path" -ForegroundColor Red }
    throw 'Central package management convention may be violated.'
}

$manifestPath = Join-Path $repoRoot 'config\azure-runtime\deployment\environment-manifests.sample.json'
$json = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if ($null -eq $json.PSObject.Properties['environments'] -or @($json.environments).Count -eq 0) {
    throw 'Sample deployment environment manifest must contain at least one environment.'
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

Write-Host 'P5.3.2 Azure deployment environment manifest validation passed.' -ForegroundColor Green
