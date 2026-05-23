Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDirectory = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $invocationPath = $MyInvocation.MyCommand.Path
    if ([string]::IsNullOrWhiteSpace($invocationPath)) { throw 'Unable to resolve script path.' }
    $scriptDirectory = Split-Path -Parent $invocationPath
}
$repoRoot = Split-Path -Parent $scriptDirectory

function Assert-FileExists {
    param([Parameter(Mandatory=$true)][string]$RelativePath)
    $fullPath = Join-Path $repoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Missing expected P5.2.7 file: ${RelativePath}"
    }
}

function Get-ProjectXml {
    param([Parameter(Mandatory=$true)][string]$ProjectPath)
    if (-not (Test-Path -LiteralPath $ProjectPath -PathType Leaf)) { return $null }
    [xml]$xml = Get-Content -LiteralPath $ProjectPath -Raw
    return $xml
}

function Get-XmlChildrenByName {
    param(
        [Parameter(Mandatory=$false)]$Node,
        [Parameter(Mandatory=$true)][string]$Name
    )
    if ($null -eq $Node) { return @() }
    $properties = $Node.PSObject.Properties
    if ($null -eq $properties -or $null -eq $properties[$Name]) { return @() }
    return @($properties[$Name].Value)
}

function Get-XmlAttributeValue {
    param(
        [Parameter(Mandatory=$false)]$Node,
        [Parameter(Mandatory=$true)][string]$Name
    )
    if ($null -eq $Node) { return $null }
    $attribute = $Node.Attributes[$Name]
    if ($null -eq $attribute) { return $null }
    return $attribute.Value
}

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Concurrency\AzureWorkerConcurrencyProfile.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Concurrency\AzureWorkerConcurrencyLimit.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Concurrency\AzureWorkerConcurrencyAdmissionDecision.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Concurrency\IAzureWorkerConcurrencyProfileRegistry.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Concurrency\AzureWorkerConcurrencyProfileRegistry.cs',
    'config\azure-runtime\workers\concurrency.profiles.sample.json'
)

foreach ($file in $expectedFiles) { Assert-FileExists -RelativePath $file }

$projectPath = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) {
    throw 'MigrationBase.Core.csproj was not found. P5.1.7 project materialization must be present before P5.2.7.'
}

$repoProjects = @(Get-ChildItem -LiteralPath $repoRoot -Recurse -Filter '*.csproj' -File | Where-Object {
    $_.FullName -notmatch '\\bin\\' -and $_.FullName -notmatch '\\obj\\'
})

$inlineVersionViolations = New-Object System.Collections.Generic.List[string]
foreach ($projectFile in $repoProjects) {
    $projectXml = Get-ProjectXml -ProjectPath $projectFile.FullName
    $projectNodes = Get-XmlChildrenByName -Node $projectXml -Name 'Project'
    foreach ($projectNode in $projectNodes) {
        $itemGroups = Get-XmlChildrenByName -Node $projectNode -Name 'ItemGroup'
        foreach ($itemGroup in $itemGroups) {
            $packageRefs = Get-XmlChildrenByName -Node $itemGroup -Name 'PackageReference'
            foreach ($packageRef in $packageRefs) {
                $versionAttribute = Get-XmlAttributeValue -Node $packageRef -Name 'Version'
                $versionNode = Get-XmlChildrenByName -Node $packageRef -Name 'Version'
                if (-not [string]::IsNullOrWhiteSpace($versionAttribute) -or @($versionNode).Length -gt 0) {
                    $inlineVersionViolations.Add($projectFile.FullName)
                }
            }
        }
    }
}

if ($inlineVersionViolations.Count -gt 0) {
    $unique = @($inlineVersionViolations | Sort-Object -Unique)
    throw "Inline PackageReference Version attributes detected outside central package management:`n - $($unique -join "`n - ")"
}

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $projectPath
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

Write-Host 'Building MigrationBase.Core...'
dotnet build $projectPath --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }

Write-Host 'P5.2.7 Azure worker concurrency control contract validation passed.'
