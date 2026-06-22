Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($scriptRoot)) { $scriptRoot = (Get-Location).Path }
$repoRoot = Split-Path -Parent $scriptRoot
$packRoot = $repoRoot

$sourceRoot = Join-Path $packRoot 'files'
if (-not (Test-Path -LiteralPath $sourceRoot)) {
    throw "Pack files folder not found: $sourceRoot"
}

$files = @(
    'src\Connectors\Targets\Migration.Connectors.Targets.Bynder\Taxonomy\BynderTaxonomyWorkbookBuilder.cs',
    'src\Connectors\Targets\Migration.Connectors.Targets.Bynder\Registration\ServiceCollectionExtensions.cs',
    'src\Connectors\Targets\Migration.Connectors.Targets.Bynder\Services\BynderMetadataPropertiesService.cs',
    'src\Core\Migration.Admin.Api\Endpoints\TaxonomyBuilderEndpoints.cs'
)

foreach ($relativePath in $files) {
    $src = Join-Path $sourceRoot $relativePath
    $dst = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $src)) {
        throw "Pack source file missing: $src"
    }

    $dstDir = Split-Path -Parent $dst
    if (-not (Test-Path -LiteralPath $dstDir)) {
        New-Item -ItemType Directory -Path $dstDir -Force | Out-Null
    }

    if (Test-Path -LiteralPath $dst) {
        $backup = "$dst.p7-bynder-taxonomy-legacy-alignment-fix.bak"
        Copy-Item -LiteralPath $dst -Destination $backup -Force
    }

    Copy-Item -LiteralPath $src -Destination $dst -Force
    Write-Host "Applied $relativePath"
}

Write-Host 'P7 Bynder taxonomy legacy alignment fix applied.'
