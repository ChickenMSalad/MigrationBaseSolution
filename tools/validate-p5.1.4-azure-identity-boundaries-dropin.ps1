Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot

$expectedFiles = @(
    'src/Core/Migration.Core.Azure/Identity/AzureRuntimeIdentityMode.cs',
    'src/Core/Migration.Core.Azure/Identity/AzureRuntimeSecretSource.cs',
    'src/Core/Migration.Core.Azure/Identity/AzureRuntimeIdentityOptions.cs',
    'src/Core/Migration.Core.Azure/Identity/AzureRuntimeSecretBoundaryOptions.cs',
    'src/Core/Migration.Core.Azure/Identity/AzureRuntimeIdentityValidation.cs',
    'config/azure-runtime/appsettings.AzureIdentity.sample.json'
)

$missing = @()
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        $missing += $relativePath
    }
}

if ($missing.Count -gt 0) {
    Write-Host 'Missing expected P5.1.4 files:' -ForegroundColor Red
    foreach ($item in $missing) { Write-Host " - $item" -ForegroundColor Red }
    exit 1
}

$projectFiles = Get-ChildItem -LiteralPath $repoRoot -Recurse -Filter '*.csproj' -File |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }

$inlineVersionViolations = @()
foreach ($projectFile in $projectFiles) {
    [xml]$projectXml = Get-Content -LiteralPath $projectFile.FullName -Raw
    if (-not $projectXml.Project) { continue }
    if (-not $projectXml.Project.PSObject.Properties['ItemGroup']) { continue }

    foreach ($itemGroup in @($projectXml.Project.ItemGroup)) {
        if (-not $itemGroup.PSObject.Properties['PackageReference']) { continue }

        foreach ($packageReference in @($itemGroup.PackageReference)) {
            if ($packageReference.PSObject.Properties['Version']) {
                $inlineVersionViolations += $projectFile.FullName
                break
            }
        }
    }
}

if ($inlineVersionViolations.Count -gt 0) {
    Write-Host 'Inline PackageReference Version attributes detected. Central package management convention may be violated:' -ForegroundColor Red
    foreach ($path in ($inlineVersionViolations | Sort-Object -Unique)) { Write-Host " - $path" -ForegroundColor Red }
    exit 1
}

Write-Host 'P5.1.4 Azure identity and secret-boundary drop-in validation passed.' -ForegroundColor Green
