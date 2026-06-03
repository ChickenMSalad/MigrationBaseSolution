Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$applyPath = Join-Path $scriptRoot 'Apply-P10.3H-AdminWebManualUxAcceptanceChecklist.ps1'
$runPath = Join-Path $scriptRoot 'Run-P10.3H-AdminWebManualUxAcceptanceChecklist.ps1'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.3H-AdminWebManualUxAcceptanceChecklist.md'

$required = New-Object 'System.Collections.Generic.List[string]'
[void]$required.Add($applyPath)
[void]$required.Add($runPath)
[void]$required.Add($reportPath)
foreach ($path in $required) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw ('Required file missing: {0}' -f $path)
    }
}

$scriptFiles = New-Object 'System.Collections.Generic.List[string]'
[void]$scriptFiles.Add($applyPath)
[void]$scriptFiles.Add($runPath)
[void]$scriptFiles.Add($MyInvocation.MyCommand.Path)
foreach ($scriptFile in $scriptFiles) {
    $content = Get-Content -LiteralPath $scriptFile -Raw
    [void][scriptblock]::Create($content)
}

$report = Get-Content -LiteralPath $reportPath -Raw
if ($report.IndexOf('Manual checks', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw 'Checklist report is missing Manual checks section.'
}
if ($report.IndexOf('/runtime-dashboard', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw 'Checklist report is missing runtime-dashboard route.'
}
if ($report.IndexOf('deferred historical builder parity', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw 'Checklist report is missing deferred builder parity note.'
}
Write-Host 'P10.3H Admin Web manual UX acceptance checklist validation passed.'
