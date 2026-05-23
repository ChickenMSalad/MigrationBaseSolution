Set-StrictMode -Version Latest
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

$repoRoot = Resolve-Path (Join-Path $scriptDirectory '..')

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Infrastructure\Identity\AzureCredentialResolutionMode.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Infrastructure\Identity\AzureCredentialBoundary.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Infrastructure\Identity\IAzureCredentialBoundaryRegistry.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Infrastructure\Identity\AzureCredentialBoundaryRegistry.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Infrastructure\Identity\AzureCredentialBoundaryValidationResult.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Infrastructure\Identity\AzureCredentialBoundaryValidator.cs',
    'config\azure-runtime\identity\credential-boundaries.sample.json'
)

$missing = @()
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        $missing += $relativePath
    }
}

if (@($missing).Length -gt 0) {
    Write-Host 'Missing expected P6.2.2 files:'
    foreach ($file in $missing) { Write-Host " - $file" }
    throw 'P6.2.2 validation failed.'
}

$projectPath = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "Missing project file: ${projectPath}"
}

$projectFiles = @(Get-ChildItem -Path (Join-Path $repoRoot 'src') -Filter '*.csproj' -Recurse | Where-Object {
    $_.FullName -notmatch '\\bin\\' -and $_.FullName -notmatch '\\obj\\'
})

$badPackageRefs = @()
foreach ($project in $projectFiles) {
    [xml]$projectXml = Get-Content -LiteralPath $project.FullName
    $itemGroups = @()
    if ($projectXml.Project.PSObject.Properties.Name -contains 'ItemGroup') {
        $itemGroups = @($projectXml.Project.ItemGroup)
    }

    foreach ($itemGroup in $itemGroups) {
        if ($null -eq $itemGroup) { continue }
        if (-not ($itemGroup.PSObject.Properties.Name -contains 'PackageReference')) { continue }
        $packageRefs = @($itemGroup.PackageReference)
        foreach ($packageRef in $packageRefs) {
            if ($null -eq $packageRef) { continue }
            $versionAttr = $packageRef.Attributes['Version']
            if ($null -ne $versionAttr) {
                $badPackageRefs += $project.FullName
                break
            }
        }
    }
}

if (@($badPackageRefs).Length -gt 0) {
    Write-Host 'Inline PackageReference Version attributes detected:'
    foreach ($path in $badPackageRefs) { Write-Host " - $path" }
    throw 'Central package management validation failed.'
}

Push-Location $repoRoot
try {
    Write-Host 'Restoring MigrationBase.Core...'
    dotnet restore $projectPath
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

    Write-Host 'Building MigrationBase.Core...'
    dotnet build $projectPath --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }
}
finally {
    Pop-Location
}

Write-Host 'P6.2.2 Azure identity credential boundary contract validation passed.'
