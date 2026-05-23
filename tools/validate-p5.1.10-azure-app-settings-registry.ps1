Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $scriptDirectory = Split-Path -Parent $MyInvocation.ScriptName
    return (Resolve-Path (Join-Path $scriptDirectory '..')).Path
}

function Assert-FileExists {
    param([Parameter(Mandatory=$true)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Missing expected file: $Path"
    }
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

$repoRoot = Get-RepoRoot
$coreProject = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Configuration\AzureAppSettingDescriptor.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Configuration\AzureAppSettingRegistry.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Configuration\IAzureAppSettingRegistry.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Configuration\AzureAppSettingRequirement.cs',
    'config\azure-runtime\app-settings\app-settings.registry.sample.json'
)

$missing = New-Object System.Collections.Generic.List[string]
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        [void]$missing.Add($relativePath)
    }
}

if ($missing.Count -gt 0) {
    Write-Host 'Missing expected P5.1.10 files:'
    foreach ($item in $missing) { Write-Host " - $item" }
    exit 1
}

Assert-FileExists -Path $coreProject

$projectFiles = Get-ChildItem -LiteralPath $repoRoot -Recurse -Filter '*.csproj' -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }

$inlineVersionHits = New-Object System.Collections.Generic.List[string]
foreach ($projectFile in $projectFiles) {
    [xml]$projectXml = Get-Content -LiteralPath $projectFile.FullName -Raw
    if ($null -eq $projectXml.Project) { continue }
    if (-not $projectXml.Project.PSObject.Properties['ItemGroup']) { continue }

    foreach ($itemGroup in @($projectXml.Project.ItemGroup)) {
        if ($null -eq $itemGroup) { continue }
        if (-not $itemGroup.PSObject.Properties['PackageReference']) { continue }

        foreach ($packageRef in @($itemGroup.PackageReference)) {
            if ($null -eq $packageRef) { continue }
            $versionAttribute = Get-XmlAttributeValue -Node $packageRef -Name 'Version'
            $versionElement = $null
            if ($packageRef.PSObject.Properties['Version']) { $versionElement = $packageRef.Version }
            if (-not [string]::IsNullOrWhiteSpace($versionAttribute) -or $null -ne $versionElement) {
                [void]$inlineVersionHits.Add($projectFile.FullName)
            }
        }
    }
}

if ($inlineVersionHits.Count -gt 0) {
    Write-Host 'Inline PackageReference Version attributes/elements detected:'
    foreach ($hit in ($inlineVersionHits | Sort-Object -Unique)) { Write-Host " - $hit" }
    exit 1
}

Push-Location $repoRoot
try {
    Write-Host 'Restoring MigrationBase.Core...'
    dotnet restore '.\src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

    Write-Host 'Building MigrationBase.Core...'
    dotnet build '.\src\Core\MigrationBase.Core\MigrationBase.Core.csproj' --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }
}
finally {
    Pop-Location
}

Write-Host 'P5.1.10 Azure app settings registry validation passed.'
