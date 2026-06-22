Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot

function Assert-FileContains([string] $Path, [string] $Text) {
    if (-not (Test-Path -LiteralPath $Path)) { throw ('Missing expected file: ' + $Path) }
    $content = Get-Content -LiteralPath $Path -Raw
    if ($content.IndexOf($Text, [StringComparison]::Ordinal) -lt 0) {
        throw ('Missing expected text in ' + $Path + ': ' + $Text)
    }
}

$sourceConnector = Join-Path $repoRoot 'src\Connectors\Sources\Migration.Connectors.Sources.AzureBlob\AzureBlobSourceConnector.cs'

Assert-FileContains $sourceConnector 'FindBySourceAssetIdTagAsync'
Assert-FileContains $sourceConnector 'FindBySourceAssetIdMetadataAsync'
Assert-FileContains $sourceConnector 'BlobTraits.Metadata'
Assert-FileContains $sourceConnector 'source_asset_id'
Assert-FileContains $sourceConnector 'metadata fallback'
Assert-FileContains $sourceConnector 'OpenReadAsync'

Write-Host 'P7 AzureBlob metadata fallback fix validation passed.'
