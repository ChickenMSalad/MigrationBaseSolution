Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..')).Path
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2CV-AdminWebBuilderBackendContractReconciliation.md'
$detailsPath = Join-Path $repoRoot 'artifacts\p10\P10.2CV\builder-backend-contract-reconciliation.details.csv'

if (!(Test-Path $reportPath)) {
    throw ('Expected report was not found: {0}' -f $reportPath)
}
if (!(Test-Path $detailsPath)) {
    throw ('Expected details CSV was not found: {0}' -f $detailsPath)
}

$reportText = Get-Content -Path $reportPath -Raw
foreach ($term in @('Manifest Builder','Taxonomy Builder','Mapping Builder','Next action')) {
    if (!$reportText.Contains($term)) {
        throw ('Report missing expected section/text: {0}' -f $term)
    }
}

$detailsText = Get-Content -Path $detailsPath -Raw
if (!$detailsText.StartsWith('Category,Term,RelativePath,LineNumber,Text')) {
    throw 'Details CSV header is missing or malformed.'
}

$applyPath = Join-Path $scriptRoot 'Apply-P10.2CV-AdminWebBuilderBackendContractReconciliation.ps1'
$testPath = Join-Path $scriptRoot 'Test-P10.2CV-AdminWebBuilderBackendContractReconciliation.ps1'
[void][scriptblock]::Create((Get-Content -Path $applyPath -Raw))
[void][scriptblock]::Create((Get-Content -Path $testPath -Raw))

Write-Host 'P10.2CV Admin Web builder/backend contract reconciliation validation passed.'
