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
$featuresRoot = Join-Path $sourceRoot 'features'
$docsRoot = Join-Path $repoRoot 'docs\P10'
$reportPath = Join-Path $docsRoot 'P10.2BO-AdminWebNavigationReachabilityAudit.Report.md'

if (-not (Test-Path -Path $adminWeb -PathType Container)) {
    throw ('Admin Web folder was not found: {0}' -f $adminWeb)
}
if (-not (Test-Path -Path $appPath -PathType Leaf)) {
    throw ('App.tsx was not found: {0}' -f $appPath)
}
if (-not (Test-Path -Path $featuresRoot -PathType Container)) {
    throw ('Features folder was not found: {0}' -f $featuresRoot)
}
if (-not (Test-Path -Path $docsRoot -PathType Container)) {
    New-Item -Path $docsRoot -ItemType Directory -Force | Out-Null
}

$report = New-Object System.Collections.Generic.List[string]
[void]$report.Add('# P10.2BO - Admin Web Navigation Reachability Audit')
[void]$report.Add('')
[void]$report.Add('Report-only. No source files were changed.')
[void]$report.Add('')

$appText = [System.IO.File]::ReadAllText($appPath)

$routeMatches = @([regex]::Matches($appText, '<Route\s+path\s*=\s*"([^"]+)"'))
$routePaths = New-Object System.Collections.Generic.List[string]
foreach ($match in $routeMatches) {
    if ($null -ne $match -and $match.Groups.Count -gt 1) {
        $value = $match.Groups[1].Value
        if (-not [string]::IsNullOrWhiteSpace($value)) {
            [void]$routePaths.Add($value)
        }
    }
}

$routeTargetMatches = @([regex]::Matches($appText, 'element\s*=\s*\{\s*<([A-Za-z][A-Za-z0-9_]*)'))
$routeTargets = New-Object System.Collections.Generic.List[string]
foreach ($match in $routeTargetMatches) {
    if ($null -ne $match -and $match.Groups.Count -gt 1) {
        $value = $match.Groups[1].Value
        if (-not [string]::IsNullOrWhiteSpace($value)) {
            [void]$routeTargets.Add($value)
        }
    }
}

$featurePages = @(
    Get-ChildItem -Path $featuresRoot -Recurse -File -Filter '*.tsx' |
        Where-Object {
            $full = $_.FullName
            ($full -like '*\pages\*') -and
            ($full -notlike '*\reference\*') -and
            ($full -notlike '*\node_modules\*') -and
            ($full -notlike '*\dist\*')
        } |
        Sort-Object FullName
)

$featurePageNames = New-Object System.Collections.Generic.List[string]
foreach ($page in $featurePages) {
    $name = [System.IO.Path]::GetFileNameWithoutExtension($page.Name)
    if (-not [string]::IsNullOrWhiteSpace($name)) {
        [void]$featurePageNames.Add($name)
    }
}

[void]$report.Add('## App.tsx route paths')
if ($routePaths.Count -eq 0) {
    [void]$report.Add('- No `<Route path="...">` entries were detected by the local audit.')
} else {
    foreach ($path in ($routePaths | Sort-Object -Unique)) {
        [void]$report.Add(('- `{0}`' -f $path))
    }
}
[void]$report.Add('')

[void]$report.Add('## App.tsx route targets')
if ($routeTargets.Count -eq 0) {
    [void]$report.Add('- No route target components were detected by the local audit.')
} else {
    foreach ($target in ($routeTargets | Sort-Object -Unique)) {
        [void]$report.Add(('- `{0}`' -f $target))
    }
}
[void]$report.Add('')

[void]$report.Add('## Feature pages under canonical src/features')
if ($featurePages.Length -eq 0) {
    [void]$report.Add('- No feature pages were found under canonical `src/features`.')
} else {
    foreach ($page in $featurePages) {
        $relative = $page.FullName.Substring($sourceRoot.Length).TrimStart('\') -replace '\\','/'
        [void]$report.Add(('- `{0}`' -f $relative))
    }
}
[void]$report.Add('')

[void]$report.Add('## Feature pages without detected App.tsx route target')
$hiddenCount = 0
foreach ($name in ($featurePageNames | Sort-Object -Unique)) {
    if (-not ($routeTargets -contains $name)) {
        $hiddenCount++
        [void]$report.Add(('- `{0}`' -f $name))
    }
}
if ($hiddenCount -eq 0) {
    [void]$report.Add('- None detected.')
}
[void]$report.Add('')

[void]$report.Add('## Route targets without detected canonical feature page')
$missingCount = 0
foreach ($target in ($routeTargets | Sort-Object -Unique)) {
    if (($target -eq 'Navigate') -or ($target -eq 'Layout')) {
        continue
    }
    if (-not ($featurePageNames -contains $target)) {
        $missingCount++
        [void]$report.Add(('- `{0}`' -f $target))
    }
}
if ($missingCount -eq 0) {
    [void]$report.Add('- None detected.')
}
[void]$report.Add('')

[void]$report.Add('## Candidate navigation files')
$navFiles = @(
    Get-ChildItem -Path $sourceRoot -Recurse -File -Include '*.ts','*.tsx' |
        Where-Object {
            $full = $_.FullName
            ($full -notlike '*\node_modules\*') -and
            ($full -notlike '*\dist\*') -and
            ($full -notlike '*\reference\*') -and
            (($_.Name -like '*Nav*') -or ($_.Name -like '*Sidebar*') -or ($_.Name -like '*Layout*') -or ($_.Name -like '*Menu*'))
        } |
        Sort-Object FullName
)
if ($navFiles.Length -eq 0) {
    [void]$report.Add('- No candidate navigation files found.')
} else {
    foreach ($file in $navFiles) {
        $relative = $file.FullName.Substring($sourceRoot.Length).TrimStart('\') -replace '\\','/'
        [void]$report.Add(('- `{0}`' -f $relative))
    }
}
[void]$report.Add('')

[void]$report.Add('## Notes')
[void]$report.Add('- This audit intentionally does not add routes. It identifies reachability gaps for a targeted follow-up set.')
[void]$report.Add('- Import paths with `.tsx` extensions are checked by the test script, not changed by this apply script.')

[System.IO.File]::WriteAllLines($reportPath, $report.ToArray(), [System.Text.UTF8Encoding]::new($false))
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2BO Admin Web navigation reachability audit applied.'
