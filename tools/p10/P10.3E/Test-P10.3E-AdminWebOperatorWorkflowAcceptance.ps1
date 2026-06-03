Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..\..')
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$docsRoot = Join-Path $repoRoot 'docs\P10'
$reportPath = Join-Path $docsRoot 'P10.3E-AdminWebOperatorWorkflowAcceptance.md'
$applyScript = Join-Path $PSScriptRoot 'Apply-P10.3E-AdminWebOperatorWorkflowAcceptance.ps1'
$testScript = Join-Path $PSScriptRoot 'Test-P10.3E-AdminWebOperatorWorkflowAcceptance.ps1'
$runScript = Join-Path $PSScriptRoot 'Run-P10.3E-AdminWebOperatorWorkflowAcceptance.ps1'

if (-not (Test-Path -LiteralPath $adminWebRoot -PathType Container)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}

foreach ($scriptPath in @($applyScript, $testScript, $runScript)) {
    if (-not (Test-Path -LiteralPath $scriptPath -PathType Leaf)) {
        throw ('Expected script missing: {0}' -f $scriptPath)
    }

    $content = Get-Content -LiteralPath $scriptPath -Raw
    [void][scriptblock]::Create($content)
}

if (-not (Test-Path -LiteralPath $reportPath -PathType Leaf)) {
    throw ('Expected report missing: {0}' -f $reportPath)
}

$reportContent = Get-Content -LiteralPath $reportPath -Raw
if ($reportContent.IndexOf('Operator Workflow Acceptance') -lt 0) {
    throw ('Report did not contain the expected title: {0}' -f $reportPath)
}

Write-Host 'P10.3E Admin Web operator workflow acceptance validation passed.'
