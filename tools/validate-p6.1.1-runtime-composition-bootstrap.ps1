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
if (-not (Test-Path (Join-Path $repoRoot 'MigrationBaseSolution.sln'))) {
    throw "Unable to locate repo root from ${scriptDirectory}."
}

$expectedFiles = @(
    'src/Core/MigrationBase.Core/Cloud/Azure/Runtime/Composition/AzureRuntimeCompositionStepKind.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Runtime/Composition/AzureRuntimeCompositionStep.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Runtime/Composition/AzureRuntimeCompositionPlan.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Runtime/Composition/AzureRuntimeCompositionValidationResult.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Runtime/Composition/IAzureRuntimeCompositionPlanner.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Runtime/Composition/AzureRuntimeCompositionPlanner.cs',
    'config/azure-runtime/composition/runtime-composition.sample.json'
)

$missingFiles = @()
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path $fullPath)) {
        $missingFiles += $relativePath
    }
}

if (@($missingFiles).Length -gt 0) {
    Write-Host 'Missing expected P6.1.1 files:'
    foreach ($missingFile in $missingFiles) {
        Write-Host " - $missingFile"
    }
    throw 'P6.1.1 validation failed because expected files are missing.'
}

$projectFiles = @(Get-ChildItem -Path (Join-Path $repoRoot 'src') -Recurse -Filter '*.csproj' -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' })

$inlineVersionProjects = @()
foreach ($projectFile in $projectFiles) {
    $projectText = Get-Content -Path $projectFile.FullName -Raw
    if ($projectText -match '<PackageReference\s+[^>]*\bVersion\s*=') {
        $inlineVersionProjects += $projectFile.FullName
    }
}

if (@($inlineVersionProjects).Length -gt 0) {
    Write-Host 'Inline PackageReference Version attributes detected:'
    foreach ($project in $inlineVersionProjects) {
        Write-Host " - $project"
    }
    throw 'Central package management convention may be violated.'
}

$projectPath = Join-Path $repoRoot 'src/Core/MigrationBase.Core/MigrationBase.Core.csproj'
if (-not (Test-Path $projectPath)) {
    throw "Missing project file: ${projectPath}"
}

Push-Location $repoRoot
try {
    Write-Host 'Restoring MigrationBase.Core...'
    dotnet restore $projectPath --nologo
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

    Write-Host 'Building MigrationBase.Core...'
    dotnet build $projectPath --no-restore --nologo
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }
}
finally {
    Pop-Location
}

Write-Host 'P6.1.1 runtime composition bootstrap validation passed.'
