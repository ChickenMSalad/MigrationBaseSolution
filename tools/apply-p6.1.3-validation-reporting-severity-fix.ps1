Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $PSCommandPath
}
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = (Get-Location).Path
}

$repoRoot = Resolve-Path (Join-Path $scriptRoot '..')
$reportPath = Join-Path $repoRoot 'src\Core\MigrationBase.Core\Cloud\Azure\Runtime\Composition\AzureRuntimeCompositionValidationReport.cs'

if (-not (Test-Path -LiteralPath $reportPath)) {
    throw "Expected file not found: $reportPath"
}

$content = Get-Content -LiteralPath $reportPath -Raw
$updated = $content.Replace('AzureRuntimeCompositionValidationSeverity.Critical', 'AzureRuntimeCompositionValidationSeverity.Error')

if ($updated -eq $content) {
    Write-Host 'No Critical severity reference found; file may already be fixed.'
} else {
    Set-Content -LiteralPath $reportPath -Value $updated -Encoding UTF8
    Write-Host "Updated severity reference in: $reportPath"
}

Write-Host 'P6.1.3 validation reporting severity compatibility fix complete.'
