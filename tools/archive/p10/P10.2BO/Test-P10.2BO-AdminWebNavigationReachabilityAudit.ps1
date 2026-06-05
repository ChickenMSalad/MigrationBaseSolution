Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$adminWeb = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$sourceRoot = Join-Path $adminWeb 'src'
$appPath = Join-Path $sourceRoot 'App.tsx'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2BO-AdminWebNavigationReachabilityAudit.Report.md'

if (-not (Test-Path -Path $appPath -PathType Leaf)) {
    throw ('App.tsx was not found: {0}' -f $appPath)
}
if (-not (Test-Path -Path $reportPath -PathType Leaf)) {
    throw ('Expected report was not found: {0}' -f $reportPath)
}

$appText = [System.IO.File]::ReadAllText($appPath)
if ($appText -match 'from\s+["''][^"'']+\.tsx["'']') {
    throw 'App.tsx contains an import path ending with .tsx.'
}
if ($appText -match '\.tsx["'']') {
    throw 'App.tsx contains a .tsx extension-bearing import source.'
}

$reportText = [System.IO.File]::ReadAllText($reportPath)
$requiredHeadings = @(
    '# P10.2BO - Admin Web Navigation Reachability Audit',
    '## App.tsx route paths',
    '## App.tsx route targets',
    '## Feature pages under canonical src/features',
    '## Feature pages without detected App.tsx route target',
    '## Route targets without detected canonical feature page',
    '## Candidate navigation files'
)
foreach ($heading in $requiredHeadings) {
    if (-not $reportText.Contains($heading)) {
        throw ('Expected report heading missing: {0}' -f $heading)
    }
}

$toolPath = Join-Path $repoRoot 'tools\p10\P10.2BO'
$toolFiles = @(Get-ChildItem -Path $toolPath -File -Filter '*.ps1')
if ($toolFiles.Length -lt 2) {
    throw ('Expected apply and test scripts under: {0}' -f $toolPath)
}
foreach ($file in $toolFiles) {
    $text = [System.IO.File]::ReadAllText($file.FullName)
    if ($text.Contains('@(' + [Environment]::NewLine + '    @(')) {
        throw ('Nested array literal pattern found in {0}' -f $file.FullName)
    }
    if ($text.Contains('$Label:') -or $text.Contains('$Path:') -or $text.Contains('$Source:')) {
        throw ('Unsafe variable interpolation token found in {0}' -f $file.FullName)
    }
}

Write-Host 'P10.2BO Admin Web navigation reachability audit validation passed.'
