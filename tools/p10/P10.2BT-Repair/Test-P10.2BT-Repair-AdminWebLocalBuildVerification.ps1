Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$runnerPath = Join-Path $repoRoot 'tools\p10\P10.2BT-Repair\Run-P10.2BT-Repair-AdminWebNpmBuild.ps1'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2BT-Repair-AdminWebLocalBuildVerification.md'

if (-not (Test-Path -Path $adminWebRoot -PathType Container)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}
if (-not (Test-Path -Path (Join-Path $adminWebRoot 'package.json') -PathType Leaf)) {
    throw ('Admin Web package.json was not found: {0}' -f (Join-Path $adminWebRoot 'package.json'))
}
if (-not (Test-Path -Path $runnerPath -PathType Leaf)) {
    throw ('Repair build runner was not found: {0}' -f $runnerPath)
}
if (-not (Test-Path -Path $reportPath -PathType Leaf)) {
    throw ('Repair report was not found: {0}' -f $reportPath)
}

$runnerContent = Get-Content -Path $runnerPath -Raw
if ($runnerContent -notlike '*RedirectStandardOutput*') {
    throw 'Repair runner does not redirect stdout.'
}
if ($runnerContent -notlike '*RedirectStandardError*') {
    throw 'Repair runner does not redirect stderr.'
}
if ($runnerContent -like '*.tsx''*' -or $runnerContent -like '*.tsx"*') {
    throw 'Unexpected extension-bearing TypeScript import-like token found in repair runner.'
}

Write-Host 'P10.2BT Repair Admin Web local build verification test passed.'
