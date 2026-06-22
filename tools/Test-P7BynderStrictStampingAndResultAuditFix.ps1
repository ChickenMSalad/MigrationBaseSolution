Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot

function Assert-FileExists([string] $Path) {
    if (-not (Test-Path -LiteralPath $Path)) { throw ('Missing expected file: ' + $Path) }
}

function Assert-Contains([string] $Path, [string] $Text) {
    Assert-FileExists $Path
    $content = Get-Content -LiteralPath $Path -Raw
    if ($content.IndexOf($Text, [System.StringComparison]::Ordinal) -lt 0) {
        throw ('Missing expected text in ' + $Path + ': ' + $Text)
    }
}

function Assert-NotContains([string] $Path, [string] $Text) {
    Assert-FileExists $Path
    $content = Get-Content -LiteralPath $Path -Raw
    if ($content.IndexOf($Text, [System.StringComparison]::Ordinal) -ge 0) {
        throw ('Found rejected text in ' + $Path + ': ' + $Text)
    }
}

$bynder = Join-Path $repoRoot 'src\Connectors\Targets\Migration.Connectors.Targets.Bynder\BynderTargetConnector.cs'
$taxonomy = Join-Path $repoRoot 'src\Connectors\Targets\Migration.Connectors.Targets.Bynder\Taxonomy\BynderTaxonomyWorkbookBuilder.cs'
$result = Join-Path $repoRoot 'src\Core\Migration.Domain\Models\MigrationResult.cs'
$runner = Join-Path $repoRoot 'src\Core\Migration.Orchestration\Execution\GenericMigrationJobRunner.cs'

Assert-Contains $bynder 'Bynder metadata mapping failed. No asset was created'
Assert-Contains $bynder 'BynderUploadPreparation'
Assert-Contains $bynder 'StampedFields'
Assert-NotContains $bynder 'It will not be stamped.'
Assert-Contains $taxonomy 'var columnName = metaProperty.Name;'
Assert-Contains $result 'TargetFields'
Assert-Contains $runner 'CreateCompletionStateProperties'
Assert-Contains $runner 'StampedFieldsJson'
Assert-Contains $runner 'Origin_Id'
Assert-Contains $runner 'TargetPayloadFieldsJson'

Write-Host 'P7 Bynder strict stamping and result audit fix validation passed.'
