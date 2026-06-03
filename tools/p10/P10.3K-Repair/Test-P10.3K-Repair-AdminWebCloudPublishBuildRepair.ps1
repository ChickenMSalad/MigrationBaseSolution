Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$sourceRoot = Join-Path $adminWebRoot 'src'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.3K-Repair-AdminWebCloudPublishBuildRepair.md'

$scriptFiles = @(
    (Join-Path $scriptRoot 'Apply-P10.3K-Repair-AdminWebCloudPublishBuildRepair.ps1'),
    (Join-Path $scriptRoot 'Test-P10.3K-Repair-AdminWebCloudPublishBuildRepair.ps1')
)
foreach ($scriptFile in $scriptFiles) {
    if (-not (Test-Path -LiteralPath $scriptFile)) {
        throw ('Expected script missing: {0}' -f $scriptFile)
    }
    $scriptContent = Get-Content -LiteralPath $scriptFile -Raw
    [void][scriptblock]::Create($scriptContent)
}

$targetFiles = @(
    'features\governance\mappingBuilder\pages\MappingBuilder.tsx',
    'features\governance\taxonomyBuilder\pages\TaxonomyBuilder.tsx'
)
foreach ($relativePath in $targetFiles) {
    $filePath = Join-Path $sourceRoot $relativePath
    if (-not (Test-Path -LiteralPath $filePath)) {
        throw ('Required builder page was not found: {0}' -f $filePath)
    }
    $content = Get-Content -LiteralPath $filePath -Raw
    if ($content.Contains(' message={message}')) {
        throw ('Builder page still uses unsupported Card message prop: {0}' -f $filePath)
    }
    if (-not $content.Contains(' description={message}')) {
        throw ('Builder page is missing expected Card description prop: {0}' -f $filePath)
    }
}

if (-not (Test-Path -LiteralPath $reportPath)) {
    throw ('Expected report missing: {0}' -f $reportPath)
}

Write-Host 'P10.3K Repair Admin Web cloud publish build repair validation passed.'
