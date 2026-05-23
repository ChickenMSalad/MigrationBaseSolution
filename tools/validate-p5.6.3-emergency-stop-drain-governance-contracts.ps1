Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptDirectory = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $PSCommandPath }
$repoRoot = Split-Path -Parent $scriptDirectory

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Governance\AzureEmergencyStopMode.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Governance\AzureEmergencyStopDescriptor.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Governance\AzureDrainRequestDescriptor.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Governance\AzureEmergencyStopValidationResult.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Governance\IAzureEmergencyStopPolicy.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Governance\AzureEmergencyStopPolicy.cs',
    'config\azure-runtime\governance\emergency-stop.sample.json'
)

$missing = @()
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        $missing += $relativePath
    }
}

if (@($missing).Length -gt 0) {
    Write-Host 'Missing expected P5.6.3 files:' -ForegroundColor Red
    foreach ($file in $missing) { Write-Host " - $file" -ForegroundColor Red }
    exit 1
}

$projectPath = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "Missing project file: ${projectPath}"
}

$sourceFiles = Get-ChildItem -Path (Join-Path $repoRoot 'src') -Recurse -File -Include '*.csproj' |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }

foreach ($project in @($sourceFiles)) {
    [xml]$xml = Get-Content -LiteralPath $project.FullName -Raw
    $projectNode = $xml.Project
    if ($null -eq $projectNode) { continue }
    $itemGroups = @()
    if ($projectNode.PSObject.Properties['ItemGroup']) { $itemGroups = @($projectNode.ItemGroup) }
    foreach ($itemGroup in @($itemGroups)) {
        if ($null -eq $itemGroup) { continue }
        if (-not $itemGroup.PSObject.Properties['PackageReference']) { continue }
        foreach ($packageRef in @($itemGroup.PackageReference)) {
            if ($null -eq $packageRef) { continue }
            $hasVersionElement = $packageRef.PSObject.Properties['Version'] -and -not [string]::IsNullOrWhiteSpace([string]$packageRef.Version)
            $hasVersionAttribute = $packageRef.HasAttribute('Version')
            if ($hasVersionElement -or $hasVersionAttribute) {
                throw "Inline PackageReference Version attribute detected in ${($project.FullName)}"
            }
        }
    }
}

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $projectPath
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

Write-Host 'Building MigrationBase.Core...'
dotnet build $projectPath --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }

Write-Host 'P5.6.3 emergency stop and drain governance validation passed.' -ForegroundColor Green
