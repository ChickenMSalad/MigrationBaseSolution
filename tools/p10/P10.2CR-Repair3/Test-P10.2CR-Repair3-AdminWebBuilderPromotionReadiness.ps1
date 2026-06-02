Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2CR-Repair3-AdminWebBuilderPromotionReadiness.md'

if (-not (Test-Path -LiteralPath $reportPath)) {
    throw ('Expected report was not found: {0}' -f $reportPath)
}

$content = Get-Content -LiteralPath $reportPath -Raw
if ([string]::IsNullOrWhiteSpace($content)) {
    throw ('Expected report is empty: {0}' -f $reportPath)
}

$requiredText = @(
    'Builder Promotion Readiness',
    'Manifest Builder',
    'Taxonomy Builder',
    'Mapping Builder',
    'Canonical Admin Web',
    'Reference Admin Web',
    'Legacy apps Admin UI'
)

foreach ($item in $requiredText) {
    if ($content.IndexOf($item, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw ('Expected report text missing: {0}' -f $item)
    }
}

Write-Host 'P10.2CR Repair3 builder promotion readiness validation passed.'
