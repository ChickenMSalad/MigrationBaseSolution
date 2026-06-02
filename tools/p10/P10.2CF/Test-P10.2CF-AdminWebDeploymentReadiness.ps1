Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2CF-AdminWebDeploymentReadiness.Report.md'
$runScript = Join-Path $scriptRoot 'Run-P10.2CF-AdminWebProductionBuild.ps1'

$requiredLeafFiles = @(
    (Join-Path $adminWebRoot 'package.json'),
    (Join-Path $adminWebRoot 'package-lock.json'),
    (Join-Path $adminWebRoot 'vite.config.ts'),
    (Join-Path $adminWebRoot 'tsconfig.json'),
    (Join-Path $adminWebRoot 'index.html'),
    $runScript
)

foreach ($path in $requiredLeafFiles) {
    if (-not (Test-Path -Path $path -PathType Leaf)) {
        throw ('Required file missing: {0}' -f $path)
    }
}

$srcRoot = Join-Path $adminWebRoot 'src'
if (-not (Test-Path -Path $srcRoot -PathType Container)) {
    throw ('Admin Web src folder missing: {0}' -f $srcRoot)
}

if (-not (Test-Path -Path $reportPath -PathType Leaf)) {
    throw ('Readiness report missing. Run the apply script first: {0}' -f $reportPath)
}

$reportText = Get-Content -Path $reportPath -Raw
if ($reportText -notlike '*Admin Web Deployment Readiness Report*') {
    throw 'Readiness report did not contain the expected heading.'
}
if ($reportText -notlike '*Required build surface*') {
    throw 'Readiness report did not contain required build surface section.'
}

Write-Host 'P10.2CF Admin Web deployment readiness validation passed.'
