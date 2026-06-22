Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot

function Assert-FileContains {
    param([string]$Path, [string]$Text)
    if (-not (Test-Path -LiteralPath $Path)) { throw ('Missing expected file: ' + $Path) }
    $content = Get-Content -LiteralPath $Path -Raw
    if ($content.IndexOf($Text, [StringComparison]::Ordinal) -lt 0) {
        throw ('Missing expected text in ' + $Path + ': ' + $Text)
    }
}

$bynder = Join-Path $repoRoot 'src\Connectors\Targets\Migration.Connectors.Targets.Bynder\BynderTargetConnector.cs'
$taxonomy = Join-Path $repoRoot 'src\Connectors\Targets\Migration.Connectors.Targets.Bynder\Taxonomy\BynderTaxonomyWorkbookBuilder.cs'
$result = Join-Path $repoRoot 'src\Core\Migration.Domain\Models\MigrationResult.cs'
$runner = Join-Path $repoRoot 'src\Core\Migration.Orchestration\Execution\GenericMigrationJobRunner.cs'
$assetResiliency = Join-Path $repoRoot 'src\Connectors\Targets\Migration.Connectors.Targets.Bynder\Services\AssetResiliencyService.cs'

Assert-FileContains $bynder 'var uploadPreparation = await BuildUploadQueryAsync'
Assert-FileContains $bynder 'var uploadQuery = uploadPreparation.Query;'
Assert-FileContains $bynder 'TargetFields = uploadPreparation.StampedFields'
Assert-FileContains $bynder 'Bynder metadata mapping failed. No asset was created'
Assert-FileContains $taxonomy 'BaseUrl = new Uri(baseUrl.Trim(), UriKind.Absolute)'
Assert-FileContains $result 'TargetFields'
Assert-FileContains $runner 'TargetFields = result.TargetFields'
Assert-FileContains $assetResiliency 'UploadFileAsync(Stream stream, UploadQuery uploadQuery)'

Write-Host 'P7 Bynder strict stamping/result audit fix v2 validation passed.'
