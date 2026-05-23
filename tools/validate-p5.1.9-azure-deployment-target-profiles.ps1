Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path -Parent $PSScriptRoot

function Assert-FileExists {
    param([Parameter(Mandatory=$true)][string]$Path)

    $fullPath = Join-Path $RepoRoot $Path
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Missing expected P5.1.9 file: $Path"
    }
}

function Get-XmlPropertyValue {
    param(
        [Parameter(Mandatory=$false)]$Node,
        [Parameter(Mandatory=$true)][string]$Name
    )

    if ($null -eq $Node) { return $null }

    $property = $Node.PSObject.Properties[$Name]
    if ($null -eq $property) { return $null }

    return $property.Value
}

function Get-XmlAttributeText {
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

$ExpectedFiles = @(
    'src/Core/MigrationBase.Core/Cloud/Azure/Deployment/AzureDeploymentTargetKind.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Deployment/AzureDeploymentTargetDescriptor.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Deployment/AzureDeploymentProfile.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Deployment/AzureDeploymentProfileValidationResult.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Deployment/AzureDeploymentProfileValidator.cs',
    'config/azure-runtime/deployment/deployment-profiles.sample.json'
)

foreach ($file in $ExpectedFiles) {
    Assert-FileExists -Path $file
}

$projectPath = Join-Path $RepoRoot 'src/Core/MigrationBase.Core/MigrationBase.Core.csproj'
if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) {
    throw "MigrationBase.Core project file is missing: $projectPath"
}

$projectFiles = @(Get-ChildItem -LiteralPath $RepoRoot -Recurse -Filter '*.csproj' -File |
    Where-Object {
        $_.FullName -notmatch '\\bin\\' -and
        $_.FullName -notmatch '\\obj\\'
    })

$inlineVersionViolations = New-Object System.Collections.Generic.List[string]

foreach ($projectFile in $projectFiles) {
    [xml]$projectXml = Get-Content -LiteralPath $projectFile.FullName -Raw
    $projectNode = Get-XmlPropertyValue -Node $projectXml -Name 'Project'
    if ($null -eq $projectNode) { continue }

    $itemGroupsRaw = Get-XmlPropertyValue -Node $projectNode -Name 'ItemGroup'
    if ($null -eq $itemGroupsRaw) { continue }

    $itemGroups = @($itemGroupsRaw)
    foreach ($itemGroup in $itemGroups) {
        if ($null -eq $itemGroup) { continue }

        $packageRefsRaw = Get-XmlPropertyValue -Node $itemGroup -Name 'PackageReference'
        if ($null -eq $packageRefsRaw) { continue }

        $packageRefs = @($packageRefsRaw)
        foreach ($packageRef in $packageRefs) {
            if ($null -eq $packageRef) { continue }

            $versionAttribute = Get-XmlAttributeText -Node $packageRef -Name 'Version'
            $versionElement = Get-XmlPropertyValue -Node $packageRef -Name 'Version'
            if (-not [string]::IsNullOrWhiteSpace($versionAttribute) -or $null -ne $versionElement) {
                $inlineVersionViolations.Add($projectFile.FullName)
            }
        }
    }
}

if ($inlineVersionViolations.Count -gt 0) {
    throw ("Inline PackageReference Version attributes detected:`n - " + (($inlineVersionViolations | Sort-Object -Unique) -join "`n - "))
}

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $projectPath
if ($LASTEXITCODE -ne 0) {
    throw 'MigrationBase.Core restore failed.'
}

Write-Host 'Building MigrationBase.Core...'
dotnet build $projectPath --no-restore
if ($LASTEXITCODE -ne 0) {
    throw 'MigrationBase.Core build failed.'
}

Write-Host 'P5.1.9 Azure deployment target profile validation passed.'
