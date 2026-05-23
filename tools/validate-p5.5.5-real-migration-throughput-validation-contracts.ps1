Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptDirectory = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $scriptPath = $MyInvocation.MyCommand.Path
    if (-not [string]::IsNullOrWhiteSpace($scriptPath)) {
        $scriptDirectory = Split-Path -Parent $scriptPath
    }
}
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) { throw 'Unable to determine script directory.' }

$repoRoot = Resolve-Path (Join-Path $scriptDirectory '..')

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Validation\Throughput\AzureMigrationThroughputValidationProfile.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Validation\Throughput\AzureMigrationThroughputValidationResult.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Validation\Throughput\AzureMigrationThroughputCheckpoint.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Validation\Throughput\IAzureMigrationThroughputValidationRegistry.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Validation\Throughput\AzureMigrationThroughputValidationRegistry.cs',
    'config\azure-runtime\real-migration-validation\throughput.validation.sample.json'
)

$missing = @()
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) { $missing += $relativePath }
}
if (@($missing).Length -gt 0) {
    Write-Host 'Missing expected P5.5.5 files:' -ForegroundColor Red
    foreach ($file in $missing) { Write-Host " - $file" -ForegroundColor Red }
    exit 1
}

$projectPath = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (-not (Test-Path -LiteralPath $projectPath)) { throw "Missing project file: ${projectPath}" }

$projectFiles = @(Get-ChildItem -Path (Join-Path $repoRoot 'src') -Filter '*.csproj' -Recurse -File | Where-Object {
    $_.FullName -notmatch '\\bin\\' -and $_.FullName -notmatch '\\obj\\'
})

$badPackageRefs = @()
foreach ($project in $projectFiles) {
    [xml]$projectXml = Get-Content -LiteralPath $project.FullName -Raw
    $itemGroups = @()
    if ($projectXml.Project -and $projectXml.Project.PSObject.Properties['ItemGroup']) {
        $itemGroups = @($projectXml.Project.ItemGroup)
    }
    foreach ($itemGroup in $itemGroups) {
        if ($null -eq $itemGroup) { continue }
        if (-not $itemGroup.PSObject.Properties['PackageReference']) { continue }
        $packageRefs = @($itemGroup.PackageReference)
        foreach ($packageRef in $packageRefs) {
            if ($null -eq $packageRef) { continue }
            $hasVersionProperty = $false
            if ($packageRef.PSObject.Properties['Version']) { $hasVersionProperty = $true }
            $versionAttribute = $packageRef.GetAttribute('Version')
            if ($hasVersionProperty -or -not [string]::IsNullOrWhiteSpace($versionAttribute)) {
                $badPackageRefs += $project.FullName
                break
            }
        }
    }
}
if (@($badPackageRefs).Length -gt 0) {
    Write-Host 'Inline PackageReference Version attributes detected:' -ForegroundColor Red
    foreach ($bad in @($badPackageRefs | Sort-Object -Unique)) { Write-Host " - $bad" -ForegroundColor Red }
    exit 1
}

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $projectPath
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

Write-Host 'Building MigrationBase.Core...'
dotnet build $projectPath --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }

Write-Host 'P5.5.5 real migration throughput validation contract validation passed.' -ForegroundColor Green
