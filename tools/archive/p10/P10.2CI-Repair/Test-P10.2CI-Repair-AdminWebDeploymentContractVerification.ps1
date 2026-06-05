Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..')).Path
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$ciToolRoot = Join-Path $repoRoot 'tools\p10\P10.2CI'
$repairToolRoot = Join-Path $repoRoot 'tools\p10\P10.2CI-Repair'
$docsRoot = Join-Path $repoRoot 'docs\P10'
$artifactsRoot = Join-Path $repoRoot 'artifacts\p10\P10.2CI-Repair'

$requiredFiles = @(
    [pscustomobject]@{ Label = 'Admin Web package.json'; Path = (Join-Path $adminWebRoot 'package.json') },
    [pscustomobject]@{ Label = 'Admin Web vite.config.ts'; Path = (Join-Path $adminWebRoot 'vite.config.ts') },
    [pscustomobject]@{ Label = 'P10.2CI runner'; Path = (Join-Path $ciToolRoot 'Run-P10.2CI-AdminWebDeploymentContractCheck.ps1') },
    [pscustomobject]@{ Label = 'P10.2CI Repair apply'; Path = (Join-Path $repairToolRoot 'Apply-P10.2CI-Repair-AdminWebDeploymentContractVerification.ps1') },
    [pscustomobject]@{ Label = 'P10.2CI Repair test'; Path = (Join-Path $repairToolRoot 'Test-P10.2CI-Repair-AdminWebDeploymentContractVerification.ps1') },
    [pscustomobject]@{ Label = 'P10.2CI Repair report'; Path = (Join-Path $docsRoot 'P10.2CI-Repair-AdminWebDeploymentContractVerification.Report.md') },
    [pscustomobject]@{ Label = 'P10.2CI Repair evidence'; Path = (Join-Path $artifactsRoot 'deployment-contract-repair-summary.md') }
)

foreach ($item in $requiredFiles) {
    if (-not (Test-Path -Path $item.Path -PathType Leaf)) {
        throw ('Required file missing: {0}: {1}' -f $item.Label, $item.Path)
    }
}

if (-not (Test-Path -Path $adminWebRoot -PathType Container)) {
    throw ('Admin Web root missing: {0}' -f $adminWebRoot)
}

$reportPath = Join-Path $docsRoot 'P10.2CI-Repair-AdminWebDeploymentContractVerification.Report.md'
$reportText = Get-Content -Path $reportPath -Raw
if ($reportText -notlike '*deployment contract*') {
    throw ('Repair report does not mention deployment contract: {0}' -f $reportPath)
}
if ($reportText -notlike '*Does not modify Admin Web source files*') {
    throw ('Repair report does not document non-mutating behavior: {0}' -f $reportPath)
}

$runnerPath = Join-Path $ciToolRoot 'Run-P10.2CI-AdminWebDeploymentContractCheck.ps1'
$runnerText = Get-Content -Path $runnerPath -Raw
if ($runnerText.Length -le 0) {
    throw ('P10.2CI deployment contract runner is empty: {0}' -f $runnerPath)
}
if (($runnerText -notlike '*Admin*') -and ($runnerText -notlike '*dist*') -and ($runnerText -notlike '*Deployment*')) {
    throw ('P10.2CI deployment contract runner does not appear to describe deployment validation: {0}' -f $runnerPath)
}

Write-Host 'P10.2CI Repair deployment contract verification passed.'
