Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-ScriptDirectory {
    if (-not [string]::IsNullOrWhiteSpace($PSScriptRoot)) { return $PSScriptRoot }
    if ($MyInvocation.MyCommand.Path) { return (Split-Path -Parent $MyInvocation.MyCommand.Path) }
    return (Get-Location).Path
}

function Join-RepoPath {
    param([Parameter(Mandatory=$true)][string]$RelativePath)
    return (Join-Path -Path $script:RepoRoot -ChildPath $RelativePath)
}

function Assert-FileExists {
    param([Parameter(Mandatory=$true)][string]$RelativePath)
    $path = Join-RepoPath -RelativePath $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Missing expected P5.1.22 file: $RelativePath"
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
    return [string]$attribute.Value
}

$scriptDirectory = Get-ScriptDirectory
$script:RepoRoot = (Resolve-Path (Join-Path $scriptDirectory '..')).Path

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Topology\AzureRuntimeTopologyHandoffStatus.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Topology\AzureRuntimeTopologyHandoffItem.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Topology\AzureRuntimeTopologyHandoffManifest.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Topology\AzureRuntimeTopologyHandoffEvaluator.cs',
    'config\azure-runtime\topology\p5.1-topology-handoff.sample.json'
)

foreach ($relativePath in $expectedFiles) {
    Assert-FileExists -RelativePath $relativePath
}

$projectPath = Join-RepoPath -RelativePath 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (Test-Path -LiteralPath $projectPath -PathType Leaf) {
    [xml]$projectXml = Get-Content -LiteralPath $projectPath -Raw
    $itemGroups = @()
    if ($projectXml.Project.PSObject.Properties['ItemGroup']) {
        $itemGroups = @($projectXml.Project.ItemGroup)
    }

    foreach ($itemGroup in $itemGroups) {
        if ($null -eq $itemGroup) { continue }
        if (-not $itemGroup.PSObject.Properties['PackageReference']) { continue }

        $packageRefs = @($itemGroup.PackageReference)
        foreach ($packageRef in $packageRefs) {
            if ($null -eq $packageRef) { continue }
            $include = Get-XmlAttributeValue -Node $packageRef -Name 'Include'
            $version = Get-XmlAttributeValue -Node $packageRef -Name 'Version'
            if (-not [string]::IsNullOrWhiteSpace($include) -and -not [string]::IsNullOrWhiteSpace($version)) {
                throw "Inline PackageReference Version attribute detected in ${projectPath}: ${include}"
            }
        }
    }

    Write-Host 'Restoring MigrationBase.Core...'
    dotnet restore $projectPath
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

    Write-Host 'Building MigrationBase.Core...'
    dotnet build $projectPath --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }
}
else {
    Write-Host 'MigrationBase.Core project file not found; file presence validation completed only.'
}

Write-Host 'P5.1.22 Azure runtime topology handoff validation passed.'
