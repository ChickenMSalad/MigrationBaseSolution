Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$repoRootPath = $repoRoot.ProviderPath

$applyPath = Join-Path $scriptRoot 'Apply-P10.2CU-AdminWebBuilderApiContractSmoke.ps1'
$runnerPath = Join-Path $scriptRoot 'Run-P10.2CU-AdminWebBuilderApiContractSmoke.ps1'
$reportPath = Join-Path $repoRootPath 'docs\P10\P10.2CU-AdminWebBuilderApiContractSmoke.md'
$adminWebRoot = Join-Path $repoRootPath 'src\Admin\Migration.Admin.Web'

$requiredFiles = @($applyPath, $runnerPath, $reportPath)
foreach ($requiredFile in $requiredFiles) {
    if (-not (Test-Path -LiteralPath $requiredFile)) {
        throw ('Required file missing: {0}' -f $requiredFile)
    }
}

if (-not (Test-Path -LiteralPath $adminWebRoot)) {
    throw ('Admin Web root missing: {0}' -f $adminWebRoot)
}

$runnerText = Get-Content -LiteralPath $runnerPath -Raw
if ($runnerText.IndexOf('param(', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw 'Runner does not contain a param block.'
}
if ($runnerText.IndexOf('TimeoutSec', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw 'Runner does not contain bounded request timeout handling.'
}
if ($runnerText.IndexOf('SkippedActionEndpoint', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw 'Runner does not classify action endpoints as skipped.'
}
if ($runnerText.IndexOf('Export-Csv', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw 'Runner does not write CSV details.'
}

$reportText = Get-Content -LiteralPath $reportPath -Raw
if ($reportText.IndexOf('Manifest', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw 'Report does not mention Manifest builder coverage.'
}
if ($reportText.IndexOf('Taxonomy', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw 'Report does not mention Taxonomy builder coverage.'
}
if ($reportText.IndexOf('Mapping', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw 'Report does not mention Mapping builder coverage.'
}

Write-Host 'P10.2CU Admin Web builder API contract smoke verification passed.'
