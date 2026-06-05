Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($scriptRoot, '..', '..', '..'))

$canonicalSrc = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src')
$appsSrc = [System.IO.Path]::Combine($repoRoot, 'apps', 'migration-admin-ui', 'src')
$reportPath = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10', 'P10.2AY-AdminWebCanonicalParityStatus.md')
$applyPath = [System.IO.Path]::Combine($repoRoot, 'tools', 'p10', 'P10.2AY', 'Apply-P10.2AY-AdminWebCanonicalParityStatus.ps1')
$testPath = [System.IO.Path]::Combine($repoRoot, 'tools', 'p10', 'P10.2AY', 'Test-P10.2AY-AdminWebCanonicalParityStatus.ps1')

$requiredFiles = @($applyPath, $testPath, $reportPath)
foreach ($path in $requiredFiles) {
    if (-not (Test-Path -Path $path -PathType Leaf)) {
        throw ('Required file was not found: {0}' -f $path)
    }
}

$requiredFolders = @($canonicalSrc, $appsSrc)
foreach ($path in $requiredFolders) {
    if (-not (Test-Path -Path $path -PathType Container)) {
        throw ('Required folder was not found: {0}' -f $path)
    }
}

$reportContent = Get-Content -Path $reportPath -Raw
if ($reportContent -notlike '*# P10.2AY - Admin Web Canonical Parity Status*') {
    throw ('Report heading missing from {0}' -f $reportPath)
}
if ($reportContent -notlike '*Apps reference files missing from canonical source*') {
    throw ('Report missing apps parity section: {0}' -f $reportPath)
}
if ($reportContent -notlike '*Remaining canonical flat folders*') {
    throw ('Report missing flat folder section: {0}' -f $reportPath)
}

$scriptFiles = @($applyPath, $testPath)
$forbiddenOne = 'function ' + 'Add-Line'
$forbiddenTwo = 'function ' + 'Add-ReportLine'
$unsafeLabel = '$' + 'Label:'
$unsafePath = '$' + 'Path:'
foreach ($path in $scriptFiles) {
    $content = Get-Content -Path $path -Raw
    if ($content.Contains($forbiddenOne) -or $content.Contains($forbiddenTwo)) {
        throw ('Disallowed report helper function found in {0}' -f $path)
    }
    if ($content.Contains($unsafeLabel) -or $content.Contains($unsafePath)) {
        throw ('Unsafe variable interpolation token found in {0}' -f $path)
    }
}

Write-Host 'P10.2AY Admin Web canonical parity status validation passed.'
