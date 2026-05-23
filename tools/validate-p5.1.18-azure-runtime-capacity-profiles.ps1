Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-ScriptDirectory {
    if ($PSScriptRoot -and $PSScriptRoot.Trim().Length -gt 0) { return $PSScriptRoot }
    if ($MyInvocation.MyCommand.Path -and $MyInvocation.MyCommand.Path.Trim().Length -gt 0) { return (Split-Path -Parent $MyInvocation.MyCommand.Path) }
    return (Get-Location).Path
}

function Get-RepoRoot {
    $scriptDirectory = Get-ScriptDirectory
    return (Resolve-Path (Join-Path $scriptDirectory '..')).Path
}

function Test-XmlAttributeExists {
    param(
        [Parameter(Mandatory=$false)] $Node,
        [Parameter(Mandatory=$true)] [string] $Name
    )

    if ($null -eq $Node) { return $false }
    if ($null -eq $Node.Attributes) { return $false }
    $attribute = $Node.Attributes[$Name]
    return $null -ne $attribute
}

$repoRoot = Get-RepoRoot
$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Capacity\AzureRuntimeCapacityProfile.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Capacity\AzureRuntimeCapacityRequirement.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Capacity\AzureRuntimeCapacityValidationResult.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Capacity\AzureRuntimeCapacityProfileValidator.cs',
    'config\azure-runtime\capacity\capacity-profiles.sample.json'
)

$missing = @()
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        $missing += $relativePath
    }
}

if (@($missing).Length -gt 0) {
    Write-Host 'Missing expected P5.1.18 files:' -ForegroundColor Red
    foreach ($file in $missing) { Write-Host " - $file" -ForegroundColor Red }
    exit 1
}

$projectFiles = @(Get-ChildItem -LiteralPath $repoRoot -Recurse -Filter '*.csproj' -File |
    Where-Object {
        $_.FullName -notmatch '\\bin\\' -and
        $_.FullName -notmatch '\\obj\\'
    })

$inlineVersionProjects = @()
foreach ($projectFile in $projectFiles) {
    [xml] $projectXml = Get-Content -LiteralPath $projectFile.FullName -Raw
    $itemGroups = @()
    if ($projectXml.Project -and $projectXml.Project.PSObject.Properties['ItemGroup']) {
        $itemGroups = @($projectXml.Project.ItemGroup)
    }

    foreach ($itemGroup in $itemGroups) {
        if ($null -eq $itemGroup) { continue }
        if (-not $itemGroup.PSObject.Properties['PackageReference']) { continue }
        $packageRefs = @($itemGroup.PackageReference)
        foreach ($packageRef in $packageRefs) {
            if ($null -eq $packageRef) { continue }
            if ((Test-XmlAttributeExists -Node $packageRef -Name 'Version') -or $packageRef.PSObject.Properties['Version']) {
                $inlineVersionProjects += $projectFile.FullName
                break
            }
        }
    }
}

if (@($inlineVersionProjects).Length -gt 0) {
    Write-Host 'Inline PackageReference Version attributes detected:' -ForegroundColor Red
    foreach ($project in @($inlineVersionProjects | Sort-Object -Unique)) { Write-Host " - $project" -ForegroundColor Red }
    exit 1
}

$coreProject = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (Test-Path -LiteralPath $coreProject -PathType Leaf) {
    Write-Host 'Restoring MigrationBase.Core...'
    dotnet restore $coreProject
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

    Write-Host 'Building MigrationBase.Core...'
    dotnet build $coreProject --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }
}

Write-Host 'P5.1.18 Azure runtime capacity profile validation passed.' -ForegroundColor Green
