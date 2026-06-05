Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.3A-AdminWebRuntimeAcceptanceHarness.md'
$runPath = Join-Path $scriptRoot 'Run-P10.3A-AdminWebRuntimeAcceptance.ps1'
$applyPath = Join-Path $scriptRoot 'Apply-P10.3A-AdminWebRuntimeAcceptanceHarness.ps1'

if (-not (Test-Path -LiteralPath $adminWebRoot)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}

if (-not (Test-Path -LiteralPath $reportPath)) {
    throw ('Expected report was not found: {0}' -f $reportPath)
}

if (-not (Test-Path -LiteralPath $runPath)) {
    throw ('Expected runtime runner was not found: {0}' -f $runPath)
}

$reportText = Get-Content -LiteralPath $reportPath -Raw
if ($reportText -notlike '*Runtime Acceptance Harness*') {
    throw ('Report does not contain the expected title: {0}' -f $reportPath)
}

$scriptPaths = New-Object 'System.Collections.Generic.List[string]'
[void]$scriptPaths.Add($applyPath)
[void]$scriptPaths.Add($runPath)

foreach ($path in $scriptPaths) {
    $text = Get-Content -LiteralPath $path -Raw
    try {
        [void][scriptblock]::Create($text)
    }
    catch {
        throw ('PowerShell script failed parse validation: {0}. {1}' -f $path, $_.Exception.Message)
    }
}

Write-Host 'P10.3A Admin Web runtime acceptance harness validation passed.'
