Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot

function Assert-FileExists {
    param([Parameter(Mandatory=$true)][string]$RelativePath)
    $path = Join-Path $repoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Missing expected P5.1.8 file: $RelativePath"
    }
}

$expectedFiles = @(
    'src/Core/MigrationBase.Core/Cloud/Azure/Hosting/AzureHostRoleKind.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Hosting/AzureHostWorkloadCapability.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Hosting/AzureHostRoleDescriptor.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Hosting/AzureHostRoleRegistry.cs',
    'src/Core/MigrationBase.Core/Cloud/Azure/Hosting/AzureHostRoleDefaults.cs',
    'config/azure-runtime/hosting/host-roles.sample.json'
)

foreach ($relativePath in $expectedFiles) {
    Assert-FileExists -RelativePath $relativePath
}

$projectPath = Join-Path $repoRoot 'src/Core/MigrationBase.Core/MigrationBase.Core.csproj'
if (Test-Path -LiteralPath $projectPath -PathType Leaf) {
    Write-Host 'Restoring MigrationBase.Core...'
    dotnet restore $projectPath
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

    Write-Host 'Building MigrationBase.Core...'
    dotnet build $projectPath --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }
}
else {
    Write-Host 'MigrationBase.Core.csproj not found. Source file presence validated only.'
}

Write-Host 'P5.1.8 Azure host role descriptor validation passed.'
