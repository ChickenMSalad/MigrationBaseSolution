Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..')).Path
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$workflowDir = Join-Path $repoRoot '.github\workflows'
$workflowPath = Join-Path $workflowDir 'admin-web-build-validation.yml'
$samplePath = Join-Path $workflowDir 'admin-web-build-validation.p10.sample.yml'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2CJ-Repair-AdminWebCiBuildValidationWorkflow.md'

if (-not (Test-Path -Path $adminWebRoot -PathType Container)) {
    throw ('Admin Web root not found: {0}' -f $adminWebRoot)
}
if (-not (Test-Path -Path $workflowDir -PathType Container)) {
    throw ('Workflow directory not found: {0}' -f $workflowDir)
}
if (-not (Test-Path -Path $reportPath -PathType Leaf)) {
    throw ('Expected report not found: {0}' -f $reportPath)
}

$workflowExists = Test-Path -Path $workflowPath -PathType Leaf
$sampleExists = Test-Path -Path $samplePath -PathType Leaf
if ((-not $workflowExists) -and (-not $sampleExists)) {
    throw 'Neither existing workflow nor companion sample workflow exists.'
}

$reportText = Get-Content -Path $reportPath -Raw
if ($reportText -notlike '*P10.2CJ Repair*') {
    throw 'Report does not contain P10.2CJ Repair heading.'
}
if ($reportText -notlike '*Workflow exists*') {
    throw 'Report does not include workflow existence status.'
}

if ($sampleExists) {
    $sampleText = Get-Content -Path $samplePath -Raw
    if ($sampleText -notlike '*src/Admin/Migration.Admin.Web*') {
        throw 'Companion sample does not reference canonical Admin Web path.'
    }
    if ($sampleText -notlike '*npm ci*') {
        throw 'Companion sample does not include npm ci.'
    }
    if ($sampleText -notlike '*npm run build*') {
        throw 'Companion sample does not include npm run build.'
    }
}

Write-Host 'P10.2CJ Repair validation passed.'
