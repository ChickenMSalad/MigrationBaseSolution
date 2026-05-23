Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$expectedFiles = @(
    'README-P5.1.3.md',
    'docs/p5/P5.1.3-azure-hosting-profiles.md',
    'config/azure-runtime/azure-hosting-profiles.sample.json',
    'config/azure-runtime/environments/appsettings.AzureRuntime.Development.sample.json',
    'config/azure-runtime/environments/appsettings.AzureRuntime.Test.sample.json',
    'config/azure-runtime/environments/appsettings.AzureRuntime.Production.sample.json'
)

$missing = @()
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        $missing += $relativePath
    }
}

if ($missing.Count -gt 0) {
    Write-Host 'Missing expected P5.1.3 files:' -ForegroundColor Red
    foreach ($item in $missing) { Write-Host " - $item" -ForegroundColor Red }
    exit 1
}

$jsonFiles = Get-ChildItem -LiteralPath (Join-Path $repoRoot 'config/azure-runtime') -Filter '*.json' -Recurse |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }

foreach ($jsonFile in $jsonFiles) {
    try {
        Get-Content -LiteralPath $jsonFile.FullName -Raw | ConvertFrom-Json | Out-Null
    }
    catch {
        Write-Host "Invalid JSON: $($jsonFile.FullName)" -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor Red
        exit 1
    }
}

$projectFiles = Get-ChildItem -LiteralPath $repoRoot -Filter '*.csproj' -Recurse |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }

$inlineVersionHits = @()
foreach ($projectFile in $projectFiles) {
    [xml]$projectXml = Get-Content -LiteralPath $projectFile.FullName -Raw
    if ($null -eq $projectXml.Project -or -not ($projectXml.Project.PSObject.Properties.Name -contains 'ItemGroup')) { continue }

    foreach ($itemGroup in @($projectXml.Project.ItemGroup)) {
        if ($null -eq $itemGroup -or -not ($itemGroup.PSObject.Properties.Name -contains 'PackageReference')) { continue }
        foreach ($packageReference in @($itemGroup.PackageReference)) {
            if ($null -ne $packageReference -and $packageReference.PSObject.Properties.Name -contains 'Version') {
                $inlineVersionHits += $projectFile.FullName
                break
            }
        }
    }
}

if ($inlineVersionHits.Count -gt 0) {
    Write-Host 'Inline PackageReference Version attributes detected in source project files:' -ForegroundColor Red
    foreach ($hit in ($inlineVersionHits | Sort-Object -Unique)) { Write-Host " - $hit" -ForegroundColor Red }
    exit 1
}

Write-Host 'P5.1.3 Azure hosting profiles drop-in validation passed.' -ForegroundColor Green
