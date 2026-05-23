Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-ScriptDirectory {
    if (-not [string]::IsNullOrWhiteSpace($PSScriptRoot)) {
        return $PSScriptRoot
    }

    $invocationPath = $MyInvocation.MyCommand.Path
    if (-not [string]::IsNullOrWhiteSpace($invocationPath)) {
        return Split-Path -Parent $invocationPath
    }

    return (Get-Location).Path
}

function Assert-FileExists {
    param([Parameter(Mandatory = $true)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Missing expected P5.5.7 file: ${Path}"
    }
}

function Get-XmlAttributeValue {
    param(
        [Parameter(Mandatory = $false)]$Node,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if ($null -eq $Node) { return $null }
    if ($null -eq $Node.Attributes) { return $null }

    $attribute = $Node.Attributes[$Name]
    if ($null -eq $attribute) { return $null }

    return $attribute.Value
}

function Get-ProjectPackageReferences {
    param([Parameter(Mandatory = $true)][string]$ProjectPath)

    if (-not (Test-Path -LiteralPath $ProjectPath -PathType Leaf)) {
        return @()
    }

    [xml]$projectXml = Get-Content -LiteralPath $ProjectPath -Raw
    if ($null -eq $projectXml.Project) {
        return @()
    }

    $itemGroups = @()
    if ($projectXml.Project.PSObject.Properties['ItemGroup']) {
        $itemGroups = @($projectXml.Project.ItemGroup) | Where-Object { $null -ne $_ }
    }

    $references = @()
    foreach ($itemGroup in $itemGroups) {
        if ($null -eq $itemGroup) { continue }
        if (-not $itemGroup.PSObject.Properties['PackageReference']) { continue }

        foreach ($packageReference in @($itemGroup.PackageReference)) {
            if ($null -eq $packageReference) { continue }

            $include = Get-XmlAttributeValue -Node $packageReference -Name 'Include'
            $version = Get-XmlAttributeValue -Node $packageReference -Name 'Version'
            $references += [pscustomobject]@{
                Include = $include
                Version = $version
                ProjectPath = $ProjectPath
            }
        }
    }

    return @($references)
}

$scriptDirectory = Get-ScriptDirectory
$repoRoot = Split-Path -Parent $scriptDirectory

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\ExecutionValidation\LargeManifestValidationProfile.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\ExecutionValidation\LargeManifestValidationCheck.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\ExecutionValidation\LargeManifestValidationSeverity.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\ExecutionValidation\LargeManifestValidationResult.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\ExecutionValidation\LargeManifestValidationCheckResult.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\ExecutionValidation\LargeManifestValidationRegistry.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\ExecutionValidation\ILargeManifestValidationRegistry.cs',
    'config\real-migration-validation\large-manifest.validation.sample.json'
)

foreach ($relativePath in $expectedFiles) {
    Assert-FileExists -Path (Join-Path $repoRoot $relativePath)
}

$projectFiles = @(Get-ChildItem -LiteralPath $repoRoot -Recurse -Filter '*.csproj' -File | Where-Object {
    $_.FullName -notmatch '\\bin\\' -and $_.FullName -notmatch '\\obj\\'
})

$inlineVersions = @()
foreach ($project in $projectFiles) {
    foreach ($reference in @(Get-ProjectPackageReferences -ProjectPath $project.FullName)) {
        if ($null -ne $reference.Version -and -not [string]::IsNullOrWhiteSpace([string]$reference.Version)) {
            $inlineVersions += $reference
        }
    }
}

if (@($inlineVersions).Length -gt 0) {
    $messages = @($inlineVersions | ForEach-Object { " - $($_.ProjectPath): $($_.Include) has Version=$($_.Version)" })
    throw "Inline PackageReference Version attributes detected.`n$($messages -join [Environment]::NewLine)"
}

$samplePath = Join-Path $repoRoot 'config\real-migration-validation\large-manifest.validation.sample.json'
$sampleJson = Get-Content -LiteralPath $samplePath -Raw | ConvertFrom-Json
if ($null -eq $sampleJson.largeManifestValidation) {
    throw 'large-manifest.validation.sample.json is missing largeManifestValidation root object.'
}
if ($null -eq $sampleJson.checks) {
    throw 'large-manifest.validation.sample.json is missing checks array.'
}
if (@($sampleJson.checks).Length -lt 1) {
    throw 'large-manifest.validation.sample.json must include at least one check.'
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

Write-Host 'P5.5.7 real migration large manifest validation contract validation passed.'
