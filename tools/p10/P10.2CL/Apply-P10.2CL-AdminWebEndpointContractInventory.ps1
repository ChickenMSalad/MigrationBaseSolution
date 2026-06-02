Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$repoRoot = $repoRoot.Path

$docsDir = Join-Path $repoRoot 'docs\P10'
if (-not (Test-Path -LiteralPath $docsDir)) {
    New-Item -ItemType Directory -Path $docsDir -Force | Out-Null
}

$runnerPath = Join-Path $scriptRoot 'Run-P10.2CL-AdminWebEndpointContractInventory.ps1'
if (-not (Test-Path -LiteralPath $runnerPath)) {
    throw ('Required runner was not found: {0}' -f $runnerPath)
}

& powershell -ExecutionPolicy Bypass -File $runnerPath

$artifactReport = Join-Path $repoRoot 'artifacts\p10\P10.2CL\admin-web-endpoint-contract-inventory.md'
if (-not (Test-Path -LiteralPath $artifactReport)) {
    throw ('Expected endpoint inventory report was not created: {0}' -f $artifactReport)
}

$docReport = Join-Path $docsDir 'P10.2CL-AdminWebEndpointContractInventory.Report.md'
Copy-Item -LiteralPath $artifactReport -Destination $docReport -Force
Write-Host ('Wrote report: {0}' -f $docReport)
Write-Host 'P10.2CL Admin Web endpoint contract inventory applied.'
