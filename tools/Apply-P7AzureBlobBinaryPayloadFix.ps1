Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent $ScriptRoot
$PackRoot = Join-Path $RepoRoot 'p7-azureblob-binary-payload-fix'

if (-not (Test-Path -LiteralPath $PackRoot)) {
    $PackRoot = Split-Path -Parent $ScriptRoot
}

$FilesRoot = Join-Path $PackRoot 'files'
if (-not (Test-Path -LiteralPath $FilesRoot)) {
    throw ('Pack files folder not found: ' + $FilesRoot)
}

$files = @(
    'src\Connectors\Sources\Migration.Connectors.Sources.AzureBlob\AzureBlobSourceConnector.cs',
    'src\Core\Migration.Orchestration\Validation\TargetBinaryValidationStep.cs'
)

foreach ($relative in $files) {
    $source = Join-Path $FilesRoot $relative
    $target = Join-Path $RepoRoot $relative

    if (-not (Test-Path -LiteralPath $source)) {
        throw ('Missing pack file: ' + $source)
    }

    $targetDir = Split-Path -Parent $target
    if (-not (Test-Path -LiteralPath $targetDir)) {
        New-Item -ItemType Directory -Path $targetDir | Out-Null
    }

    if (Test-Path -LiteralPath $target) {
        $backup = $target + '.p7-azureblob-binary-payload.bak'
        Copy-Item -LiteralPath $target -Destination $backup -Force
    }

    Copy-Item -LiteralPath $source -Destination $target -Force
    Write-Host ('Applied ' + $relative)
}

Write-Host 'P7 AzureBlob binary payload fix applied.'
