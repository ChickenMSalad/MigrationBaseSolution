Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')

$expectedFiles = @(
    'src/Core/Migration.Core.Azure/ResourceNaming/AzureResourceNamingOptions.cs',
    'src/Core/Migration.Core.Azure/ResourceNaming/AzureResourceNameRequest.cs',
    'src/Core/Migration.Core.Azure/ResourceNaming/AzureResourceNameResult.cs',
    'src/Core/Migration.Core.Azure/ResourceNaming/IAzureResourceNameBuilder.cs',
    'src/Core/Migration.Core.Azure/ResourceNaming/AzureResourceNameBuilder.cs',
    'src/Core/Migration.Core.Azure/ResourceNaming/AzureResourceTagOptions.cs',
    'src/Core/Migration.Core.Azure/ResourceNaming/IAzureResourceTagBuilder.cs',
    'src/Core/Migration.Core.Azure/ResourceNaming/AzureResourceTagBuilder.cs',
    'src/Core/Migration.Core.Azure/ResourceNaming/AzureResourceNamingServiceCollectionExtensions.cs',
    'config/azure-runtime/naming/resource-naming.sample.json',
    'tools/validate-p5.1.6-azure-resource-naming-dropin.ps1'
)

$missing = New-Object System.Collections.Generic.List[string]
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        [void]$missing.Add($relativePath)
    }
}

if ($missing.Count -gt 0) {
    Write-Host 'Missing expected P5.1.6 files:' -ForegroundColor Red
    foreach ($item in $missing) { Write-Host " - $item" -ForegroundColor Red }
    exit 1
}

$sourceFiles = Get-ChildItem -LiteralPath $repoRoot -Recurse -Filter '*.csproj' |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }

$inlinePackageVersions = New-Object System.Collections.Generic.List[string]
foreach ($projectFile in $sourceFiles) {
    [xml]$projectXml = Get-Content -LiteralPath $projectFile.FullName -Raw
    if (-not $projectXml.PSObject.Properties['Project']) { continue }
    $project = $projectXml.Project
    if (-not $project.PSObject.Properties['ItemGroup']) { continue }

    foreach ($itemGroup in @($project.ItemGroup)) {
        if ($null -eq $itemGroup) { continue }
        if (-not $itemGroup.PSObject.Properties['PackageReference']) { continue }

        foreach ($packageRef in @($itemGroup.PackageReference)) {
            if ($null -eq $packageRef) { continue }
            $hasVersionElement = $packageRef.PSObject.Properties['Version']
            $hasVersionAttribute = $false
            if ($packageRef.PSObject.Properties['Attributes'] -and $null -ne $packageRef.Attributes) {
                $versionAttribute = $packageRef.Attributes['Version']
                $hasVersionAttribute = $null -ne $versionAttribute
            }
            if ($hasVersionElement -or $hasVersionAttribute) {
                [void]$inlinePackageVersions.Add($projectFile.FullName)
                break
            }
        }
    }
}

if ($inlinePackageVersions.Count -gt 0) {
    Write-Host 'Inline PackageReference Version attributes detected. Central package management convention may be violated:' -ForegroundColor Red
    foreach ($path in ($inlinePackageVersions | Sort-Object -Unique)) { Write-Host " - $path" -ForegroundColor Red }
    exit 1
}

try {
    $jsonPath = Join-Path $repoRoot 'config/azure-runtime/naming/resource-naming.sample.json'
    $null = Get-Content -LiteralPath $jsonPath -Raw | ConvertFrom-Json
}
catch {
    Write-Host 'resource-naming.sample.json is not valid JSON.' -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

Write-Host 'P5.1.6 Azure resource naming drop-in validation passed.' -ForegroundColor Green
