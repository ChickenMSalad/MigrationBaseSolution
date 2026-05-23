Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot

$requiredFiles = @(
    'src/Core/MigrationBase.Core/Cloud/Azure/AzureRuntimeOptions.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/RuntimeEnvironmentOptions.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/AzureIdentityOptions.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/AzureSqlOperationalStoreOptions.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/AzureArtifactStorageOptions.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/AzureQueueTopologyOptions.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/AzureTelemetryOptions.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/AzureRuntimeOptionsValidator.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/AzureRuntimeServiceCollectionExtensions.cs',
    'config/appsettings.P5.AzureRuntime.sample.json'
)

$missing = @()
foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        $missing += $relativePath
    }
}

if ($missing.Count -gt 0) {
    Write-Host 'Missing P5.1.1 files:' -ForegroundColor Red
    foreach ($item in $missing) { Write-Host " - $item" -ForegroundColor Red }
    exit 1
}

$csprojFiles = Get-ChildItem -LiteralPath $repoRoot -Recurse -Filter '*.csproj' -ErrorAction SilentlyContinue
$inlineVersions = @()
foreach ($file in $csprojFiles) {
    [xml]$xml = Get-Content -LiteralPath $file.FullName
    if ($null -eq $xml.Project) { continue }

    $packageRefs = $xml.Project.SelectNodes('//PackageReference')
    foreach ($packageRef in $packageRefs) {
        if ($packageRef.PSObject.Properties.Name -contains 'Version') {
            $inlineVersions += $file.FullName
            break
        }
        if ($packageRef.Attributes -and $packageRef.Attributes['Version']) {
            $inlineVersions += $file.FullName
            break
        }
    }
}

if ($inlineVersions.Count -gt 0) {
    Write-Host 'Inline PackageReference Version attributes detected. Central package management convention may be violated:' -ForegroundColor Red
    foreach ($item in ($inlineVersions | Select-Object -Unique)) { Write-Host " - $item" -ForegroundColor Red }
    exit 1
}

Write-Host 'P5.1.1 Azure runtime topology drop-in validation passed.' -ForegroundColor Green
