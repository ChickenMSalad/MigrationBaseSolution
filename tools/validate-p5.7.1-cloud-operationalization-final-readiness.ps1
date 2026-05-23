Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDirectory = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $scriptPath = $PSCommandPath
    if ([string]::IsNullOrWhiteSpace($scriptPath)) { $scriptPath = $MyInvocation.MyCommand.Path }
    if ([string]::IsNullOrWhiteSpace($scriptPath)) { throw 'Unable to resolve script path.' }
    $scriptDirectory = Split-Path -Parent $scriptPath
}

$repoRoot = Resolve-Path (Join-Path $scriptDirectory '..')

function Assert-FileExists {
    param([Parameter(Mandatory = $true)][string]$RelativePath)
    $fullPath = Join-Path $repoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Missing expected file: ${RelativePath}"
    }
}

function Get-ProjectPackageReferences {
    param([Parameter(Mandatory = $true)][string]$ProjectPath)

    [xml]$projectXml = Get-Content -LiteralPath $ProjectPath -Raw
    $projectNode = $projectXml.Project
    if ($null -eq $projectNode) { return @() }
    if (-not $projectNode.PSObject.Properties['ItemGroup']) { return @() }

    $results = New-Object System.Collections.Generic.List[object]
    foreach ($itemGroup in @($projectNode.ItemGroup)) {
        if ($null -eq $itemGroup) { continue }
        if (-not $itemGroup.PSObject.Properties['PackageReference']) { continue }
        foreach ($packageReference in @($itemGroup.PackageReference)) {
            if ($null -eq $packageReference) { continue }
            $include = $null
            $version = $null
            if ($packageReference.PSObject.Properties['Include']) { $include = [string]$packageReference.Include }
            if ($packageReference.PSObject.Properties['Version']) { $version = [string]$packageReference.Version }
            $results.Add([pscustomobject]@{ Include = $include; Version = $version }) | Out-Null
        }
    }

    return @($results.ToArray())
}

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Readiness\AzureOperationalizationReadinessCategory.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Readiness\AzureOperationalizationReadinessStatus.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Readiness\AzureOperationalizationReadinessItem.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Readiness\AzureOperationalizationReadinessSnapshot.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Readiness\IAzureOperationalizationReadinessRegistry.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Readiness\AzureOperationalizationReadinessRegistry.cs',
    'config\azure-runtime\readiness\p5-final-readiness.sample.json'
)

foreach ($expectedFile in $expectedFiles) { Assert-FileExists -RelativePath $expectedFile }

$projectFiles = @(Get-ChildItem -LiteralPath $repoRoot -Filter '*.csproj' -Recurse |
    Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\' })

$inlineVersions = New-Object System.Collections.Generic.List[string]
foreach ($project in $projectFiles) {
    foreach ($reference in @(Get-ProjectPackageReferences -ProjectPath $project.FullName)) {
        if ($null -ne $reference -and -not [string]::IsNullOrWhiteSpace($reference.Version)) {
            $inlineVersions.Add($project.FullName) | Out-Null
            break
        }
    }
}

if (@($inlineVersions.ToArray()).Length -gt 0) {
    throw ("Inline PackageReference Version attributes detected:`n - " + ((@($inlineVersions.ToArray()) | Sort-Object -Unique) -join "`n - "))
}

$coreProject = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (Test-Path -LiteralPath $coreProject -PathType Leaf) {
    Write-Host 'Restoring MigrationBase.Core...'
    dotnet restore $coreProject
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

    Write-Host 'Building MigrationBase.Core...'
    dotnet build $coreProject --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }
}
else {
    Write-Host 'MigrationBase.Core project not found; skipping project build validation.'
}

Write-Host 'P5.7.1 cloud operationalization final readiness validation passed.'
