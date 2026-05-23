Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDirectory = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $scriptPath = $MyInvocation.MyCommand.Path
    if ([string]::IsNullOrWhiteSpace($scriptPath)) { $scriptDirectory = (Get-Location).Path }
    else { $scriptDirectory = Split-Path -Parent $scriptPath }
}

$repoRoot = Split-Path -Parent $scriptDirectory
$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Infrastructure\AzureInfrastructureClientKind.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Infrastructure\AzureInfrastructureClientDescriptor.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Infrastructure\AzureInfrastructureClientValidationIssue.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Infrastructure\AzureInfrastructureClientValidationResult.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Infrastructure\IAzureInfrastructureClientRegistry.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Infrastructure\AzureInfrastructureClientRegistry.cs',
    'config\azure-runtime\infrastructure\infrastructure-clients.sample.json'
)

$missing = @()
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) { $missing += $relativePath }
}

if (@($missing).Length -gt 0) {
    Write-Host 'Missing expected P6.2.1 files:' -ForegroundColor Red
    foreach ($path in $missing) { Write-Host " - $path" -ForegroundColor Red }
    throw 'P6.2.1 validation failed.'
}

$projectPath = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (-not (Test-Path -LiteralPath $projectPath)) { throw "Missing project file: ${projectPath}" }

$projectFiles = @(Get-ChildItem -Path (Join-Path $repoRoot 'src') -Filter '*.csproj' -Recurse -File | Where-Object {
    $_.FullName -notmatch '\\bin\\' -and $_.FullName -notmatch '\\obj\\'
})

foreach ($project in $projectFiles) {
    [xml]$projectXml = Get-Content -LiteralPath $project.FullName -Raw
    $itemGroups = @()
    if ($null -ne $projectXml.Project -and $projectXml.Project.PSObject.Properties['ItemGroup']) {
        $itemGroups = @($projectXml.Project.ItemGroup)
    }

    foreach ($itemGroup in $itemGroups) {
        if ($null -eq $itemGroup -or -not $itemGroup.PSObject.Properties['PackageReference']) { continue }
        foreach ($packageReference in @($itemGroup.PackageReference)) {
            if ($null -eq $packageReference) { continue }
            $versionProperty = $packageReference.PSObject.Properties['Version']
            $versionAttribute = $null
            if ($null -ne $packageReference.Attributes) {
                $attribute = $packageReference.Attributes['Version']
                if ($null -ne $attribute) { $versionAttribute = $attribute.Value }
            }
            if ($null -ne $versionProperty -or -not [string]::IsNullOrWhiteSpace($versionAttribute)) {
                throw "Inline PackageReference Version detected in ${($project.FullName)}"
            }
        }
    }
}

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $projectPath
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

Write-Host 'Building MigrationBase.Core...'
dotnet build $projectPath --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }

Write-Host 'P6.2.1 Azure infrastructure client boundary contracts validation passed.' -ForegroundColor Green
