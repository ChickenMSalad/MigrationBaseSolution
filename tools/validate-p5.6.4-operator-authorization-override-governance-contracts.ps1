Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptDirectory = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $scriptPath = $MyInvocation.MyCommand.Path
    if ([string]::IsNullOrWhiteSpace($scriptPath)) { $scriptPath = $PSCommandPath }
    if ([string]::IsNullOrWhiteSpace($scriptPath)) { throw 'Unable to determine script path.' }
    $scriptDirectory = Split-Path -Parent $scriptPath
}

$repoRoot = Split-Path -Parent $scriptDirectory

$expectedFiles = @(
    'src\Core\MigrationBase.Core\Cloud\Azure\Governance\AzureOperatorAuthorizationRequirement.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Governance\AzureOperatorOverrideRequest.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Governance\AzureOperatorOverrideDecision.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Governance\AzureOperatorOverrideDecisionStatus.cs',
    'src\Core\MigrationBase.Core\Cloud\Azure\Governance\AzureOperatorAuthorizationEvaluation.cs',
    'config\azure-runtime\governance\operator-authorization.sample.json'
)

$missing = @()
foreach ($relativePath in $expectedFiles) {
    $path = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $path)) { $missing += $relativePath }
}
if (@($missing).Length -gt 0) {
    Write-Host 'Missing expected P5.6.4 files:' -ForegroundColor Red
    foreach ($file in $missing) { Write-Host " - $file" -ForegroundColor Red }
    exit 1
}

$projectPath = Join-Path $repoRoot 'src\Core\MigrationBase.Core\MigrationBase.Core.csproj'
if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "Missing project file: ${projectPath}"
}

$projectFiles = @(Get-ChildItem -Path $repoRoot -Filter *.csproj -Recurse -File | Where-Object {
    $_.FullName -notmatch '\\bin\\' -and $_.FullName -notmatch '\\obj\\'
})

$inlineVersions = @()
foreach ($project in $projectFiles) {
    [xml]$xml = Get-Content -LiteralPath $project.FullName -Raw
    if ($null -eq $xml.Project) { continue }
    $itemGroups = @($xml.Project.ChildNodes | Where-Object { $null -ne $_ -and $_.Name -eq 'ItemGroup' })
    foreach ($itemGroup in $itemGroups) {
        $packageRefs = @($itemGroup.ChildNodes | Where-Object { $null -ne $_ -and $_.Name -eq 'PackageReference' })
        foreach ($packageRef in $packageRefs) {
            $versionAttr = $null
            if ($null -ne $packageRef.Attributes) { $versionAttr = $packageRef.Attributes['Version'] }
            if ($null -ne $versionAttr -and -not [string]::IsNullOrWhiteSpace($versionAttr.Value)) {
                $inlineVersions += $project.FullName
                break
            }
        }
    }
}

if (@($inlineVersions).Length -gt 0) {
    Write-Host 'Inline PackageReference Version attributes detected:' -ForegroundColor Red
    foreach ($path in @($inlineVersions | Sort-Object -Unique)) { Write-Host " - $path" -ForegroundColor Red }
    exit 1
}

Write-Host 'Restoring MigrationBase.Core...'
dotnet restore $projectPath
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core restore failed.' }

Write-Host 'Building MigrationBase.Core...'
dotnet build $projectPath --no-restore
if ($LASTEXITCODE -ne 0) { throw 'MigrationBase.Core build failed.' }

Write-Host 'P5.6.4 operator authorization override governance contract validation passed.' -ForegroundColor Green
