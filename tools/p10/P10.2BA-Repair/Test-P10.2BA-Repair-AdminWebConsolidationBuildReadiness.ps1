Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $current = $PSScriptRoot
    while ($true) {
        if ([string]::IsNullOrWhiteSpace($current)) {
            throw 'Unable to locate repository root.'
        }

        $adminWeb = [System.IO.Path]::Combine($current, 'src', 'Admin', 'Migration.Admin.Web')
        $gitDir = [System.IO.Path]::Combine($current, '.git')
        if ((Test-Path -Path $adminWeb -PathType Container) -or (Test-Path -Path $gitDir -PathType Container)) {
            return $current
        }

        $parent = Split-Path -Path $current -Parent
        if ($parent -eq $current) {
            throw 'Unable to locate repository root.'
        }
        $current = $parent
    }
}

$repoRoot = Get-RepoRoot
$adminWebRoot = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web')
$sourceRoot = [System.IO.Path]::Combine($adminWebRoot, 'src')
$reportPath = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10', 'P10.2BA-Repair-AdminWebConsolidationBuildReadiness.Report.md')

$requiredFolders = @(
    $adminWebRoot,
    $sourceRoot,
    [System.IO.Path]::Combine($sourceRoot, 'features'),
    [System.IO.Path]::Combine($sourceRoot, 'components')
)
foreach ($folder in $requiredFolders) {
    if (-not (Test-Path -Path $folder -PathType Container)) {
        throw ('Required folder missing: {0}' -f $folder)
    }
}

$requiredFiles = @(
    [System.IO.Path]::Combine($adminWebRoot, 'package.json'),
    [System.IO.Path]::Combine($sourceRoot, 'App.tsx'),
    [System.IO.Path]::Combine($sourceRoot, 'main.tsx'),
    $reportPath
)
foreach ($file in $requiredFiles) {
    if (-not (Test-Path -Path $file -PathType Leaf)) {
        throw ('Required file missing: {0}' -f $file)
    }
}

$reportText = Get-Content -Path $reportPath -Raw
$requiredText = @(
    '# P10.2BA Repair - Admin Web Consolidation Build Readiness Report',
    '## Required Surface',
    '## Remaining Flat Files',
    '## Apps Reference Scan',
    '## Result'
)
foreach ($text in $requiredText) {
    if ($reportText -notlike ('*' + $text + '*')) {
        throw ('Expected report text missing: {0}' -f $text)
    }
}

Write-Host 'P10.2BA Repair build-readiness validation passed.'
