Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$docsRoot = Join-Path $repoRoot 'docs\P10'
if (-not (Test-Path $docsRoot)) {
    New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null
}

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.3E Repair2 - Admin Web Operator Workflow Acceptance')
[void]$report.Add('')
[void]$report.Add('Purpose: repair the operator workflow runtime runner so local HTTPS Admin API probes do not depend on a PowerShell certificate callback script block.')
[void]$report.Add('')
[void]$report.Add('The runner uses curl.exe with -k and --max-time for API probes, which avoids the PS5.1 runspace issue caused by ServerCertificateValidationCallback script blocks.')
[void]$report.Add('')
[void]$report.Add('No Admin Web or Admin API source files are modified by this set.')

$reportPath = Join-Path $docsRoot 'P10.3E-Repair2-AdminWebOperatorWorkflowAcceptance.md'
Set-Content -Path $reportPath -Value $report.ToArray() -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.3E Repair2 Admin Web operator workflow acceptance applied.'
