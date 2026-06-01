Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    $current = $PSScriptRoot
    while ($null -ne $current -and $current.Length -gt 0) {
        $adminPath = [System.IO.Path]::Combine($current, 'src', 'Admin', 'Migration.Admin.Web')
        if (Test-Path -Path $adminPath -PathType Container) {
            return $current
        }
        $parent = [System.IO.Directory]::GetParent($current)
        if ($null -eq $parent) {
            break
        }
        $current = $parent.FullName
    }
    throw 'Unable to locate repository root from script location.'
}

$repoRoot = Get-RepositoryRoot
$adminSrc = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src')
$reportPath = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10', 'P10.2BG-AdminWebConsolidationResidualClassification.md')
$appTsx = [System.IO.Path]::Combine($adminSrc, 'App.tsx')

if (-not (Test-Path -Path $adminSrc -PathType Container)) {
    throw ('Canonical Admin Web src folder was not found: {0}' -f $adminSrc)
}

if (-not (Test-Path -Path $appTsx -PathType Leaf)) {
    throw ('App.tsx was not found: {0}' -f $appTsx)
}

if (-not (Test-Path -Path $reportPath -PathType Leaf)) {
    throw ('Expected report was not found: {0}' -f $reportPath)
}

$reportLines = @(Get-Content -Path $reportPath -ErrorAction Stop)
$reportText = [string]::Join([Environment]::NewLine, $reportLines)
if (-not $reportText.Contains('P10.2BG - Admin Web Consolidation Residual Classification')) {
    throw 'Report title was not found.'
}

$expectedFolders = @('features', 'components')
foreach ($folder in $expectedFolders) {
    $path = [System.IO.Path]::Combine($adminSrc, $folder)
    if (-not (Test-Path -Path $path -PathType Container)) {
        throw ('Expected canonical folder was not found: {0}' -f $path)
    }
}

$scriptRoot = [System.IO.Path]::Combine($repoRoot, 'tools', 'p10', 'P10.2BG')
$applyScript = [System.IO.Path]::Combine($scriptRoot, 'Apply-P10.2BG-AdminWebConsolidationResidualClassification.ps1')
$testScript = [System.IO.Path]::Combine($scriptRoot, 'Test-P10.2BG-AdminWebConsolidationResidualClassification.ps1')
if (-not (Test-Path -Path $applyScript -PathType Leaf)) {
    throw ('Apply script was not found: {0}' -f $applyScript)
}
if (-not (Test-Path -Path $testScript -PathType Leaf)) {
    throw ('Test script was not found: {0}' -f $testScript)
}

Write-Host 'P10.2BG Admin Web consolidation residual classification validation passed.'
