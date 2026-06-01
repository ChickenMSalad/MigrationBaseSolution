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

function Get-RelativePathSafe {
    param(
        [Parameter(Mandatory=$true)][string]$BasePath,
        [Parameter(Mandatory=$true)][string]$TargetPath
    )

    $baseFull = [System.IO.Path]::GetFullPath($BasePath)
    $targetFull = [System.IO.Path]::GetFullPath($TargetPath)
    if (-not $baseFull.EndsWith([System.IO.Path]::DirectorySeparatorChar)) {
        $baseFull = $baseFull + [System.IO.Path]::DirectorySeparatorChar
    }

    $baseUri = New-Object System.Uri($baseFull)
    $targetUri = New-Object System.Uri($targetFull)
    $relativeUri = $baseUri.MakeRelativeUri($targetUri)
    $relative = [System.Uri]::UnescapeDataString($relativeUri.ToString())
    return $relative.Replace('/', [System.IO.Path]::DirectorySeparatorChar)
}

$repoRoot = Get-RepositoryRoot
$adminSrc = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src')
$appsSrc = [System.IO.Path]::Combine($repoRoot, 'apps', 'migration-admin-ui', 'src')
$docsDir = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10')
$reportPath = [System.IO.Path]::Combine($docsDir, 'P10.2BG-AdminWebConsolidationResidualClassification.md')

if (-not (Test-Path -Path $adminSrc -PathType Container)) {
    throw ('Canonical Admin Web src folder was not found: {0}' -f $adminSrc)
}

if (-not (Test-Path -Path $docsDir -PathType Container)) {
    New-Item -Path $docsDir -ItemType Directory -Force | Out-Null
}

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2BG - Admin Web Consolidation Residual Classification')
[void]$report.Add('')
[void]$report.Add('This report is generated from the local working tree. It does not move or rewrite source files.')
[void]$report.Add('')
[void]$report.Add(('Repository root: `{0}`' -f $repoRoot))
[void]$report.Add(('Canonical Admin Web src: `{0}`' -f $adminSrc))
if (Test-Path -Path $appsSrc -PathType Container) {
    [void]$report.Add(('Reference apps src: `{0}`' -f $appsSrc))
} else {
    [void]$report.Add('Reference apps src: not present in this working tree.')
}
[void]$report.Add('')

$topLevelNames = @('features', 'components', 'auth', 'lib', 'api', 'types', 'styles', 'pages')
[void]$report.Add('## Canonical top-level folder inventory')
[void]$report.Add('')
foreach ($name in $topLevelNames) {
    $path = [System.IO.Path]::Combine($adminSrc, $name)
    if (Test-Path -Path $path -PathType Container) {
        $fileItems = @(Get-ChildItem -Path $path -Recurse -File -ErrorAction Stop)
        [void]$report.Add(('- `{0}`: {1} files' -f $name, $fileItems.Length))
    } else {
        [void]$report.Add(('- `{0}`: missing' -f $name))
    }
}
[void]$report.Add('')

[void]$report.Add('## Remaining flat-folder files')
[void]$report.Add('')
$flatNames = @('pages', 'api', 'types')
foreach ($name in $flatNames) {
    $path = [System.IO.Path]::Combine($adminSrc, $name)
    [void]$report.Add(('### `{0}`' -f $name))
    if (Test-Path -Path $path -PathType Container) {
        $files = @(Get-ChildItem -Path $path -File -ErrorAction Stop | Sort-Object -Property Name)
        if ($files.Length -eq 0) {
            [void]$report.Add('- No direct files remain.')
        } else {
            foreach ($file in $files) {
                $rel = Get-RelativePathSafe -BasePath $repoRoot -TargetPath $file.FullName
                [void]$report.Add(('- `{0}`' -f $rel))
            }
        }
    } else {
        [void]$report.Add('- Folder not present.')
    }
    [void]$report.Add('')
}

[void]$report.Add('## Apps-to-canonical residual source comparison')
[void]$report.Add('')
$copyRoots = @('features', 'components', 'auth', 'lib')
if (Test-Path -Path $appsSrc -PathType Container) {
    foreach ($name in $copyRoots) {
        $appsRoot = [System.IO.Path]::Combine($appsSrc, $name)
        $adminRoot = [System.IO.Path]::Combine($adminSrc, $name)
        [void]$report.Add(('### `{0}`' -f $name))
        if (-not (Test-Path -Path $appsRoot -PathType Container)) {
            [void]$report.Add('- Reference folder not present under apps source.')
            [void]$report.Add('')
            continue
        }
        $appsFiles = @(Get-ChildItem -Path $appsRoot -Recurse -File -ErrorAction Stop | Sort-Object -Property FullName)
        $missing = New-Object 'System.Collections.Generic.List[string]'
        foreach ($file in $appsFiles) {
            $relativeWithinRoot = Get-RelativePathSafe -BasePath $appsRoot -TargetPath $file.FullName
            $target = [System.IO.Path]::Combine($adminRoot, $relativeWithinRoot)
            if (-not (Test-Path -Path $target -PathType Leaf)) {
                [void]$missing.Add($relativeWithinRoot)
            }
        }
        [void]$report.Add(('- Apps files checked: {0}' -f $appsFiles.Length))
        [void]$report.Add(('- Missing in canonical: {0}' -f $missing.Count))
        if ($missing.Count -gt 0) {
            $max = [Math]::Min($missing.Count, 50)
            for ($i = 0; $i -lt $max; $i++) {
                [void]$report.Add(('- `{0}`' -f $missing[$i]))
            }
            if ($missing.Count -gt $max) {
                [void]$report.Add(('- ...and {0} more' -f ($missing.Count - $max)))
            }
        }
        [void]$report.Add('')
    }
} else {
    [void]$report.Add('Reference apps source folder was not present; comparison skipped.')
    [void]$report.Add('')
}

[void]$report.Add('## App.tsx route/import posture')
[void]$report.Add('')
$appTsx = [System.IO.Path]::Combine($adminSrc, 'App.tsx')
if (Test-Path -Path $appTsx -PathType Leaf) {
    $appLines = @(Get-Content -Path $appTsx -ErrorAction Stop)
    $importLines = @($appLines | Where-Object { $_ -match '^\s*import\s+' })
    $routeLines = @($appLines | Where-Object { $_ -match '<Route\s+' })
    [void]$report.Add(('- Import lines: {0}' -f $importLines.Length))
    [void]$report.Add(('- Route lines: {0}' -f $routeLines.Length))
    $joinedApp = [string]::Join([Environment]::NewLine, $appLines)
    if ($joinedApp.Contains('ConnectorConfiguration""')) {
        [void]$report.Add('- Warning: malformed ConnectorConfiguration import text still appears in App.tsx.')
    } else {
        [void]$report.Add('- No malformed ConnectorConfiguration double-quote marker found.')
    }
} else {
    [void]$report.Add('- App.tsx not found.')
}
[void]$report.Add('')

Set-Content -Path $reportPath -Value $report -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2BG Admin Web consolidation residual classification applied.'
