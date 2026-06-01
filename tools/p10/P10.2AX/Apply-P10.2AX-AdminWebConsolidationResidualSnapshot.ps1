Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..')).Path
$adminSrc = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web\src'
$appsSrc = Join-Path $repoRoot 'apps\migration-admin-ui\src'
$docsDir = Join-Path $repoRoot 'docs\P10'
$reportPath = Join-Path $docsDir 'P10.2AX-AdminWebConsolidationResidualSnapshot.md'
$jsonPath = Join-Path $docsDir 'P10.2AX-AdminWebConsolidationResidualSnapshot.json'

if (-not (Test-Path -Path $adminSrc -PathType Container)) {
    throw ('Canonical Admin Web source folder not found: {0}' -f $adminSrc)
}
if (-not (Test-Path -Path $appsSrc -PathType Container)) {
    throw ('Reference apps Admin UI source folder not found: {0}' -f $appsSrc)
}
if (-not (Test-Path -Path $docsDir -PathType Container)) {
    New-Item -ItemType Directory -Path $docsDir -Force | Out-Null
}

$report = New-Object System.Collections.ArrayList
[void]$report.Add('# P10.2AX - Admin Web Consolidation Residual Snapshot')
[void]$report.Add('')
[void]$report.Add('Purpose: capture the current canonical Admin Web state after the accelerated apps parity and feature consolidation batches.')
[void]$report.Add('')
[void]$report.Add('This set is intentionally report-only. It does not move, rewrite, or delete Admin Web source files.')
[void]$report.Add('')

$relativeRoots = @('features', 'components', 'auth', 'lib')
$appsOnly = New-Object System.Collections.ArrayList
$canonicalOnly = New-Object System.Collections.ArrayList
$shared = New-Object System.Collections.ArrayList

foreach ($relativeRoot in $relativeRoots) {
    $appsRoot = Join-Path $appsSrc $relativeRoot
    $adminRoot = Join-Path $adminSrc $relativeRoot

    if ((Test-Path -Path $appsRoot -PathType Container) -and (Test-Path -Path $adminRoot -PathType Container)) {
        $appsFiles = @(Get-ChildItem -Path $appsRoot -Recurse -File | ForEach-Object { $_.FullName.Substring($appsRoot.Length).TrimStart('\') })
        $adminFiles = @(Get-ChildItem -Path $adminRoot -Recurse -File | ForEach-Object { $_.FullName.Substring($adminRoot.Length).TrimStart('\') })
        foreach ($file in $appsFiles) {
            if ($adminFiles -contains $file) {
                [void]$shared.Add((Join-Path $relativeRoot $file))
            }
            else {
                [void]$appsOnly.Add((Join-Path $relativeRoot $file))
            }
        }
        foreach ($file in $adminFiles) {
            if (-not ($appsFiles -contains $file)) {
                [void]$canonicalOnly.Add((Join-Path $relativeRoot $file))
            }
        }
    }
    elseif (Test-Path -Path $appsRoot -PathType Container) {
        $appsFiles = @(Get-ChildItem -Path $appsRoot -Recurse -File | ForEach-Object { Join-Path $relativeRoot ($_.FullName.Substring($appsRoot.Length).TrimStart('\')) })
        foreach ($file in $appsFiles) { [void]$appsOnly.Add($file) }
    }
    elseif (Test-Path -Path $adminRoot -PathType Container) {
        $adminFiles = @(Get-ChildItem -Path $adminRoot -Recurse -File | ForEach-Object { Join-Path $relativeRoot ($_.FullName.Substring($adminRoot.Length).TrimStart('\')) })
        foreach ($file in $adminFiles) { [void]$canonicalOnly.Add($file) }
    }
}

$flatFolders = @('pages', 'api', 'types')
$flatFiles = New-Object System.Collections.ArrayList
foreach ($flatFolder in $flatFolders) {
    $folderPath = Join-Path $adminSrc $flatFolder
    if (Test-Path -Path $folderPath -PathType Container) {
        $files = @(Get-ChildItem -Path $folderPath -Recurse -File)
        foreach ($file in $files) {
            [void]$flatFiles.Add($file.FullName.Substring($adminSrc.Length).TrimStart('\'))
        }
    }
}

$appsImportFiles = New-Object System.Collections.ArrayList
$tsFiles = @(Get-ChildItem -Path $adminSrc -Recurse -File -Include '*.ts','*.tsx')
foreach ($tsFile in $tsFiles) {
    $content = Get-Content -Path $tsFile.FullName -Raw
    if ($content.IndexOf('apps/migration-admin-ui', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $content.IndexOf('apps\migration-admin-ui', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        [void]$appsImportFiles.Add($tsFile.FullName.Substring($adminSrc.Length).TrimStart('\'))
    }
}

$featureFolders = New-Object System.Collections.ArrayList
$featuresRoot = Join-Path $adminSrc 'features'
if (Test-Path -Path $featuresRoot -PathType Container) {
    $dirs = @(Get-ChildItem -Path $featuresRoot -Directory -Recurse)
    foreach ($dir in $dirs) {
        [void]$featureFolders.Add($dir.FullName.Substring($featuresRoot.Length).TrimStart('\'))
    }
}

[void]$report.Add('## Summary')
[void]$report.Add('')
[void]$report.Add(('- Apps-only residual files under tracked roots: {0}' -f $appsOnly.Count))
[void]$report.Add(('- Canonical-only files under tracked roots: {0}' -f $canonicalOnly.Count))
[void]$report.Add(('- Shared files under tracked roots: {0}' -f $shared.Count))
[void]$report.Add(('- Remaining canonical flat files under pages/api/types: {0}' -f $flatFiles.Count))
[void]$report.Add(('- Canonical files referencing /apps/migration-admin-ui: {0}' -f $appsImportFiles.Count))
[void]$report.Add(('- Canonical feature folder count: {0}' -f $featureFolders.Count))
[void]$report.Add('')

[void]$report.Add('## Remaining Canonical Flat Files')
[void]$report.Add('')
if ($flatFiles.Count -eq 0) {
    [void]$report.Add('- None found.')
}
else {
    foreach ($item in @($flatFiles | Sort-Object)) { [void]$report.Add(('- `{0}`' -f $item.Replace('\','/'))) }
}
[void]$report.Add('')

[void]$report.Add('## Apps-only Residual Files')
[void]$report.Add('')
if ($appsOnly.Count -eq 0) {
    [void]$report.Add('- None found under features/components/auth/lib.')
}
else {
    foreach ($item in @($appsOnly | Sort-Object)) { [void]$report.Add(('- `{0}`' -f $item.Replace('\','/'))) }
}
[void]$report.Add('')

[void]$report.Add('## Canonical Files Referencing apps/migration-admin-ui')
[void]$report.Add('')
if ($appsImportFiles.Count -eq 0) {
    [void]$report.Add('- None found.')
}
else {
    foreach ($item in @($appsImportFiles | Sort-Object)) { [void]$report.Add(('- `{0}`' -f $item.Replace('\','/'))) }
}
[void]$report.Add('')

[void]$report.Add('## Next Batch Recommendation')
[void]$report.Add('')
[void]$report.Add('Use this report to drive the next implementation batch from actual local state. Prefer bundling by the remaining flat folder inventory instead of moving one feature at a time.')
[void]$report.Add('')

Set-Content -Path $reportPath -Value $report -Encoding UTF8

$result = [ordered]@{
    AdminSrc = $adminSrc
    AppsSrc = $appsSrc
    AppsOnly = @($appsOnly | Sort-Object)
    CanonicalOnly = @($canonicalOnly | Sort-Object)
    Shared = @($shared | Sort-Object)
    RemainingFlatFiles = @($flatFiles | Sort-Object)
    AppsImportReferences = @($appsImportFiles | Sort-Object)
    FeatureFolders = @($featureFolders | Sort-Object)
}
$result | ConvertTo-Json -Depth 8 | Set-Content -Path $jsonPath -Encoding UTF8

Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host ('Wrote JSON: {0}' -f $jsonPath)
Write-Host 'P10.2AX Admin Web consolidation residual snapshot applied.'
