Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..')).Path
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2CZ-AdminWebBuilderEndpointImplementationMap.md'
$csvPath = Join-Path $repoRoot 'artifacts\p10\P10.2CZ\builder-endpoint-implementation-map.csv'

if (-not (Test-Path $reportPath)) {
    throw ('Expected report was not found: {0}' -f $reportPath)
}
if (-not (Test-Path $csvPath)) {
    throw ('Expected CSV was not found: {0}' -f $csvPath)
}

$report = Get-Content -LiteralPath $reportPath -Raw
if ($report.IndexOf('Expected Builder API Surface', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw 'Report is missing expected builder API surface section.'
}
if ($report.IndexOf('Backend Builder-Related Source Candidates', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw 'Report is missing backend candidate section.'
}
if ($report.IndexOf('Admin Web Builder API Usage Candidates', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw 'Report is missing Admin Web usage section.'
}

$csv = Get-Content -LiteralPath $csvPath -Raw
if ($csv.IndexOf('Area,Kind,PathOrFile,Status,Notes', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw 'CSV is missing expected header.'
}

Write-Host 'P10.2CZ Admin Web builder endpoint implementation map validation passed.'
