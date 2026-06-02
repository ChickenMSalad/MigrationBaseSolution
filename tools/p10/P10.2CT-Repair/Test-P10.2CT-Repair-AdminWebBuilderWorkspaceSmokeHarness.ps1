Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$repoRoot = $repoRoot.Path

$applyPath = Join-Path $scriptRoot 'Apply-P10.2CT-Repair-AdminWebBuilderWorkspaceSmokeHarness.ps1'
$runnerPath = Join-Path $scriptRoot 'Run-P10.2CT-Repair-AdminWebBuilderWorkspaceSmoke.ps1'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2CT-Repair-AdminWebBuilderWorkspaceSmokeHarness.md'
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$appPath = Join-Path $adminWebRoot 'src\App.tsx'

$requiredFiles = New-Object 'System.Collections.Generic.List[string]'
[void]$requiredFiles.Add($applyPath)
[void]$requiredFiles.Add($runnerPath)
[void]$requiredFiles.Add($reportPath)
[void]$requiredFiles.Add($appPath)

foreach ($file in $requiredFiles) {
    if (-not (Test-Path -LiteralPath $file)) {
        throw ('Required file is missing: {0}' -f $file)
    }
}

$parseTargets = New-Object 'System.Collections.Generic.List[string]'
[void]$parseTargets.Add($applyPath)
[void]$parseTargets.Add($runnerPath)
[void]$parseTargets.Add($MyInvocation.MyCommand.Path)

foreach ($path in $parseTargets) {
    $content = Get-Content -LiteralPath $path -Raw
    [void][scriptblock]::Create($content)
}

$runnerText = Get-Content -LiteralPath $runnerPath -Raw
if ($runnerText -notmatch 'param\s*\(') {
    throw 'Repair runner does not contain a param block.'
}
if ($runnerText -notmatch 'AdminWebBaseUrl') {
    throw 'Repair runner does not expose AdminWebBaseUrl.'
}
if ($runnerText -notmatch 'TimeoutSec') {
    throw 'Repair runner does not expose TimeoutSec.'
}
if ($runnerText -notmatch 'Invoke-WebRequest') {
    throw 'Repair runner does not perform web requests.'
}

$reportText = Get-Content -LiteralPath $reportPath -Raw
if ($reportText -notmatch 'Builder Workspace Smoke Harness') {
    throw 'Repair report does not describe the builder workspace smoke harness.'
}

Write-Host 'P10.2CT Repair Admin Web builder workspace smoke harness validation passed.'
