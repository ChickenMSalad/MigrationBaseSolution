Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptDirectory = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $scriptDirectory = Split-Path -Parent $PSCommandPath
}
if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $scriptDirectory = (Get-Location).Path
}

$repoRoot = Split-Path -Parent $scriptDirectory
$duplicateFile = Join-Path $repoRoot 'src\Core\MigrationBase.Core\Cloud\Azure\Runtime\Composition\AzureRuntimeCompositionValidationSeverity.cs'

# P6.1.3 accidentally added a duplicate enum file. The severity enum already exists in the
# composition model from prior P6.1 sets, so this standalone file should be removed.
if (Test-Path -LiteralPath $duplicateFile) {
    Remove-Item -LiteralPath $duplicateFile -Force
    Write-Host "Removed duplicate file: $duplicateFile"
}
else {
    Write-Host "Duplicate severity file was not present; no removal needed."
}

Write-Host 'P6.1.3 validation reporting duplicate severity cleanup complete.'
