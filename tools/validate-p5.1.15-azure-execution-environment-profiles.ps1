Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptDirectory = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $invocationPath = $MyInvocation.MyCommand.Path
    if (-not [string]::IsNullOrWhiteSpace($invocationPath)) {
        $scriptDirectory = Split-Path -Parent $invocationPath
    }
}
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) { throw 'Unable to resolve script directory.' }
$repoRoot = Resolve-Path (Join-Path $scriptDirectory '..')

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Execution\AzureExecutionEnvironmentProfile.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Execution\AzureExecutionEnvironmentProfileRegistry.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Execution\IAzureExecutionEnvironmentProfileRegistry.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Execution\AzureExecutionEnvironmentProfileValidationResult.cs',
    'config\azure-runtime\execution\execution-environment-profiles.sample.json'
)

$missing = @()
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) { $missing += $relativePath }
}
if (@($missing).Length -gt 0) {
    Write-Host 'Missing expected P5.1.15 files:' -ForegroundColor Red
    foreach ($item in $missing) { Write-Host " - $item" -ForegroundColor Red }
    exit 1
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

$projectFiles = @(Get-ChildItem -LiteralPath (Join-Path $repoRoot 'src') -Filter '*.csproj' -Recurse | Where-Object {
    $_.FullName -notmatch '[\\/](bin|obj)[\\/]'
})

$inlineVersionViolations = @()
foreach ($projectFile in $projectFiles) {
    [xml]$projectXml = Get-Content -LiteralPath $projectFile.FullName -Raw
    $projectNode = $projectXml.Project
    if ($null -eq $projectNode) { continue }
    if (-not $projectNode.PSObject.Properties['ItemGroup']) { continue }
    $itemGroups = @($projectNode.ItemGroup)
    foreach ($itemGroup in $itemGroups) {
        if ($null -eq $itemGroup) { continue }
        if (-not $itemGroup.PSObject.Properties['PackageReference']) { continue }
        $packageRefs = @($itemGroup.PackageReference)
        foreach ($packageRef in $packageRefs) {
            if ($null -eq $packageRef) { continue }
            $versionAttribute = Get-XmlAttributeValue -Node $packageRef -Name 'Version'
            if (-not [string]::IsNullOrWhiteSpace($versionAttribute)) {
                $inlineVersionViolations += $projectFile.FullName
            }
        }
    }
}

if (@($inlineVersionViolations).Length -gt 0) {
    Write-Host 'Inline PackageReference Version attributes detected:' -ForegroundColor Red
    foreach ($violation in @($inlineVersionViolations | Sort-Object -Unique)) { Write-Host " - $violation" -ForegroundColor Red }
    exit 1
}

$coreProject = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (Test-Path -LiteralPath $coreProject) {
    Write-Host 'Restoring MigrationBase.Core...'
    dotnet restore $coreProject
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }
    Write-Host 'Building MigrationBase.Core...'
    dotnet build $coreProject --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }
}

Write-Host 'P5.1.15 Azure execution environment profile validation passed.' -ForegroundColor Green
