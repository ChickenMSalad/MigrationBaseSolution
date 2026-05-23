Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptDirectory = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $scriptPath = $MyInvocation.MyCommand.Path
    if ([string]::IsNullOrWhiteSpace($scriptPath)) { $scriptDirectory = (Get-Location).Path }
    else { $scriptDirectory = Split-Path -Parent $scriptPath }
}
$repoRoot = Split-Path -Parent $scriptDirectory

function Assert-FileExists {
    param([Parameter(Mandatory=$true)][string]$RelativePath)
    $fullPath = Join-Path $repoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Missing expected P5.1.19 file: ${RelativePath}"
    }
}

function Get-XmlChildNodesByName {
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
    'src\Core\MigrationBase.Core\Cloud\Azure\ExecutionIsolation\AzureExecutionIsolationBoundary.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\ExecutionIsolation\AzureExecutionIsolationProfile.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\ExecutionIsolation\AzureExecutionIsolationRequirement.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\ExecutionIsolation\AzureExecutionIsolationValidationResult.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\ExecutionIsolation\AzureExecutionIsolationValidator.cs',
    'config\azure-runtime\execution-isolation\execution-isolation.sample.json'
)

foreach ($relativePath in $expectedFiles) { Assert-FileExists -RelativePath $relativePath }

$projectFiles = @(Get-ChildItem -LiteralPath $repoRoot -Recurse -Filter '*.csproj' -File |
    Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\' })

$inlineVersionProjects = New-Object System.Collections.Generic.List[string]
foreach ($projectFile in $projectFiles) {
    [xml]$projectXml = Get-Content -LiteralPath $projectFile.FullName -Raw
    $itemGroups = Get-XmlChildNodesByName -Node $projectXml.Project -Name 'ItemGroup'
    foreach ($itemGroup in $itemGroups) {
        $packageRefs = Get-XmlChildNodesByName -Node $itemGroup -Name 'PackageReference'
        foreach ($packageRef in $packageRefs) {
            $versionAttribute = Get-XmlAttributeValue -Node $packageRef -Name 'Version'
            $versionChild = Get-XmlChildNodesByName -Node $packageRef -Name 'Version'
            if (-not [string]::IsNullOrWhiteSpace($versionAttribute) -or @($versionChild).Length -gt 0) {
                $inlineVersionProjects.Add($projectFile.FullName)
            }
        }
    }
}

if ($inlineVersionProjects.Count -gt 0) {
    $unique = @($inlineVersionProjects | Sort-Object -Unique)
    throw ("Inline PackageReference Version attributes detected:`n - " + ($unique -join "`n - "))
}

$projectPath = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (Test-Path -LiteralPath $projectPath -PathType Leaf) {
    Write-Host 'Restoring MigrationBase.Core...'
    dotnet restore $projectPath
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

    Write-Host 'Building MigrationBase.Core...'
    dotnet build $projectPath --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }
}

Write-Host 'P5.1.19 Azure execution isolation contract validation passed.'
