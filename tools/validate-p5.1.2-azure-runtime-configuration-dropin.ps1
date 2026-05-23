Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

$expectedFiles = @(
    'src/Core/Migration.Core.Azure/Configuration/AzureRuntimeConfigurationCompositionExtensions.cs',
    'src/Core/Migration.Core.Azure/Configuration/AzureRuntimeConfigurationCompositionOptions.cs',
    'src/Core/Migration.Core.Azure/Configuration/AzureRuntimeConfigurationSources.cs',
    'config/azure-runtime/appsettings.AzureRuntime.sample.json'
)

$missing = @()
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path $fullPath -PathType Leaf)) {
        $missing += $relativePath
    }
}

if ($missing.Count -gt 0) {
    Write-Host 'Missing expected P5.1.2 files:' -ForegroundColor Red
    foreach ($item in $missing) { Write-Host " - $item" -ForegroundColor Red }
    exit 1
}

$projectFiles = Get-ChildItem -Path $repoRoot -Filter '*.csproj' -Recurse | Where-Object {
    $_.FullName -notmatch '[\\/](bin|obj)[\\/]'
}

$violations = @()
foreach ($projectFile in $projectFiles) {
    [xml]$xml = Get-Content -Path $projectFile.FullName
    if (-not $xml.Project -or -not $xml.Project.PSObject.Properties['ItemGroup']) { continue }

    foreach ($itemGroup in @($xml.Project.ItemGroup)) {
        if (-not $itemGroup.PSObject.Properties['PackageReference']) { continue }

        foreach ($packageRef in @($itemGroup.PackageReference)) {
            if ($packageRef.PSObject.Properties['Version'] -and -not [string]::IsNullOrWhiteSpace($packageRef.Version)) {
                $violations += $projectFile.FullName
            }
        }
    }
}

if ($violations.Count -gt 0) {
    Write-Host 'Inline PackageReference Version attributes detected. Central package management convention may be violated:' -ForegroundColor Red
    foreach ($violation in ($violations | Sort-Object -Unique)) { Write-Host " - $violation" -ForegroundColor Red }
    exit 1
}

Write-Host 'P5.1.2 Azure runtime configuration composition drop-in validation passed.' -ForegroundColor Green
