Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
$packRoot = Join-Path $repoRoot '.p7-azureblob-metadata-fallback-fix'

if (-not (Test-Path -LiteralPath $packRoot)) { throw ('Pack payload folder not found: ' + $packRoot) }

$files = @(
    'src\Connectors\Sources\Migration.Connectors.Sources.AzureBlob\AzureBlobSourceConnector.cs',
    'src\Connectors\Sources\Migration.Connectors.Sources.AzureBlob\Migration.Connectors.Sources.AzureBlob.csproj'
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

Write-Host 'P7 AzureBlob metadata fallback fix applied.'
