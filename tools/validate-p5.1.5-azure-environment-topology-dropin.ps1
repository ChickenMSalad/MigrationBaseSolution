Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')

$expectedFiles = @(
    'src/Core/Migration.Core.Azure/Topology/AzureEnvironmentTopologyDescriptor.cs',
    'src/Core/Migration.Core.Azure/Topology/AzureEnvironmentTopologyRegistry.cs',
    'src/Core/Migration.Core.Azure/Topology/IAzureEnvironmentTopologyRegistry.cs',
    'src/Core/Migration.Core.Azure/Topology/AzureEnvironmentTopologyValidationResult.cs',
    'config/azure-runtime/topology/environments.sample.json'
)

$missing = @()
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        $missing += $relativePath
    }
}

if ($missing.Count -gt 0) {
    Write-Host 'Missing expected P5.1.5 files:' -ForegroundColor Red
    foreach ($item in $missing) {
        Write-Host " - $item" -ForegroundColor Red
    }
    exit 1
}

$projectFiles = Get-ChildItem -LiteralPath $repoRoot -Recurse -Filter '*.csproj' -File |
    Where-Object {
        $_.FullName -notmatch '\\bin\\' -and
        $_.FullName -notmatch '\\obj\\'
    }

$inlineVersionProjects = @()
foreach ($projectFile in $projectFiles) {
    [xml]$projectXml = Get-Content -LiteralPath $projectFile.FullName -Raw
    $inlineVersionNodes = $projectXml.SelectNodes('//PackageReference[@Version]')
    if ($null -ne $inlineVersionNodes -and $inlineVersionNodes.Count -gt 0) {
        $inlineVersionProjects += $projectFile.FullName
    }
}

if ($inlineVersionProjects.Count -gt 0) {
    Write-Host 'Inline PackageReference Version attributes detected. Central package management convention may be violated:' -ForegroundColor Red
    foreach ($project in $inlineVersionProjects) {
        Write-Host " - $project" -ForegroundColor Red
    }
    exit 1
}

Write-Host 'P5.1.5 Azure environment topology registry drop-in validation passed.' -ForegroundColor Green
