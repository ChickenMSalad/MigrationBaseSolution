Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-ScriptDirectory {
    if (-not [string]::IsNullOrWhiteSpace($PSScriptRoot)) { return $PSScriptRoot }
    if ($MyInvocation -and $MyInvocation.MyCommand -and -not [string]::IsNullOrWhiteSpace($MyInvocation.MyCommand.Path)) {
        return Split-Path -Parent $MyInvocation.MyCommand.Path
    }
    return (Get-Location).Path
}

function Get-RepoRoot {
    $scriptDirectory = Get-ScriptDirectory
    return (Resolve-Path (Join-Path $scriptDirectory '..')).Path
}

function Get-ChildElementArray {
    param(
        [Parameter(Mandatory=$false)] $Node,
        [Parameter(Mandatory=$true)] [string] $Name
    )

    if ($null -eq $Node) { return @() }
    $property = $Node.PSObject.Properties[$Name]
    if ($null -eq $property) { return @() }
    if ($null -eq $property.Value) { return @() }
    return @($property.Value)
}

function Get-XmlAttributeValue {
    param(
        [Parameter(Mandatory=$false)] $Node,
        [Parameter(Mandatory=$true)] [string] $Name
    )

    if ($null -eq $Node) { return $null }
    $attribute = $Node.Attributes[$Name]
    if ($null -eq $attribute) { return $null }
    return $attribute.Value
}

$repoRoot = Get-RepoRoot
$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Tenancy\AzureOperationalTenantBoundary.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Tenancy\AzureOperationalTenantBoundaryRegistry.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Tenancy\IAzureOperationalTenantBoundaryRegistry.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Tenancy\AzureOperationalTenantBoundaryValidationResult.cs',
    'config\azure-runtime\tenancy\tenant-boundaries.sample.json'
)

$missingFiles = @()
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) { $missingFiles += $relativePath }
}

if (@($missingFiles).Count -gt 0) {
    Write-Host 'Missing expected P5.1.20 files:'
    foreach ($file in $missingFiles) { Write-Host " - $file" }
    exit 1
}

$coreProject = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (-not (Test-Path -LiteralPath $coreProject)) {
    throw "Missing project file: $coreProject"
}

$projectFiles = Get-ChildItem -Path $repoRoot -Recurse -Filter '*.csproj' -File |
    Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\' }

$inlineVersions = @()
foreach ($projectFile in $projectFiles) {
    [xml]$projectXml = Get-Content -LiteralPath $projectFile.FullName -Raw
    $itemGroups = Get-ChildElementArray -Node $projectXml.Project -Name 'ItemGroup'
    foreach ($itemGroup in $itemGroups) {
        $packageRefs = Get-ChildElementArray -Node $itemGroup -Name 'PackageReference'
        foreach ($packageRef in $packageRefs) {
            $version = Get-XmlAttributeValue -Node $packageRef -Name 'Version'
            if (-not [string]::IsNullOrWhiteSpace($version)) {
                $inlineVersions += $projectFile.FullName
            }
        }
    }
}

if (@($inlineVersions).Count -gt 0) {
    Write-Host 'Inline PackageReference Version attributes detected:'
    foreach ($path in (@($inlineVersions) | Sort-Object -Unique)) { Write-Host " - $path" }
    exit 1
}

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $coreProject
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

Write-Host 'Building MigrationBase.Core...'
dotnet build $coreProject --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }

Write-Host 'P5.1.20 Azure operational tenant boundaries validation passed.'
