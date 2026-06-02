Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$repoRoot = $scriptRoot
while ($true) {
    if ([string]::IsNullOrWhiteSpace($repoRoot)) { throw 'Unable to locate repository root.' }
    $candidate = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src', 'features')
    if (Test-Path -Path $candidate -PathType Container) { break }
    $parent = Split-Path -Parent $repoRoot
    if ($parent -eq $repoRoot) { throw 'Unable to locate repository root containing src/Admin/Migration.Admin.Web/src/features.' }
    $repoRoot = $parent
}

$adminRoot = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web')
$sourceRoot = [System.IO.Path]::Combine($adminRoot, 'src')
$featuresRoot = [System.IO.Path]::Combine($sourceRoot, 'features')
$referenceFeaturesRoot = [System.IO.Path]::Combine($adminRoot, 'reference', 'apps-migration-admin-ui', 'src', 'features')
$docsRoot = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10')
$reportPath = [System.IO.Path]::Combine($docsRoot, 'P10.2BP-AdminWebRemainingAppsFeatureQuarantine.Report.md')

$canonicalGroups = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
[void]$canonicalGroups.Add('connectors')
[void]$canonicalGroups.Add('governance')
[void]$canonicalGroups.Add('operations')
[void]$canonicalGroups.Add('platform')
[void]$canonicalGroups.Add('security')

$sourceFiles = @(
    Get-ChildItem -Path $sourceRoot -Recurse -File -Include '*.ts','*.tsx','*.js','*.jsx' -ErrorAction SilentlyContinue |
        Where-Object {
            $full = $_.FullName
            ($full -notlike '*\reference\*') -and
            ($full -notlike '*\node_modules\*') -and
            ($full -notlike '*\dist\*') -and
            ($full -notlike '*\build\*')
        }
)

function Test-CompiledSourceReferencesFeatureName {
    param([Parameter(Mandatory=$true)][string]$FeatureName)

    $needle1 = ('features/{0}' -f $FeatureName)
    $needle2 = ('features\{0}' -f $FeatureName)
    $needle3 = ('/{0}/' -f $FeatureName)
    $needle4 = ('\{0}\' -f $FeatureName)

    foreach ($file in $sourceFiles) {
        if (-not (Test-Path -Path $file.FullName -PathType Leaf)) { continue }
        $content = Get-Content -Path $file.FullName -Raw
        if ($null -eq $content) { continue }

        if (($content.IndexOf($needle1, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) -or
            ($content.IndexOf($needle2, [System.StringComparison]::OrdinalIgnoreCase) -ge 0)) { return $true }

        $selfSegment = [System.IO.Path]::DirectorySeparatorChar + $FeatureName + [System.IO.Path]::DirectorySeparatorChar
        if ($file.FullName.IndexOf($selfSegment, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) { continue }

        if (($content.IndexOf($needle3, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) -or
            ($content.IndexOf($needle4, [System.StringComparison]::OrdinalIgnoreCase) -ge 0)) { return $true }
    }
    return $false
}

if (-not (Test-Path -Path $featuresRoot -PathType Container)) { throw ('Admin Web features root not found: {0}' -f $featuresRoot) }
New-Item -ItemType Directory -Path $referenceFeaturesRoot -Force | Out-Null
New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2BP - Admin Web Remaining Apps Feature Quarantine')
[void]$report.Add('')
[void]$report.Add(('Repository root: `{0}`' -f $repoRoot))
[void]$report.Add(('Admin Web root: `{0}`' -f $adminRoot))
[void]$report.Add(('Source features root: `{0}`' -f $featuresRoot))
[void]$report.Add(('Reference features root: `{0}`' -f $referenceFeaturesRoot))
[void]$report.Add('')
[void]$report.Add('## Feature folders evaluated')
[void]$report.Add('')

function Move-DirectoryContentsNoOverwrite {
    param(
        [Parameter(Mandatory=$true)][string]$SourceDirectory,
        [Parameter(Mandatory=$true)][string]$TargetDirectory,
        [Parameter(Mandatory=$true)][string]$FeatureName
    )

    New-Item -ItemType Directory -Path $TargetDirectory -Force | Out-Null
    $items = @(Get-ChildItem -Path $SourceDirectory -Force -ErrorAction SilentlyContinue)
    foreach ($item in $items) {
        $targetPath = [System.IO.Path]::Combine($TargetDirectory, $item.Name)
        if (Test-Path -Path $targetPath) {
            [void]$report.Add(('- Conflict left in place for `{0}`: `{1}` already exists.' -f $FeatureName, $targetPath))
            continue
        }
        Move-Item -Path $item.FullName -Destination $targetPath
        [void]$report.Add(('- Moved `{0}` item: `{1}` -> `{2}`' -f $FeatureName, $item.FullName, $targetPath))
    }

    $remaining = @(Get-ChildItem -Path $SourceDirectory -Force -ErrorAction SilentlyContinue)
    if ($remaining.Length -eq 0) {
        Remove-Item -Path $SourceDirectory -Force
        [void]$report.Add(('- Removed empty source feature folder: `{0}`' -f $SourceDirectory))
    } else {
        [void]$report.Add(('- Source feature folder retained because conflicts remain: `{0}`' -f $SourceDirectory))
    }
}

$featureDirectories = @(Get-ChildItem -Path $featuresRoot -Directory -ErrorAction SilentlyContinue | Sort-Object -Property Name)
$movedCount = 0
$skippedCount = 0

foreach ($directory in $featureDirectories) {
    $name = $directory.Name
    if ($canonicalGroups.Contains($name)) {
        [void]$report.Add(('- Preserved canonical group: `{0}`' -f $name))
        continue
    }

    if (Test-CompiledSourceReferencesFeatureName -FeatureName $name) {
        $skippedCount += 1
        [void]$report.Add(('- Skipped referenced ungrouped feature folder: `{0}`' -f $name))
        continue
    }

    $target = [System.IO.Path]::Combine($referenceFeaturesRoot, $name)
    Move-DirectoryContentsNoOverwrite -SourceDirectory $directory.FullName -TargetDirectory $target -FeatureName $name
    $movedCount += 1
}

[void]$report.Add('')
[void]$report.Add('## Summary')
[void]$report.Add('')
[void]$report.Add(('- Ungrouped feature folders moved or merged: {0}' -f $movedCount))
[void]$report.Add(('- Referenced ungrouped feature folders skipped: {0}' -f $skippedCount))
[System.IO.File]::WriteAllLines($reportPath, $report.ToArray(), [System.Text.Encoding]::UTF8)
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2BP Admin Web remaining apps feature quarantine applied.'
