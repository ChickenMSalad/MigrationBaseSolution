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
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Poison\AzureWorkerPoisonDisposition.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Poison\AzureWorkerPoisonClassification.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Poison\AzureWorkerPoisonWorkRecord.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Poison\IAzureWorkerPoisonClassifier.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Poison\AzureWorkerPoisonClassifier.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Poison\IAzureWorkerPoisonWorkSink.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Workers\Poison\AzureWorkerPoisonWorkOptions.cs',
    'config\azure-runtime\workers\poison-work.sample.json'
)

$missing = @()
foreach ($relativePath in $expectedFiles) {
    $fullPath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) { $missing += $relativePath }
}

if (@($missing).Length -gt 0) {
    Write-Host 'Missing expected P6.3.5 files:' -ForegroundColor Red
    foreach ($item in $missing) { Write-Host " - $item" -ForegroundColor Red }
    throw 'P6.3.5 validation failed.'
}

function Get-XmlAttributeValue {
    param(
        [Parameter(Mandatory=$true)] [object] $Node,
        [Parameter(Mandatory=$true)] [string] $Name
    )
    if ($null -eq $Node.Attributes) { return $null }
    $attribute = $Node.Attributes[$Name]
    if ($null -eq $attribute) { return $null }
    return $attribute.Value
}

$projectFiles = @(Get-ChildItem -Path $repoRoot -Filter '*.csproj' -Recurse | Where-Object {
    $_.FullName -notmatch '\\bin\\' -and $_.FullName -notmatch '\\obj\\'
})

$inlineVersions = @()
foreach ($project in $projectFiles) {
    [xml]$projectXml = Get-Content -LiteralPath $project.FullName
    $itemGroups = @()
    if ($projectXml.Project.PSObject.Properties['ItemGroup']) { $itemGroups = @($projectXml.Project.ItemGroup) }
    foreach ($itemGroup in $itemGroups) {
        if (-not $itemGroup.PSObject.Properties['PackageReference']) { continue }
        foreach ($packageRef in @($itemGroup.PackageReference)) {
            if ($null -eq $packageRef) { continue }
            $versionAttribute = Get-XmlAttributeValue -Node $packageRef -Name 'Version'
            if (-not [string]::IsNullOrWhiteSpace($versionAttribute)) {
                $inlineVersions += $project.FullName
            }
        }
    }
}

if (@($inlineVersions).Length -gt 0) {
    Write-Host 'Inline PackageReference Version attributes detected:' -ForegroundColor Red
    foreach ($path in @($inlineVersions | Sort-Object -Unique)) { Write-Host " - $path" -ForegroundColor Red }
    throw 'Central package management convention may be violated.'
}

$coreProject = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (-not (Test-Path -LiteralPath $coreProject)) { throw "Missing project: ${coreProject}" }

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $coreProject
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

Write-Host 'Building MigrationBase.Core...'
dotnet build $coreProject --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }

Write-Host 'P6.3.5 worker poison/abandonment foundation validation passed.' -ForegroundColor Green
