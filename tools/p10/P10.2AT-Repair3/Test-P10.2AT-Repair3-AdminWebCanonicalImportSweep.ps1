Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($scriptRoot, '..', '..', '..'))
$adminSrc = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src')
$reportPath = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10', 'P10.2AT-Repair3-CanonicalImportSweepReport.md')

if (-not (Test-Path -Path $adminSrc -PathType Container)) {
    throw ('Canonical Admin Web src folder was not found: {0}' -f $adminSrc)
}

if (-not (Test-Path -Path $reportPath -PathType Leaf)) {
    throw ('Expected report was not found. Run the apply script first: {0}' -f $reportPath)
}

$reportContent = [System.IO.File]::ReadAllText($reportPath)
if ($reportContent -notmatch '# P10\.2AT Repair3 - Canonical Import Sweep Report') {
    throw ('Report header was not found in {0}' -f $reportPath)
}

if ($reportContent -notmatch 'Scanned source files:') {
    throw ('Report scan summary was not found in {0}' -f $reportPath)
}

if ($reportContent -notmatch 'Unresolved relative import statements:') {
    throw ('Report unresolved import summary was not found in {0}' -f $reportPath)
}

$toolRoot = [System.IO.Path]::Combine($repoRoot, 'tools', 'p10', 'P10.2AT-Repair3')
$toolFiles = @(Get-ChildItem -Path $toolRoot -File -Filter '*.ps1')
foreach ($toolFile in $toolFiles) {
    $content = [System.IO.File]::ReadAllText($toolFile.FullName)
    if ($content -match '\$[A-Za-z_][A-Za-z0-9_]*:') {
        throw ('Unsafe variable-colon interpolation pattern found in {0}' -f $toolFile.FullName)
    }
    if ($content -match '@\(\s*@\(') {
        throw ('Nested array literal pattern found in {0}' -f $toolFile.FullName)
    }
    if ($content -match 'param\s*\(') {
        throw ('AT Repair3 scripts should avoid helper param binding risks: {0}' -f $toolFile.FullName)
    }
}

Write-Host 'P10.2AT Repair3 canonical import sweep validation passed.'
