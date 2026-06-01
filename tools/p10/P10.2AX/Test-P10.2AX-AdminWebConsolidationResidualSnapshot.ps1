Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..')).Path
$adminSrc = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web\src'
$appsSrc = Join-Path $repoRoot 'apps\migration-admin-ui\src'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2AX-AdminWebConsolidationResidualSnapshot.md'
$jsonPath = Join-Path $repoRoot 'docs\P10\P10.2AX-AdminWebConsolidationResidualSnapshot.json'
$applyPath = Join-Path $scriptRoot 'Apply-P10.2AX-AdminWebConsolidationResidualSnapshot.ps1'

if (-not (Test-Path -Path $adminSrc -PathType Container)) { throw ('Missing canonical Admin Web source folder: {0}' -f $adminSrc) }
if (-not (Test-Path -Path $appsSrc -PathType Container)) { throw ('Missing reference apps Admin UI source folder: {0}' -f $appsSrc) }
if (-not (Test-Path -Path $applyPath -PathType Leaf)) { throw ('Missing apply script: {0}' -f $applyPath) }
if (-not (Test-Path -Path $reportPath -PathType Leaf)) { throw ('Missing report: {0}' -f $reportPath) }
if (-not (Test-Path -Path $jsonPath -PathType Leaf)) { throw ('Missing JSON report: {0}' -f $jsonPath) }

$reportContent = Get-Content -Path $reportPath -Raw
if ($reportContent.IndexOf('P10.2AX - Admin Web Consolidation Residual Snapshot', [System.StringComparison]::Ordinal) -lt 0) {
    throw ('Report title missing in {0}' -f $reportPath)
}

$jsonContent = Get-Content -Path $jsonPath -Raw
$parsed = $jsonContent | ConvertFrom-Json
if ($null -eq $parsed.AdminSrc) { throw ('JSON report missing AdminSrc: {0}' -f $jsonPath) }
if ($null -eq $parsed.AppsSrc) { throw ('JSON report missing AppsSrc: {0}' -f $jsonPath) }

Write-Host 'P10.2AX Admin Web consolidation residual snapshot validation passed.'
