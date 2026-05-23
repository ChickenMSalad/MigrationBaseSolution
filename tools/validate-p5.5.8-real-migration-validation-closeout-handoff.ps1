Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-ScriptDirectory {
    if (-not [string]::IsNullOrWhiteSpace($PSScriptRoot)) { return $PSScriptRoot }
    if ($MyInvocation.MyCommand.Path) { return Split-Path -Parent $MyInvocation.MyCommand.Path }
    return (Get-Location).Path
}

function Get-RepositoryRoot {
    $scriptDirectory = Get-ScriptDirectory
    return (Resolve-Path (Join-Path $scriptDirectory '..')).Path
}

function Assert-FileExists {
    param([Parameter(Mandatory=$true)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Missing expected file: $Path"
    }
}

function Get-ProjectPackageReferenceNodes {
    param([Parameter(Mandatory=$true)][string]$ProjectPath)

    [xml]$projectXml = Get-Content -LiteralPath $ProjectPath -Raw
    $projectNode = $projectXml.Project
    if ($null -eq $projectNode) { return @() }

    $itemGroups = @()
    if ($projectNode.PSObject.Properties['ItemGroup']) {
        $itemGroups = @($projectNode.ItemGroup) | Where-Object { $null -ne $_ }
    }

    $packageReferences = @()
    foreach ($itemGroup in $itemGroups) {
        if ($null -ne $itemGroup -and $itemGroup.PSObject.Properties['PackageReference']) {
            $packageReferences += @($itemGroup.PackageReference) | Where-Object { $null -ne $_ }
        }
    }

    return @($packageReferences)
}

function Get-XmlAttributeValue {
    param(
        [Parameter(Mandatory=$false)]$Node,
        [Parameter(Mandatory=$true)][string]$Name
    )

    if ($null -eq $Node) { return $null }
    if ($null -eq $Node.Attributes) { return $null }

    $attribute = $Node.Attributes[$Name]
    if ($null -eq $attribute) { return $null }
    return $attribute.Value
}

$repoRoot = Get-RepositoryRoot
$expectedFiles = @(
    'src/Core/MigrationBase.Core/Cloud/Azure/RealMigrationValidation/Closeout/RealMigrationValidationCloseoutStatus.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/RealMigrationValidation/Closeout/RealMigrationValidationCloseoutCriterion.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/RealMigrationValidation/Closeout/RealMigrationValidationCloseoutDescriptor.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/RealMigrationValidation/Closeout/RealMigrationValidationHandoffDescriptor.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/RealMigrationValidation/Closeout/RealMigrationValidationCloseoutSummary.cs',
    'config/azure-runtime/real-migration-validation/closeout-handoff.sample.json'
)

foreach ($relativePath in $expectedFiles) {
    Assert-FileExists -Path (Join-Path $repoRoot $relativePath)
}

$projectFiles = @(Get-ChildItem -LiteralPath $repoRoot -Recurse -Filter '*.csproj' -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' })

$inlineVersionViolations = @()
foreach ($project in $projectFiles) {
    foreach ($packageReference in @(Get-ProjectPackageReferenceNodes -ProjectPath $project.FullName)) {
        $version = Get-XmlAttributeValue -Node $packageReference -Name 'Version'
        if (-not [string]::IsNullOrWhiteSpace($version)) {
            $inlineVersionViolations += $project.FullName
        }
    }
}

if (@($inlineVersionViolations).Length -gt 0) {
    Write-Host 'Inline PackageReference Version attributes detected:' -ForegroundColor Red
    @($inlineVersionViolations | Sort-Object -Unique) | ForEach-Object { Write-Host " - $_" -ForegroundColor Red }
    throw 'Central package management convention violation detected.'
}

$coreProject = Join-Path $repoRoot 'src/Core/MigrationBase.Core/MigrationBase.Core.csproj'
Assert-FileExists -Path $coreProject

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $coreProject
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

Write-Host 'Building MigrationBase.Core...'
dotnet build $coreProject --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }

Write-Host 'P5.5.8 real migration validation closeout/handoff validation passed.' -ForegroundColor Green
