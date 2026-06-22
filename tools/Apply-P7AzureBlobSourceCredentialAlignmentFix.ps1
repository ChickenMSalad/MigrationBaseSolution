Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($scriptRoot)) { $scriptRoot = (Get-Location).Path }
$repoRoot = Split-Path -Parent $scriptRoot
$packRoot = Join-Path $repoRoot '.p7-azureblob-source-credential-alignment-fix'

if (-not (Test-Path -LiteralPath $packRoot)) {
    throw ('Pack payload folder not found: ' + $packRoot)
}

$files = @(
    'src\Connectors\Sources\Migration.Connectors.Sources.AzureBlob\AzureBlobSourceConnector.cs',
    'src\Connectors\Sources\Migration.Connectors.Sources.AzureBlob\Migration.Connectors.Sources.AzureBlob.csproj',
    'src\Connectors\Targets\Migration.Connectors.Targets.Bynder\BynderTargetConnector.cs',
    'src\Core\Migration.Domain\Models\AssetBinary.cs',
    'src\Core\Migration.Orchestration\Validation\TargetBinaryValidationStep.cs',
    'src\Workers\Migration.Workers.QueueExecutor\Services\ProjectCredentialJobSettingsHydrator.cs'
)

foreach ($relativePath in $files) {
    $source = Join-Path $packRoot $relativePath
    $target = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $source)) { throw ('Missing payload file: ' + $source) }
    $targetDirectory = Split-Path -Parent $target
    if (-not (Test-Path -LiteralPath $targetDirectory)) { New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null }
    Copy-Item -LiteralPath $source -Destination $target -Force
    Write-Host ('Applied ' + $relativePath)
}

Write-Host 'P7 AzureBlob source credential alignment fix applied.'
