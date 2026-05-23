Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-ScriptDirectory {
    if (-not [string]::IsNullOrWhiteSpace($PSScriptRoot)) { return $PSScriptRoot }
    if ($MyInvocation.MyCommand.Path) { return Split-Path -Parent $MyInvocation.MyCommand.Path }
    return (Get-Location).Path
}

function Assert-FileExists {
    param([Parameter(Mandatory=$true)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Missing expected file: $Path"
    }
}

function Get-ChildElementsByName {
    param(
        [Parameter(Mandatory=$true)]$Node,
        [Parameter(Mandatory=$true)][string]$Name
    )

    $matches = New-Object System.Collections.Generic.List[object]
    if ($null -eq $Node.ChildNodes) { return @() }

    foreach ($child in @($Node.ChildNodes)) {
        if ($null -ne $child -and $child.NodeType -eq [System.Xml.XmlNodeType]::Element -and $child.LocalName -eq $Name) {
            [void]$matches.Add($child)
        }
    }

    return @($matches.ToArray())
}

function Get-XmlAttributeValue {
    param(
        [Parameter(Mandatory=$true)]$Node,
        [Parameter(Mandatory=$true)][string]$Name
    )

    if ($null -eq $Node.Attributes) { return $null }
    $attribute = $Node.Attributes.GetNamedItem($Name)
    if ($null -eq $attribute) { return $null }
    return $attribute.Value
}

$scriptDirectory = Get-ScriptDirectory
$repoRoot = Split-Path -Parent $scriptDirectory

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\AzureDeploymentPromotionGate.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\AzureDeploymentPromotionPolicy.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\AzureDeploymentPromotionDecision.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\IAzureDeploymentPromotionEvaluator.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Deployment\AzureDeploymentPromotionEvaluator.cs',
    'config\azure-runtime\deployment\promotion-policies.sample.json'
)

foreach ($relativePath in $expectedFiles) {
    Assert-FileExists -Path (Join-Path $repoRoot $relativePath)
}

$projectPath = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
Assert-FileExists -Path $projectPath

[xml]$projectXml = Get-Content -LiteralPath $projectPath -Raw
$projectNodes = Get-ChildElementsByName -Node $projectXml -Name 'Project'
if (@($projectNodes).Length -eq 0) {
    throw "Project XML root not found: $projectPath"
}

$inlineVersionViolations = New-Object System.Collections.Generic.List[string]
$itemGroups = Get-ChildElementsByName -Node $projectXml.Project -Name 'ItemGroup'
foreach ($itemGroup in @($itemGroups)) {
    $packageRefs = Get-ChildElementsByName -Node $itemGroup -Name 'PackageReference'
    foreach ($packageRef in @($packageRefs)) {
        $include = Get-XmlAttributeValue -Node $packageRef -Name 'Include'
        $version = Get-XmlAttributeValue -Node $packageRef -Name 'Version'
        if (-not [string]::IsNullOrWhiteSpace($version)) {
            [void]$inlineVersionViolations.Add("${projectPath}: ${include}")
        }
    }
}

if (@($inlineVersionViolations).Length -gt 0) {
    throw ("Inline PackageReference Version attributes detected:`n - " + ($inlineVersionViolations -join "`n - "))
}

$jsonPath = Join-Path $repoRoot 'config\azure-runtime\deployment\promotion-policies.sample.json'
$json = Get-Content -LiteralPath $jsonPath -Raw | ConvertFrom-Json
if ($null -eq $json.PSObject.Properties['promotionPolicies']) {
    throw 'promotion-policies.sample.json must define promotionPolicies.'
}
if (@($json.promotionPolicies).Length -lt 2) {
    throw 'promotion-policies.sample.json must define at least two sample promotion policies.'
}

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $projectPath
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

Write-Host 'Building MigrationBase.Core...'
dotnet build $projectPath --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }

Write-Host 'P5.1.16 Azure promotion policy contract validation passed.'
