Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-ScriptDirectory {
    if ($PSScriptRoot -and $PSScriptRoot.Trim().Length -gt 0) { return $PSScriptRoot }
    $invocationPath = $MyInvocation.MyCommand.Path
    if ($invocationPath -and $invocationPath.Trim().Length -gt 0) { return (Split-Path -Parent $invocationPath) }
    return (Get-Location).Path
}

function Join-RepoPath {
    param([Parameter(Mandatory=$true)][string]$RelativePath)
    return (Join-Path $script:RepoRoot $RelativePath)
}

function Assert-FileExists {
    param([Parameter(Mandatory=$true)][string]$RelativePath)
    $path = Join-RepoPath $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Missing expected P5.2.8 file: $RelativePath"
    }
}

function Get-SafeChildNodes {
    param([object]$Node, [string]$Name)
    if ($null -eq $Node) { return @() }
    $property = $Node.PSObject.Properties[$Name]
    if ($null -eq $property) { return @() }
    if ($null -eq $property.Value) { return @() }
    return @($property.Value)
}

function Get-XmlAttributeValue {
    param([object]$Node, [string]$Name)
    if ($null -eq $Node) { return $null }
    if ($null -eq $Node.Attributes) { return $null }
    $attribute = $Node.Attributes[$Name]
    if ($null -eq $attribute) { return $null }
    return $attribute.Value
}

$scriptDirectory = Get-ScriptDirectory
$script:RepoRoot = (Resolve-Path (Join-Path $scriptDirectory '..')).Path

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Abandonment\AzureWorkerAbandonmentReason.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Abandonment\AzureWorkerAbandonmentPolicy.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Abandonment\AzureWorkerAbandonmentDecision.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Abandonment\IAzureWorkerAbandonmentPolicyRegistry.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Abandonment\AzureWorkerAbandonmentPolicyRegistry.cs',
    'config\azure-runtime\workers\abandonment.policies.sample.json'
)

foreach ($file in $expectedFiles) { Assert-FileExists $file }

$projectPath = Join-RepoPath 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) {
    throw 'MigrationBase.Core project file is missing. P5.1.7 project materialization must be present before P5.2 worker contracts.'
}

$projectFiles = @(Get-ChildItem -LiteralPath (Join-RepoPath 'src') -Recurse -Filter '*.csproj' -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' })

$inlineVersions = New-Object System.Collections.Generic.List[string]
foreach ($projectFile in $projectFiles) {
    [xml]$projectXml = Get-Content -LiteralPath $projectFile.FullName -Raw
    $itemGroups = Get-SafeChildNodes -Node $projectXml.Project -Name 'ItemGroup'
    foreach ($itemGroup in $itemGroups) {
        $packageRefs = Get-SafeChildNodes -Node $itemGroup -Name 'PackageReference'
        foreach ($packageRef in $packageRefs) {
            $version = Get-XmlAttributeValue -Node $packageRef -Name 'Version'
            if (-not [string]::IsNullOrWhiteSpace($version)) {
                [void]$inlineVersions.Add($projectFile.FullName)
            }
        }
    }
}

if (@($inlineVersions).Length -gt 0) {
    $unique = @($inlineVersions | Sort-Object -Unique)
    throw "Inline PackageReference Version attributes detected:`n - $($unique -join "`n - ")"
}

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $projectPath
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

Write-Host 'Building MigrationBase.Core...'
dotnet build $projectPath --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }

Write-Host 'P5.2.8 Azure worker abandonment contract validation passed.'
