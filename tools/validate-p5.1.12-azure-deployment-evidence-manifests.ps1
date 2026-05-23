Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptDirectory = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $scriptPath = $MyInvocation.MyCommand.Path
    if ([string]::IsNullOrWhiteSpace($scriptPath)) {
        throw 'Unable to resolve script path.'
    }
    $scriptDirectory = Split-Path -Parent $scriptPath
}

$repoRoot = Split-Path -Parent $scriptDirectory

function Join-RepoPath {
    param([Parameter(Mandatory = $true)][string] $RelativePath)
    return Join-Path $repoRoot $RelativePath
}

function Assert-FileExists {
    param([Parameter(Mandatory = $true)][string] $RelativePath)
    $path = Join-RepoPath -RelativePath $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Missing expected file: $RelativePath"
    }
}

function Get-XmlChildNodes {
    param(
        [Parameter(Mandatory = $false)] $Node,
        [Parameter(Mandatory = $true)][string] $Name
    )

    if ($null -eq $Node) { return @() }
    $property = $Node.PSObject.Properties[$Name]
    if ($null -eq $property) { return @() }
    if ($null -eq $property.Value) { return @() }
    return @($property.Value | Where-Object { $null -ne $_ })
}

function Get-XmlAttributeValue {
    param(
        [Parameter(Mandatory = $false)] $Node,
        [Parameter(Mandatory = $true)][string] $Name
    )

    if ($null -eq $Node) { return $null }
    if ($null -eq $Node.Attributes) { return $null }

    $attribute = $Node.Attributes[$Name]
    if ($null -eq $attribute) { return $null }

    return $attribute.Value
}

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\AzureDeploymentEvidenceItem.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\AzureDeploymentEvidenceManifest.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\AzureDeploymentEvidenceValidationResult.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\AzureDeploymentEvidenceValidator.cs',
    'config\azure-runtime\deployment\evidence-manifest.sample.json'
)

foreach ($expectedFile in $expectedFiles) {
    Assert-FileExists -RelativePath $expectedFile
}

$projectFiles = @(Get-ChildItem -LiteralPath $repoRoot -Recurse -Filter '*.csproj' -File | Where-Object {
    $_.FullName -notmatch '\\bin\\' -and $_.FullName -notmatch '\\obj\\'
})

$inlineVersionRefs = New-Object System.Collections.Generic.List[string]
foreach ($projectFile in $projectFiles) {
    [xml] $projectXml = Get-Content -LiteralPath $projectFile.FullName -Raw
    $projectNodes = Get-XmlChildNodes -Node $projectXml -Name 'Project'
    foreach ($projectNode in $projectNodes) {
        $itemGroups = Get-XmlChildNodes -Node $projectNode -Name 'ItemGroup'
        foreach ($itemGroup in $itemGroups) {
            $packageRefs = Get-XmlChildNodes -Node $itemGroup -Name 'PackageReference'
            foreach ($packageRef in $packageRefs) {
                $versionValue = Get-XmlAttributeValue -Node $packageRef -Name 'Version'
                if (-not [string]::IsNullOrWhiteSpace($versionValue)) {
                    $inlineVersionRefs.Add($projectFile.FullName)
                    break
                }
            }
        }
    }
}

if (@($inlineVersionRefs).Count -gt 0) {
    $message = "Inline PackageReference Version attributes detected:`n - " + ((@($inlineVersionRefs) | Sort-Object -Unique) -join "`n - ")
    throw $message
}

$samplePath = Join-RepoPath -RelativePath 'config\azure-runtime\deployment\evidence-manifest.sample.json'
$sampleJson = Get-Content -LiteralPath $samplePath -Raw | ConvertFrom-Json
if ([string]::IsNullOrWhiteSpace($sampleJson.environmentName)) { throw 'Sample evidence manifest must include environmentName.' }
if ([string]::IsNullOrWhiteSpace($sampleJson.targetName)) { throw 'Sample evidence manifest must include targetName.' }
if ($null -eq $sampleJson.evidenceItems) { throw 'Sample evidence manifest must include evidenceItems.' }
$evidenceItems = @($sampleJson.evidenceItems | Where-Object { $null -ne $_ })
if (@($evidenceItems).Count -lt 3) { throw 'Sample evidence manifest should include at least three evidence items.' }

$coreProject = Join-RepoPath -RelativePath 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (Test-Path -LiteralPath $coreProject -PathType Leaf) {
    Write-Host 'Restoring MigrationBase.Core...'
    dotnet restore $coreProject
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

    Write-Host 'Building MigrationBase.Core...'
    dotnet build $coreProject --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }
}

Write-Host 'P5.1.12 Azure deployment evidence manifest validation passed.'
