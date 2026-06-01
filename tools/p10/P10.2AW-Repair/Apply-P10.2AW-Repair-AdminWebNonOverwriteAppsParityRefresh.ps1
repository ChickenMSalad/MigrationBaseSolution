Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$repoRoot = $repoRoot.ProviderPath

$appsRoot = Join-Path $repoRoot 'apps\migration-admin-ui\src'
$adminRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web\src'
$docsRoot = Join-Path $repoRoot 'docs\P10'
$reportPath = Join-Path $docsRoot 'P10.2AW-Repair-AdminWebNonOverwriteAppsParityRefresh.md'

if (-not (Test-Path -Path $appsRoot -PathType Container)) {
    throw ('Apps source root was not found: {0}' -f $appsRoot)
}
if (-not (Test-Path -Path $adminRoot -PathType Container)) {
    throw ('Canonical Admin Web source root was not found: {0}' -f $adminRoot)
}
if (-not (Test-Path -Path $docsRoot -PathType Container)) {
    New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null
}

$report = New-Object System.Collections.ArrayList
$null = $report.Add('# P10.2AW Repair - Admin Web Non-Overwrite Apps Parity Refresh')
$null = $report.Add('')
$null = $report.Add('Repairs the P10.2AW report helper failure. This script does not overwrite canonical Admin Web files.')
$null = $report.Add('')
$null = $report.Add('## Source Roots')
$null = $report.Add(('- Apps source: `{0}`' -f $appsRoot))
$null = $report.Add(('- Canonical source: `{0}`' -f $adminRoot))
$null = $report.Add('')

$segments = @('features', 'components', 'auth', 'lib')
$copiedCount = 0
$existingCount = 0
$missingSegmentCount = 0

foreach ($segment in $segments) {
    $sourceSegment = Join-Path $appsRoot $segment
    $targetSegment = Join-Path $adminRoot $segment
    $null = $report.Add(('## Segment `{0}`' -f $segment))

    if (-not (Test-Path -Path $sourceSegment -PathType Container)) {
        $missingSegmentCount++
        $null = $report.Add(('- Source segment missing: `{0}`' -f $sourceSegment))
        $null = $report.Add('')
        continue
    }

    if (-not (Test-Path -Path $targetSegment -PathType Container)) {
        New-Item -ItemType Directory -Path $targetSegment -Force | Out-Null
    }

    $sourceFiles = @(Get-ChildItem -Path $sourceSegment -Recurse -File)
    if ($sourceFiles.Length -eq 0) {
        $null = $report.Add('- No files found in source segment.')
        $null = $report.Add('')
        continue
    }

    foreach ($sourceFile in $sourceFiles) {
        $relativePath = $sourceFile.FullName.Substring($sourceSegment.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
        $targetPath = Join-Path $targetSegment $relativePath
        $targetDirectory = Split-Path -Parent $targetPath

        if (-not (Test-Path -Path $targetDirectory -PathType Container)) {
            New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
        }

        if (Test-Path -Path $targetPath -PathType Leaf) {
            $existingCount++
            continue
        }

        Copy-Item -Path $sourceFile.FullName -Destination $targetPath -Force:$false
        $copiedCount++
        $null = $report.Add(('- Copied `{0}`' -f (Join-Path $segment $relativePath)))
    }

    if ($copiedCount -eq 0) {
        $null = $report.Add('- No new files copied in this run.')
    }
    $null = $report.Add('')
}

$null = $report.Add('## Summary')
$null = $report.Add(('- Copied files: {0}' -f $copiedCount))
$null = $report.Add(('- Already existing files skipped: {0}' -f $existingCount))
$null = $report.Add(('- Missing source segments: {0}' -f $missingSegmentCount))
$null = $report.Add('')
$null = $report.Add('Canonical Admin Web remains the only deployable Admin UI. The apps tree remains reference/source only.')

[System.IO.File]::WriteAllLines($reportPath, [string[]]$report)
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2AW Repair Admin Web non-overwrite apps parity refresh applied.'
