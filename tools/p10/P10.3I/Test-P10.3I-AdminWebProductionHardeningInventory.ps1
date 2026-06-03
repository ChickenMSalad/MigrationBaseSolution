Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $scriptRoot))

$applyPath = Join-Path $scriptRoot 'Apply-P10.3I-AdminWebProductionHardeningInventory.ps1'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.3I-AdminWebProductionHardeningInventory.md'
$artifactReportPath = Join-Path $repoRoot 'artifacts\p10\P10.3I\production-hardening-inventory.md'
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'

foreach ($path in @($applyPath, $reportPath, $artifactReportPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw ('Expected file was not found: {0}' -f $path)
    }
}

if (-not (Test-Path -LiteralPath $adminWebRoot -PathType Container)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}

$report = Get-Content -LiteralPath $reportPath -Raw
foreach ($required in @('Production Hardening Inventory','## Summary','## Inventory','Runtime config','Deployment','Compile scope')) {
    if ($report -notlike ('*' + $required + '*')) {
        throw ('Report missing expected content: {0}' -f $required)
    }
}

$applyContent = Get-Content -LiteralPath $applyPath -Raw
$forbidden = @('[string[]]','[object[]]','return @(','Add-Text -','Add-Line -',' += ')
foreach ($token in $forbidden) {
    if ($applyContent.Contains($token)) {
        throw ('Apply script contains forbidden pattern: {0}' -f $token)
    }
}

Write-Host 'P10.3I Admin Web production hardening inventory validation passed.'
