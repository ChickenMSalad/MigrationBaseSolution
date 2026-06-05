Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($scriptRoot, '..', '..', '..'))

$canonicalSrc = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src')
$appsSrc = [System.IO.Path]::Combine($repoRoot, 'apps', 'migration-admin-ui', 'src')
$docsDir = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10')
$reportPath = [System.IO.Path]::Combine($docsDir, 'P10.2AY-AdminWebCanonicalParityStatus.md')

if (-not (Test-Path -Path $canonicalSrc -PathType Container)) {
    throw ('Canonical Admin Web src folder was not found: {0}' -f $canonicalSrc)
}

if (-not (Test-Path -Path $appsSrc -PathType Container)) {
    throw ('Reference apps Admin UI src folder was not found: {0}' -f $appsSrc)
}

if (-not (Test-Path -Path $docsDir -PathType Container)) {
    New-Item -ItemType Directory -Path $docsDir -Force | Out-Null
}

$report = New-Object System.Collections.ArrayList
[void]$report.Add('# P10.2AY - Admin Web Canonical Parity Status')
[void]$report.Add('')
[void]$report.Add('This report was generated from the local working tree.')
[void]$report.Add('')
[void]$report.Add('## Scope')
[void]$report.Add('')
[void]$report.Add('- Canonical Admin Web: `src/Admin/Migration.Admin.Web/src`')
[void]$report.Add('- Reference apps UI: `apps/migration-admin-ui/src`')
[void]$report.Add('- Source mutation: none')
[void]$report.Add('')

$sourceRoots = @('features', 'components', 'auth', 'lib')
$missingFromCanonical = New-Object System.Collections.ArrayList
$presentInCanonical = New-Object System.Collections.ArrayList

foreach ($relativeRoot in $sourceRoots) {
    $appsRoot = [System.IO.Path]::Combine($appsSrc, $relativeRoot)
    $canonicalRoot = [System.IO.Path]::Combine($canonicalSrc, $relativeRoot)

    if (-not (Test-Path -Path $appsRoot -PathType Container)) {
        continue
    }

    $files = @(Get-ChildItem -Path $appsRoot -File -Recurse)
    foreach ($file in $files) {
        $relativeFile = $file.FullName.Substring($appsSrc.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
        $destination = [System.IO.Path]::Combine($canonicalSrc, $relativeFile)
        $displayPath = $relativeFile.Replace([System.IO.Path]::DirectorySeparatorChar, '/')
        if (Test-Path -Path $destination -PathType Leaf) {
            [void]$presentInCanonical.Add($displayPath)
        }
        else {
            [void]$missingFromCanonical.Add($displayPath)
        }
    }
}

[void]$report.Add('## Apps reference files missing from canonical source')
[void]$report.Add('')
if ($missingFromCanonical.Count -eq 0) {
    [void]$report.Add('None detected under the scoped apps source roots.')
}
else {
    foreach ($item in @($missingFromCanonical | Sort-Object)) {
        [void]$report.Add(('- `{0}`' -f $item))
    }
}
[void]$report.Add('')
[void]$report.Add(('Scoped apps files already present in canonical source: {0}' -f $presentInCanonical.Count))
[void]$report.Add('')

[void]$report.Add('## Remaining canonical flat folders')
[void]$report.Add('')
$flatRoots = @('pages', 'api', 'types')
foreach ($flatRoot in $flatRoots) {
    $folder = [System.IO.Path]::Combine($canonicalSrc, $flatRoot)
    [void]$report.Add(('### `{0}`' -f $flatRoot))
    [void]$report.Add('')
    if (-not (Test-Path -Path $folder -PathType Container)) {
        [void]$report.Add('Folder does not exist.')
        [void]$report.Add('')
        continue
    }

    $flatFiles = @(Get-ChildItem -Path $folder -File -Recurse)
    if ($flatFiles.Length -eq 0) {
        [void]$report.Add('No files detected.')
        [void]$report.Add('')
        continue
    }

    foreach ($file in $flatFiles) {
        $relativeFile = $file.FullName.Substring($canonicalSrc.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
        [void]$report.Add(('- `{0}`' -f $relativeFile.Replace([System.IO.Path]::DirectorySeparatorChar, '/')))
    }
    [void]$report.Add('')
}

[void]$report.Add('## Canonical references to apps UI')
[void]$report.Add('')
$appsReferenceHits = New-Object System.Collections.ArrayList
$canonicalFiles = @(Get-ChildItem -Path $canonicalSrc -File -Recurse -Include '*.ts','*.tsx','*.js','*.jsx','*.css')
foreach ($file in $canonicalFiles) {
    $content = Get-Content -Path $file.FullName -Raw
    if ($content -like '*apps/migration-admin-ui*' -or $content -like '*apps\\migration-admin-ui*') {
        $relativeFile = $file.FullName.Substring($canonicalSrc.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
        [void]$appsReferenceHits.Add($relativeFile.Replace([System.IO.Path]::DirectorySeparatorChar, '/'))
    }
}

if ($appsReferenceHits.Count -eq 0) {
    [void]$report.Add('None detected.')
}
else {
    foreach ($item in @($appsReferenceHits | Sort-Object)) {
        [void]$report.Add(('- `{0}`' -f $item))
    }
}
[void]$report.Add('')
[void]$report.Add('## Suggested next action')
[void]$report.Add('')
if ($missingFromCanonical.Count -gt 0) {
    [void]$report.Add('Create a targeted batch copy for the missing apps reference files listed above.')
}
elseif ((Test-Path -Path ([System.IO.Path]::Combine($canonicalSrc, 'pages')) -PathType Container) -or (Test-Path -Path ([System.IO.Path]::Combine($canonicalSrc, 'api')) -PathType Container) -or (Test-Path -Path ([System.IO.Path]::Combine($canonicalSrc, 'types')) -PathType Container)) {
    [void]$report.Add('Review remaining canonical flat files and decide whether they are shared/core assets or should be feature-localized.')
}
else {
    [void]$report.Add('Move toward canonical Admin Web build/deploy validation.')
}

Set-Content -Path $reportPath -Value ([string[]]$report) -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2AY Admin Web canonical parity status applied.'
