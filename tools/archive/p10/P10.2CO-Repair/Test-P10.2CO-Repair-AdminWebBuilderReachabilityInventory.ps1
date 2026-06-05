Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$repoRoot = $repoRoot.ProviderPath

$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$sourceRoot = Join-Path $adminWebRoot 'src'
$docsReport = Join-Path $repoRoot 'docs\P10\P10.2CO-Repair-AdminWebBuilderReachabilityInventory.md'
$artifactReport = Join-Path $repoRoot 'artifacts\p10\P10.2CO-Repair\builder-reachability-inventory.md'

if (-not (Test-Path -LiteralPath $adminWebRoot -PathType Container)) { throw ('Admin Web root was not found: {0}' -f $adminWebRoot) }
if (-not (Test-Path -LiteralPath $sourceRoot -PathType Container)) { throw ('Admin Web source root was not found: {0}' -f $sourceRoot) }
if (-not (Test-Path -LiteralPath $docsReport -PathType Leaf)) { throw ('Expected docs report was not found: {0}' -f $docsReport) }
if (-not (Test-Path -LiteralPath $artifactReport -PathType Leaf)) { throw ('Expected artifact report was not found: {0}' -f $artifactReport) }

$reportText = [System.IO.File]::ReadAllText($docsReport)
$requiredTokens = @('Manifest Builder', 'Taxonomy Builder', 'Mapping Builder', 'Recommended next action', 'Guardrails')
foreach ($token in $requiredTokens) {
    if (-not $reportText.Contains($token)) { throw ('Report is missing required text: {0}' -f $token) }
}

$appTsx = Join-Path $sourceRoot 'App.tsx'
if (Test-Path -LiteralPath $appTsx -PathType Leaf) {
    $appText = [System.IO.File]::ReadAllText($appTsx)
    if ($appText.Contains('.tsx')) { throw ('App.tsx contains an extension-bearing TSX token: {0}' -f $appTsx) }
}

Write-Host 'P10.2CO Repair Admin Web builder reachability inventory validation passed.'
