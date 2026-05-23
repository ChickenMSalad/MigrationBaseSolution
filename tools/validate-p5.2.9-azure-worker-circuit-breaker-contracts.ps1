Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptDirectory = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $scriptPath = $MyInvocation.MyCommand.Path
    if ([string]::IsNullOrWhiteSpace($scriptPath)) {
        $scriptDirectory = (Get-Location).Path
    }
    else {
        $scriptDirectory = Split-Path -Parent $scriptPath
    }
}

$repoRoot = Split-Path -Parent $scriptDirectory

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\CircuitBreakers\AzureWorkerCircuitBreakerState.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\CircuitBreakers\AzureWorkerCircuitBreakerScope.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\CircuitBreakers\AzureWorkerCircuitBreakerPolicy.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\CircuitBreakers\AzureWorkerCircuitBreakerSnapshot.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\CircuitBreakers\IAzureWorkerCircuitBreakerRegistry.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\CircuitBreakers\AzureWorkerCircuitBreakerRegistry.cs',
    'config\azure-runtime\workers\circuit-breakers.sample.json'
)

$missing = @()
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        $missing += $relativePath
    }
}

if (@($missing).Length -gt 0) {
    Write-Host 'Missing expected P5.2.9 files:' -ForegroundColor Red
    foreach ($item in $missing) { Write-Host " - $item" -ForegroundColor Red }
    throw 'P5.2.9 validation failed.'
}

$projectPath = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "Missing project file: ${projectPath}"
}

$projectFiles = @(Get-ChildItem -Path (Join-Path $repoRoot 'src') -Filter '*.csproj' -Recurse -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' })

$inlineVersions = @()
foreach ($projectFile in $projectFiles) {
    [xml]$projectXml = Get-Content -LiteralPath $projectFile.FullName -Raw
    $itemGroups = @()
    if ($projectXml.PSObject.Properties['Project'] -and $projectXml.Project.PSObject.Properties['ItemGroup']) {
        $itemGroups = @($projectXml.Project.ItemGroup)
    }

    foreach ($itemGroup in $itemGroups) {
        if ($null -eq $itemGroup) { continue }
        if (-not $itemGroup.PSObject.Properties['PackageReference']) { continue }

        $packageRefs = @($itemGroup.PackageReference)
        foreach ($packageRef in $packageRefs) {
            if ($null -eq $packageRef) { continue }
            $hasVersionAttribute = $false
            if ($packageRef.PSObject.Properties['Version']) { $hasVersionAttribute = $true }
            if ($packageRef.Attributes -and $packageRef.Attributes['Version']) { $hasVersionAttribute = $true }
            if ($hasVersionAttribute) {
                $inlineVersions += $projectFile.FullName
            }
        }
    }
}

if (@($inlineVersions).Length -gt 0) {
    Write-Host 'Inline PackageReference Version attributes detected:' -ForegroundColor Red
    foreach ($file in (@($inlineVersions) | Sort-Object -Unique)) { Write-Host " - $file" -ForegroundColor Red }
    throw 'Central package management convention may be violated.'
}

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $projectPath
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

Write-Host 'Building MigrationBase.Core...'
dotnet build $projectPath --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }

Write-Host 'P5.2.9 Azure worker circuit breaker contract validation passed.' -ForegroundColor Green
