Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($scriptRoot)) { $scriptRoot = (Get-Location).Path }
$repoRoot = Split-Path -Parent $scriptRoot

function Assert-FileExists {
    param([Parameter(Mandatory=$true)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { throw ('Missing expected file: ' + $Path) }
}

function Assert-ContainsText {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$Text
    )
    Assert-FileExists $Path
    $content = Get-Content -LiteralPath $Path -Raw
    if ($content.IndexOf($Text, [System.StringComparison]::Ordinal) -lt 0) {
        throw ('Missing expected text in ' + $Path + ': ' + $Text)
    }
}

function Assert-NotContainsText {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$Text
    )
    Assert-FileExists $Path
    $content = Get-Content -LiteralPath $Path -Raw
    if ($content.IndexOf($Text, [System.StringComparison]::Ordinal) -ge 0) {
        throw ('Found rejected text in ' + $Path + ': ' + $Text)
    }
}

$assetBinary = Join-Path $repoRoot 'src\Core\Migration.Domain\Models\AssetBinary.cs'
$sourceConnector = Join-Path $repoRoot 'src\Connectors\Sources\Migration.Connectors.Sources.AzureBlob\AzureBlobSourceConnector.cs'
$sourceProject = Join-Path $repoRoot 'src\Connectors\Sources\Migration.Connectors.Sources.AzureBlob\Migration.Connectors.Sources.AzureBlob.csproj'
$bynderTarget = Join-Path $repoRoot 'src\Connectors\Targets\Migration.Connectors.Targets.Bynder\BynderTargetConnector.cs'
$validation = Join-Path $repoRoot 'src\Core\Migration.Orchestration\Validation\TargetBinaryValidationStep.cs'

Assert-ContainsText $assetBinary 'Func<CancellationToken, Task<Stream>>? OpenReadAsync'
Assert-ContainsText $sourceProject '<PackageReference Include="Azure.Storage.Blobs" />'
Assert-NotContainsText $sourceProject 'Version='
Assert-ContainsText $sourceConnector 'FindBySourceAssetIdTagAsync'
Assert-ContainsText $sourceConnector '"source_asset_id"'
Assert-ContainsText $sourceConnector 'BlobOpenReadOptions'
Assert-ContainsText $sourceConnector 'OpenReadAsync = async token => await blob.OpenReadAsync'
Assert-ContainsText $sourceConnector 'Refusing non-unique tag match'
Assert-ContainsText $bynderTarget 'binary?.OpenReadAsync'
Assert-ContainsText $bynderTarget 'OpenUploadStreamAsync'
Assert-ContainsText $bynderTarget 'temporary seekable file'
Assert-ContainsText $validation 'binary.OpenReadAsync is null'

Write-Host 'P7 AzureBlob intermediate tag reader fix validation passed.'
