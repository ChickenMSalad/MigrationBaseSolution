Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$repoRoot = $repoRoot.Path

$applyPath = Join-Path $scriptRoot 'Apply-P10.2CL-AdminWebEndpointContractInventory.ps1'
$runnerPath = Join-Path $scriptRoot 'Run-P10.2CL-AdminWebEndpointContractInventory.ps1'
$docPath = Join-Path $repoRoot 'docs\P10\P10.2CL-AdminWebEndpointContractInventory.md'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2CL-AdminWebEndpointContractInventory.Report.md'
$artifactReport = Join-Path $repoRoot 'artifacts\p10\P10.2CL\admin-web-endpoint-contract-inventory.md'
$adminWebSource = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web\src'

$required = @($applyPath, $runnerPath, $docPath, $adminWebSource)
foreach ($path in $required) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw ('Required path was not found: {0}' -f $path)
    }
}

& powershell -ExecutionPolicy Bypass -File $runnerPath

if (-not (Test-Path -LiteralPath $artifactReport)) {
    throw ('Expected artifact report was not found: {0}' -f $artifactReport)
}

$artifactText = Get-Content -LiteralPath $artifactReport -Raw
if ($artifactText -notmatch 'Admin Web Endpoint Contract Inventory') {
    throw 'Endpoint contract inventory report does not contain the expected title.'
}
if ($artifactText -notmatch 'API files scanned') {
    throw 'Endpoint contract inventory report does not contain the API file scan count.'
}
if ($artifactText -notmatch 'Endpoint-like string references') {
    throw 'Endpoint contract inventory report does not contain the endpoint section.'
}

if (Test-Path -LiteralPath $reportPath) {
    $reportText = Get-Content -LiteralPath $reportPath -Raw
    if ($reportText -notmatch 'Admin Web Endpoint Contract Inventory') {
        throw ('Existing docs report does not contain expected title: {0}' -f $reportPath)
    }
}

Write-Host 'P10.2CL Admin Web endpoint contract inventory validation passed.'
