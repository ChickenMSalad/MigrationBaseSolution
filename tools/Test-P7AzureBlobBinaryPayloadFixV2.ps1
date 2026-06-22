Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot

function Assert-FileExists([string] $Path) {
    if (-not (Test-Path -LiteralPath $Path)) { throw ('Missing expected file: ' + $Path) }
}

function Assert-Contains([string] $Path, [string] $Text) {
    $content = Get-Content -LiteralPath $Path -Raw
    if ($content.IndexOf($Text, [System.StringComparison]::Ordinal) -lt 0) {
        throw ('Missing expected text in ' + $Path + ': ' + $Text)
    }
}

$azureSource = Join-Path $repoRoot 'src\Connectors\Sources\Migration.Connectors.Sources.AzureBlob\AzureBlobSourceConnector.cs'
$bynderTarget = Join-Path $repoRoot 'src\Connectors\Targets\Migration.Connectors.Targets.Bynder\BynderTargetConnector.cs'
$binaryValidation = Join-Path $repoRoot 'src\Core\Migration.Orchestration\Validation\TargetBinaryValidationStep.cs'

Assert-FileExists $azureSource
Assert-FileExists $bynderTarget
Assert-FileExists $binaryValidation

Assert-Contains $azureSource 'row.SourceAssetId'
Assert-Contains $azureSource 'SourceBlobName'
Assert-Contains $azureSource 'Binary = string.IsNullOrWhiteSpace(sourceLocation)'
Assert-Contains $binaryValidation 'ResolveFallbackSourceLocation'
Assert-Contains $binaryValidation 'SourceAssetId'
Assert-Contains $bynderTarget 'SourceAssetId'
Assert-Contains $bynderTarget 'source_blob_name'

Write-Host 'P7 AzureBlob binary payload fix v3 validator passed.'
