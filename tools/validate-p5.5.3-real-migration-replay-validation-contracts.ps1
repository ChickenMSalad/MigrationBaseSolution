Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptDirectory = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $scriptPath = $MyInvocation.MyCommand.Path
    if ([string]::IsNullOrWhiteSpace($scriptPath)) { $scriptPath = $PSCommandPath }
    if ([string]::IsNullOrWhiteSpace($scriptPath)) { throw 'Unable to determine script path.' }
    $scriptDirectory = Split-Path -Parent $scriptPath
}

$repoRoot = Split-Path -Parent $scriptDirectory

function Join-RepoPath([string] $RelativePath) {
    return Join-Path $repoRoot $RelativePath
}

function Assert-FileExists([string] $RelativePath) {
    $path = Join-RepoPath $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Missing expected P5.5.3 file: $RelativePath"
    }
}

function Get-XmlAttributeValue {
    param(
        [Parameter(Mandatory = $true)] $Node,
        [Parameter(Mandatory = $true)] [string] $Name
    )

    if ($null -eq $Node) { return $null }
    if ($null -eq $Node.Attributes) { return $null }
    $attribute = $Node.Attributes[$Name]
    if ($null -eq $attribute) { return $null }
    return $attribute.Value
}

function Get-ProjectPackageReferences([string] $ProjectPath) {
    if (-not (Test-Path -LiteralPath $ProjectPath -PathType Leaf)) { return @() }

    [xml] $projectXml = Get-Content -LiteralPath $ProjectPath -Raw
    if ($null -eq $projectXml.Project) { return @() }

    $itemGroups = @()
    if ($projectXml.Project.PSObject.Properties['ItemGroup']) {
        $itemGroups = @($projectXml.Project.ItemGroup)
    }

    $packageReferences = @()
    foreach ($itemGroup in $itemGroups) {
        if ($null -eq $itemGroup) { continue }
        if (-not $itemGroup.PSObject.Properties['PackageReference']) { continue }
        foreach ($packageReference in @($itemGroup.PackageReference)) {
            if ($null -ne $packageReference) {
                $packageReferences += $packageReference
            }
        }
    }

    return @($packageReferences)
}

$expectedFiles = @(
    'src/Core/MigrationBase.Core/Cloud/Azure/ExecutionValidation/AzureReplayValidationDescriptor.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/ExecutionValidation/AzureReplayValidationMode.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/ExecutionValidation/AzureReplayValidationResult.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/ExecutionValidation/IAzureReplayValidationRegistry.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/ExecutionValidation/AzureReplayValidationRegistry.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/ExecutionValidation/AzureReplayValidationRules.cs',
    'config/azure-runtime/execution-validation/replay-validation.sample.json'
)

foreach ($expectedFile in $expectedFiles) {
    Assert-FileExists $expectedFile
}

$projectFiles = @(Get-ChildItem -LiteralPath $repoRoot -Recurse -Filter '*.csproj' -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' })

$inlineVersions = @()
foreach ($project in $projectFiles) {
    foreach ($packageReference in @(Get-ProjectPackageReferences -ProjectPath $project.FullName)) {
        $version = Get-XmlAttributeValue -Node $packageReference -Name 'Version'
        if (-not [string]::IsNullOrWhiteSpace($version)) {
            $inlineVersions += $project.FullName
            break
        }
    }
}

if (@($inlineVersions).Length -gt 0) {
    throw ("Inline PackageReference Version attributes detected:`n - " + (($inlineVersions | Sort-Object -Unique) -join "`n - "))
}

$coreProject = Join-RepoPath 'src/Core/MigrationBase.Core/MigrationBase.Core.csproj'
if (Test-Path -LiteralPath $coreProject -PathType Leaf) {
    Write-Host 'Restoring MigrationBase.Core...'
    dotnet restore $coreProject
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

    Write-Host 'Building MigrationBase.Core...'
    dotnet build $coreProject --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }
}
else {
    throw 'MigrationBase.Core project file is missing.'
}

Write-Host 'P5.5.3 real migration replay validation contract validation passed.'
