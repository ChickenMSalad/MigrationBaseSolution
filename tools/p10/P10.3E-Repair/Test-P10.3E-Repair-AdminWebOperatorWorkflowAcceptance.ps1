Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..\..')
$toolRoot = Join-Path $repoRoot 'tools\p10\P10.3E-Repair'
$docsRoot = Join-Path $repoRoot 'docs\P10'

$requiredFiles = New-Object 'System.Collections.Generic.List[string]'
[void]$requiredFiles.Add((Join-Path $toolRoot 'Apply-P10.3E-Repair-AdminWebOperatorWorkflowAcceptance.ps1'))
[void]$requiredFiles.Add((Join-Path $toolRoot 'Test-P10.3E-Repair-AdminWebOperatorWorkflowAcceptance.ps1'))
[void]$requiredFiles.Add((Join-Path $toolRoot 'Run-P10.3E-Repair-AdminWebOperatorWorkflowAcceptance.ps1'))
[void]$requiredFiles.Add((Join-Path $docsRoot 'P10.3E-Repair-AdminWebOperatorWorkflowAcceptance.md'))

foreach ($path in $requiredFiles) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw ('Required file was not found: {0}' -f $path)
    }
}

$scriptFiles = New-Object 'System.Collections.Generic.List[string]'
[void]$scriptFiles.Add((Join-Path $toolRoot 'Apply-P10.3E-Repair-AdminWebOperatorWorkflowAcceptance.ps1'))
[void]$scriptFiles.Add((Join-Path $toolRoot 'Test-P10.3E-Repair-AdminWebOperatorWorkflowAcceptance.ps1'))
[void]$scriptFiles.Add((Join-Path $toolRoot 'Run-P10.3E-Repair-AdminWebOperatorWorkflowAcceptance.ps1'))

foreach ($scriptPath in $scriptFiles) {
    $content = Get-Content -LiteralPath $scriptPath -Raw
    [void][scriptblock]::Create($content)
}

$runner = Join-Path $toolRoot 'Run-P10.3E-Repair-AdminWebOperatorWorkflowAcceptance.ps1'
$runnerText = Get-Content -LiteralPath $runner -Raw

if ($runnerText -notlike '*param(*') {
    throw 'Runner does not contain a param block.'
}

if ($runnerText -notlike '*SecurityProtocol*Tls12*') {
    throw 'Runner does not configure TLS 1.2 for local HTTPS probes.'
}

if ($runnerText -notlike '*ServerCertificateValidationCallback*') {
    throw 'Runner does not configure local development certificate handling.'
}

if ($runnerText -notlike '*TimeoutSec*') {
    throw 'Runner does not use bounded request timeouts.'
}

Write-Host 'P10.3E Repair Admin Web operator workflow acceptance validation passed.'
