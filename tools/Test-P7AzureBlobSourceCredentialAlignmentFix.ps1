Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($scriptRoot)) { $scriptRoot = (Get-Location).Path }
$repoRoot = Split-Path -Parent $scriptRoot

function Assert-FileContains {
    param([string] $Path, [string] $Text)
    if (-not (Test-Path -LiteralPath $Path)) { throw ('Missing expected file: ' + $Path) }
    $content = Get-Content -LiteralPath $Path -Raw
    if ($content.IndexOf($Text, [StringComparison]::Ordinal) -lt 0) {
        throw ('Missing expected text in ' + $Path + ': ' + $Text)
    }
}

function Assert-FileDoesNotContain {
    param([string] $Path, [string] $Text)
    if (-not (Test-Path -LiteralPath $Path)) { throw ('Missing expected file: ' + $Path) }
    $content = Get-Content -LiteralPath $Path -Raw
    if ($content.IndexOf($Text, [StringComparison]::Ordinal) -ge 0) {
        throw ('Found rejected text in ' + $Path + ': ' + $Text)
    }
}

$source = Join-Path $repoRoot 'src\Connectors\Sources\Migration.Connectors.Sources.AzureBlob\AzureBlobSourceConnector.cs'
$hydrator = Join-Path $repoRoot 'src\Workers\Migration.Workers.QueueExecutor\Services\ProjectCredentialJobSettingsHydrator.cs'
$bynder = Join-Path $repoRoot 'src\Connectors\Targets\Migration.Connectors.Targets.Bynder\BynderTargetConnector.cs'
$binary = Join-Path $repoRoot 'src\Core\Migration.Domain\Models\AssetBinary.cs'
$validation = Join-Path $repoRoot 'src\Core\Migration.Orchestration\Validation\TargetBinaryValidationStep.cs'
$csproj = Join-Path $repoRoot 'src\Connectors\Sources\Migration.Connectors.Sources.AzureBlob\Migration.Connectors.Sources.AzureBlob.csproj'

Assert-FileContains -Path $source -Text 'FindBySourceAssetIdTagAsync(container, sourceAssetId, cancellationToken)'
Assert-FileContains -Path $source -Text 'var expression = $'
Assert-FileContains -Path $source -Text 'source_asset_id'
Assert-FileContains -Path $source -Text 'SourceCredential_ConnectionString'
Assert-FileContains -Path $source -Text 'SourceCredential_ContainerName'
Assert-FileContains -Path $source -Text 'OpenReadAsync = async token => await blob.OpenReadAsync'
Assert-FileContains -Path $hydrator -Text 'Alias(settings, "SourceCredential_ConnectionString", "AzureBlobSourceConnectionString")'
Assert-FileContains -Path $hydrator -Text 'Alias(settings, "SourceCredential_ContainerName", "AzureBlobSourceContainer")'
Assert-FileContains -Path $hydrator -Text 'role.Equals("Source", StringComparison.OrdinalIgnoreCase)'
Assert-FileContains -Path $bynder -Text 'OpenUploadStreamAsync(binary, source, cancellationToken)'
Assert-FileContains -Path $bynder -Text 'CopyToTemporarySeekableFileAsync'
Assert-FileContains -Path $binary -Text 'Func<CancellationToken, Task<Stream>>? OpenReadAsync'
Assert-FileContains -Path $validation -Text 'binary.OpenReadAsync is null && string.IsNullOrWhiteSpace(binary.SourceUri)'
Assert-FileContains -Path $csproj -Text '<PackageReference Include="Azure.Storage.Blobs" />'
Assert-FileDoesNotContain -Path $csproj -Text 'Version='

Write-Host 'P7 AzureBlob source credential alignment fix validation passed.'
