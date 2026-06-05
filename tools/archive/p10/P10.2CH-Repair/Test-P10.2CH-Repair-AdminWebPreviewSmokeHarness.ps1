Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$repoRootPath = $repoRoot.Path

$adminWebRoot = Join-Path $repoRootPath 'src\Admin\Migration.Admin.Web'
$runnerPath = Join-Path $repoRootPath 'tools\p10\P10.2CH\Run-P10.2CH-AdminWebPreviewSmoke.ps1'
$repairReport = Join-Path $repoRootPath 'docs\P10\P10.2CH-Repair-AdminWebPreviewSmokeHarness.md'

if (-not (Test-Path -Path $adminWebRoot -PathType Container)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}

if (-not (Test-Path -Path $runnerPath -PathType Leaf)) {
    throw ('Preview smoke runner was not found: {0}' -f $runnerPath)
}

if (-not (Test-Path -Path $repairReport -PathType Leaf)) {
    throw ('Repair report was not found: {0}' -f $repairReport)
}

$runnerText = Get-Content -Path $runnerPath -Raw
if ([string]::IsNullOrWhiteSpace($runnerText)) {
    throw ('Preview smoke runner is empty: {0}' -f $runnerPath)
}

$requiredRunnerTokens = @(
    'Migration.Admin.Web',
    'npm',
    'preview'
)
foreach ($token in $requiredRunnerTokens) {
    if ($runnerText.IndexOf($token, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw ('Preview smoke runner is missing expected capability token: {0}' -f $token)
    }
}

$reportText = Get-Content -Path $repairReport -Raw
if ($reportText.IndexOf('P10.2CH Repair', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw ('Repair report does not contain the expected heading: {0}' -f $repairReport)
}

Write-Host 'P10.2CH Repair Admin Web preview smoke harness validation passed.'
