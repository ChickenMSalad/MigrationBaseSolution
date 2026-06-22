Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
$filesRoot = Join-Path $repoRoot 'files'
if (-not (Test-Path -LiteralPath $filesRoot)) {
    $filesRoot = Join-Path (Split-Path -Parent $scriptRoot) 'files'
}
if (-not (Test-Path -LiteralPath $filesRoot)) {
    throw 'Could not locate pack files directory. Extract the ZIP into the repository root and run this script from .\tools.'
}

$relativeFiles = @(
    'src\Connectors\Sources\Migration.Connectors.Sources.AzureBlob\AzureBlobSourceConnector.cs',
    'src\Connectors\Targets\Migration.Connectors.Targets.Bynder\BynderTargetConnector.cs',
    'src\Core\Migration.Orchestration\Validation\TargetBinaryValidationStep.cs'
)

foreach ($relative in $relativeFiles) {
    $source = Join-Path $filesRoot $relative
    $destination = Join-Path $repoRoot $relative
    if (-not (Test-Path -LiteralPath $source)) { throw ('Missing pack file: ' + $source) }
    $destinationDirectory = Split-Path -Parent $destination
    if (-not (Test-Path -LiteralPath $destinationDirectory)) {
        New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
    }
    Copy-Item -LiteralPath $source -Destination $destination -Force
    Write-Host ('Applied ' + $relative)
}

Write-Host 'P7 AzureBlob binary payload fix v3 applied.'
