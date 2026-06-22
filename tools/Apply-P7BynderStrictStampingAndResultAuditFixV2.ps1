Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
$packRoot = Join-Path $repoRoot '.p7-bynder-strict-stamping-and-result-audit-v2'

if (-not (Test-Path -LiteralPath $packRoot)) {
    throw ('Pack root not found: ' + $packRoot)
}

$files = @(
    'src\Connectors\Targets\Migration.Connectors.Targets.Bynder\BynderTargetConnector.cs',
    'src\Connectors\Targets\Migration.Connectors.Targets.Bynder\Services\AssetResiliencyService.cs',
    'src\Connectors\Targets\Migration.Connectors.Targets.Bynder\Taxonomy\BynderTaxonomyWorkbookBuilder.cs',
    'src\Core\Migration.Domain\Models\MigrationResult.cs',
    'src\Core\Migration.Orchestration\Execution\GenericMigrationJobRunner.cs'
)

foreach ($relativePath in $files) {
    $source = Join-Path $packRoot $relativePath
    $target = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $source)) { throw ('Pack file missing: ' + $source) }
    $targetDir = Split-Path -Parent $target
    if (-not (Test-Path -LiteralPath $targetDir)) { New-Item -ItemType Directory -Path $targetDir -Force | Out-Null }
    Copy-Item -LiteralPath $source -Destination $target -Force
    Write-Host ('Applied ' + $relativePath)
}

Write-Host 'P7 Bynder strict stamping/result audit fix v2 applied.'
