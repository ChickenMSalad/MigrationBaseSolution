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

if (-not (Test-Path -Path $reportPath -PathType Leaf)) { throw ('Expected report not found: {0}' -f $reportPath) }
if (-not (Test-Path -Path $featuresRoot -PathType Container)) { throw ('Expected features root not found: {0}' -f $featuresRoot) }
if (-not (Test-Path -Path $referenceFeaturesRoot -PathType Container)) { throw ('Expected reference features root not found: {0}' -f $referenceFeaturesRoot) }

$featureDirectories = @(Get-ChildItem -Path $featuresRoot -Directory -ErrorAction SilentlyContinue | Sort-Object -Property Name)
$unreferencedUngrouped = New-Object 'System.Collections.Generic.List[string]'
foreach ($directory in $featureDirectories) {
    $name = $directory.Name
    if ($canonicalGroups.Contains($name)) { continue }
    if (-not (Test-CompiledSourceReferencesFeatureName -FeatureName $name)) { [void]$unreferencedUngrouped.Add($name) }
}

if ($unreferencedUngrouped.Count -gt 0) {
    $joined = [string]::Join(', ', $unreferencedUngrouped.ToArray())
    throw ('Unreferenced ungrouped feature folders remain in compile scope: {0}' -f $joined)
}

Write-Host 'P10.2BP Admin Web remaining apps feature quarantine validation passed.'
