Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDirectory = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $scriptDirectory = Split-Path -Parent $PSCommandPath
}
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $scriptDirectory = (Get-Location).Path
}

$repoRoot = Split-Path -Parent $scriptDirectory
$coreProject = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Runtime\Worker\Capacity\AzureWorkerCapacityDecisionKind.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Runtime\Worker\Capacity\AzureWorkerCapacityLimit.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Runtime\Worker\Capacity\AzureWorkerCapacitySnapshot.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Runtime\Worker\Capacity\AzureWorkerCapacityDecision.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Runtime\Worker\Capacity\IAzureWorkerCapacityGate.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Runtime\Worker\Capacity\AzureWorkerCapacityGate.cs'
)

$missingFiles = @()
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        $missingFiles += $relativePath
    }
}

if ($missingFiles.Length -gt 0) {
    Write-Host 'Missing expected P6.3.6 files:'
    foreach ($missingFile in $missingFiles) { Write-Host " - $missingFile" }
    throw 'P6.3.6 validation failed.'
}

function Get-XmlAttributeValue {
    param(
        [Parameter(Mandatory=$false)] $Node,
        [Parameter(Mandatory=$true)] [string] $Name
    )

    if ($null -eq $Node) { return $null }
    if ($null -eq $Node.Attributes) { return $null }

    $attribute = $Node.Attributes[$Name]
    if ($null -eq $attribute) { return $null }
    return $attribute.Value
}

$projectFiles = @(Get-ChildItem -LiteralPath (Join-Path $repoRoot 'src') -Recurse -Filter '*.csproj' -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' })

$inlineVersionViolations = @()
foreach ($projectFile in $projectFiles) {
    [xml]$projectXml = Get-Content -LiteralPath $projectFile.FullName -Raw
    $itemGroups = @()
    if ($projectXml.PSObject.Properties['Project'] -and $projectXml.Project.PSObject.Properties['ItemGroup']) {
        $itemGroups = @($projectXml.Project.ItemGroup)
    }

    foreach ($itemGroup in $itemGroups) {
        if ($null -eq $itemGroup) { continue }
        if (-not $itemGroup.PSObject.Properties['PackageReference']) { continue }
        $packageReferences = @($itemGroup.PackageReference)
        foreach ($packageReference in $packageReferences) {
            if ($null -eq $packageReference) { continue }
            $version = Get-XmlAttributeValue -Node $packageReference -Name 'Version'
            if (-not [string]::IsNullOrWhiteSpace($version)) {
                $inlineVersionViolations += $projectFile.FullName
            }
        }
    }
}

if ($inlineVersionViolations.Length -gt 0) {
    Write-Host 'Inline PackageReference Version attributes detected:'
    foreach ($violation in @($inlineVersionViolations | Sort-Object -Unique)) { Write-Host " - $violation" }
    throw 'Central package management convention may be violated.'
}

if (-not (Test-Path -LiteralPath $coreProject -PathType Leaf)) {
    throw "Missing core project: $coreProject"
}

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $coreProject
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

Write-Host 'Building MigrationBase.Core...'
dotnet build $coreProject --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }

Write-Host 'P6.3.6 worker capacity gate foundation validation passed.'
