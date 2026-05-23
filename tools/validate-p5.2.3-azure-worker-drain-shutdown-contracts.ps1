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
        throw "Missing expected file: $RelativePath"
    }
}

function Get-XmlAttributeValue {
    param(
        [Parameter(Mandatory=$false)]$Node,
        [Parameter(Mandatory=$true)][string]$Name
    )
    if ($null -eq $Node) { return $null }
    $attributesProperty = $Node.PSObject.Properties['Attributes']
    if ($null -eq $attributesProperty -or $null -eq $Node.Attributes) { return $null }
    $attribute = $Node.Attributes[$Name]
    if ($null -eq $attribute) { return $null }
    return $attribute.Value
}

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Lifecycle\AzureWorkerDrainMode.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Lifecycle\AzureWorkerDrainRequest.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Lifecycle\AzureWorkerDrainStatus.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Lifecycle\AzureWorkerShutdownPolicy.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Lifecycle\IAzureWorkerDrainCoordinator.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Lifecycle\IAzureWorkerShutdownSignal.cs',
    'config\azure-runtime\workers\worker-drain-shutdown.sample.json'
)

foreach ($file in $expectedFiles) { Assert-FileExists -RelativePath $file }

$projectPath = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
Assert-FileExists -RelativePath 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'

$projectFiles = @(Get-ChildItem -LiteralPath (Join-Path $repoRoot 'src') -Recurse -Filter '*.csproj' -File | Where-Object {
    $_.FullName -notmatch '\\bin\\' -and $_.FullName -notmatch '\\obj\\'
})

$badInlineVersions = @()
foreach ($projectFile in $projectFiles) {
    [xml]$projectXml = Get-Content -LiteralPath $projectFile.FullName -Raw
    $projectNode = $projectXml.Project
    if ($null -eq $projectNode) { continue }
    $itemGroupProperty = $projectNode.PSObject.Properties['ItemGroup']
    if ($null -eq $itemGroupProperty -or $null -eq $projectNode.ItemGroup) { continue }
    $itemGroups = @($projectNode.ItemGroup)
    foreach ($itemGroup in $itemGroups) {
        if ($null -eq $itemGroup) { continue }
        $packageReferenceProperty = $itemGroup.PSObject.Properties['PackageReference']
        if ($null -eq $packageReferenceProperty -or $null -eq $itemGroup.PackageReference) { continue }
        $packageReferences = @($itemGroup.PackageReference)
        foreach ($packageReference in $packageReferences) {
            if ($null -eq $packageReference) { continue }
            $versionValue = Get-XmlAttributeValue -Node $packageReference -Name 'Version'
            if (-not [string]::IsNullOrWhiteSpace($versionValue)) {
                $badInlineVersions += $projectFile.FullName
            }
        }
    }
}

if (@($badInlineVersions).Length -gt 0) {
    $uniqueBadFiles = @($badInlineVersions | Sort-Object -Unique)
    throw ("Inline PackageReference Version attributes detected:`n - " + ($uniqueBadFiles -join "`n - "))
}

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $projectPath
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

Write-Host 'Building MigrationBase.Core...'
dotnet build $projectPath --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }

Write-Host 'P5.2.3 Azure worker drain/shutdown contract validation passed.'
