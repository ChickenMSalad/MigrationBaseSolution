Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $scriptRoot '..\..\..'))
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$sourceRoot = Join-Path $adminWebRoot 'src'
$appPath = Join-Path $sourceRoot 'App.tsx'
$appsSourceRoot = Join-Path $repoRoot 'apps\migration-admin-ui\src'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2BH-AdminWebLocalBuildReadinessVerification.Report.md'
$reportDirectory = Split-Path -Parent $reportPath
if (-not (Test-Path -Path $reportDirectory -PathType Container)) {
    New-Item -Path $reportDirectory -ItemType Directory -Force | Out-Null
}

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2BH - Admin Web Local Build Readiness Verification Report')
[void]$report.Add('')
[void]$report.Add(('Generated: {0:yyyy-MM-dd HH:mm:ss}' -f (Get-Date)))
[void]$report.Add('')
[void]$report.Add('## Canonical paths')
[void]$report.Add('')
[void]$report.Add(('- Repo root: `{0}`' -f $repoRoot))
[void]$report.Add(('- Admin Web root exists: `{0}`' -f (Test-Path -Path $adminWebRoot -PathType Container)))
[void]$report.Add(('- Admin Web src exists: `{0}`' -f (Test-Path -Path $sourceRoot -PathType Container)))
[void]$report.Add(('- App.tsx exists: `{0}`' -f (Test-Path -Path $appPath -PathType Leaf)))
[void]$report.Add(('- Apps reference src exists: `{0}`' -f (Test-Path -Path $appsSourceRoot -PathType Container)))
[void]$report.Add('')

if (-not (Test-Path -Path $sourceRoot -PathType Container)) {
    throw ('Canonical Admin Web source root was not found: {0}' -f $sourceRoot)
}
if (-not (Test-Path -Path $appPath -PathType Leaf)) {
    throw ('Canonical App.tsx was not found: {0}' -f $appPath)
}

$appContent = Get-Content -Path $appPath -Raw
$appLines = @(Get-Content -Path $appPath)
[void]$report.Add('## App.tsx local posture')
[void]$report.Add('')
[void]$report.Add(('- Line count as read locally: `{0}`' -f $appLines.Length))
[void]$report.Add(('- Character count: `{0}`' -f $appContent.Length))
[void]$report.Add(('- Contains `<Routes`: `{0}`' -f $appContent.Contains('<Routes')))
[void]$report.Add(('- Contains `<Route`: `{0}`' -f $appContent.Contains('<Route')))
[void]$report.Add(('- Contains `export default function App`: `{0}`' -f $appContent.Contains('export default function App')))
[void]$report.Add('')

$importMatches = @([System.Text.RegularExpressions.Regex]::Matches($appContent, 'from\s+[''\"]([^''\"]+)[''\"]'))
$importSources = New-Object 'System.Collections.Generic.List[string]'
foreach ($match in $importMatches) {
    if ($null -ne $match -and $match.Groups.Count -gt 1) {
        [void]$importSources.Add($match.Groups[1].Value)
    }
}

[void]$report.Add('## App.tsx relative import target check')
[void]$report.Add('')
if ($importSources.Count -eq 0) {
    [void]$report.Add('- No import sources were detected.')
} else {
    foreach ($source in $importSources) {
        if (-not $source.StartsWith('.')) {
            [void]$report.Add(('- External import: `{0}`' -f $source))
            continue
        }

        $basePath = [System.IO.Path]::GetFullPath((Join-Path $sourceRoot ($source -replace '/', [System.IO.Path]::DirectorySeparatorChar)))
        $candidates = New-Object 'System.Collections.Generic.List[string]'
        [void]$candidates.Add($basePath)
        [void]$candidates.Add(($basePath + '.ts'))
        [void]$candidates.Add(($basePath + '.tsx'))
        [void]$candidates.Add(($basePath + '.js'))
        [void]$candidates.Add(($basePath + '.jsx'))
        [void]$candidates.Add((Join-Path $basePath 'index.ts'))
        [void]$candidates.Add((Join-Path $basePath 'index.tsx'))
        [void]$candidates.Add((Join-Path $basePath 'index.js'))
        [void]$candidates.Add((Join-Path $basePath 'index.jsx'))

        $found = $false
        foreach ($candidate in $candidates) {
            if ((Test-Path -Path $candidate -PathType Leaf) -or (Test-Path -Path $candidate -PathType Container)) {
                $found = $true
                break
            }
        }

        [void]$report.Add(('- `{0}` -> target exists: `{1}`' -f $source, $found))
    }
}
[void]$report.Add('')

$routeMatches = @([System.Text.RegularExpressions.Regex]::Matches($appContent, 'path=\{?[''\"]([^''\"]+)[''\"]\}?'))
$routePaths = New-Object 'System.Collections.Generic.List[string]'
foreach ($match in $routeMatches) {
    if ($null -ne $match -and $match.Groups.Count -gt 1) {
        [void]$routePaths.Add($match.Groups[1].Value)
    }
}
[void]$report.Add('## App.tsx route paths')
[void]$report.Add('')
if ($routePaths.Count -eq 0) {
    [void]$report.Add('- No route paths were detected by the local scanner.')
} else {
    foreach ($routePath in $routePaths) {
        [void]$report.Add(('- `{0}`' -f $routePath))
    }
}
[void]$report.Add('')

[void]$report.Add('## Remaining canonical flat folders')
[void]$report.Add('')
$flatFolderNames = @('pages', 'api', 'types')
foreach ($folderName in $flatFolderNames) {
    $folderPath = Join-Path $sourceRoot $folderName
    if (Test-Path -Path $folderPath -PathType Container) {
        $files = @(Get-ChildItem -Path $folderPath -File -Recurse | Sort-Object FullName)
        [void]$report.Add(('- `{0}` exists with `{1}` file(s).' -f $folderName, $files.Length))
        foreach ($file in $files) {
            $relative = $file.FullName.Substring($repoRoot.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar)
            [void]$report.Add(('  - `{0}`' -f $relative))
        }
    } else {
        [void]$report.Add(('- `{0}` does not exist.' -f $folderName))
    }
}
[void]$report.Add('')

[void]$report.Add('## Admin Web package script posture')
[void]$report.Add('')
$packagePath = Join-Path $adminWebRoot 'package.json'
if (Test-Path -Path $packagePath -PathType Leaf) {
    [void]$report.Add(('- package.json exists: `{0}`' -f $packagePath))
    $packageContent = Get-Content -Path $packagePath -Raw
    [void]$report.Add(('- Contains build script token: `{0}`' -f $packageContent.Contains('"build"')))
    [void]$report.Add(('- Contains lint script token: `{0}`' -f $packageContent.Contains('"lint"')))
    [void]$report.Add(('- Contains typecheck token: `{0}`' -f $packageContent.Contains('typecheck')))
} else {
    [void]$report.Add('- package.json was not found under canonical Admin Web root.')
}
[void]$report.Add('')

[void]$report.Add('## Notes')
[void]$report.Add('')
[void]$report.Add('- This package is report-only and intentionally does not run npm, move source files, or rewrite App.tsx.')
[void]$report.Add('- Use this report to choose the next cleanup set from the local committed state, not from raw GitHub formatting artifacts.')

Set-Content -Path $reportPath -Value $report -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2BH Admin Web local build readiness verification applied.'
