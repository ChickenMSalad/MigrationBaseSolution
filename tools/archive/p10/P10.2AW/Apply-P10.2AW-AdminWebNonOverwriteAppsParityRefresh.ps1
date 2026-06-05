param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    $current = $PSScriptRoot
    while ($null -ne $current -and $current.Length -gt 0) {
        $candidate = [System.IO.Path]::Combine($current, 'src', 'Admin', 'Migration.Admin.Web')
        if (Test-Path -Path $candidate -PathType Container) {
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

function Add-Line {
    param(
        [Parameter(Mandatory=$true)] [System.Collections.ArrayList] $Lines,
        [Parameter(Mandatory=$true)] [AllowEmptyString()] [string] $Line
    )

    [void]$Lines.Add($Line)
}

function Get-RelativePathFromRoot {
    param(
        [Parameter(Mandatory=$true)] [string] $RootPath,
        [Parameter(Mandatory=$true)] [string] $FullPath
    )

    $rootFull = [System.IO.Path]::GetFullPath($RootPath).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    $fileFull = [System.IO.Path]::GetFullPath($FullPath)
    if (-not $fileFull.StartsWith($rootFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $fileFull
    }

    $relative = $fileFull.Substring($rootFull.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    return ($relative -replace [regex]::Escape([System.IO.Path]::DirectorySeparatorChar), '/')
}

$repoRoot = Get-RepositoryRoot
$adminSourceRoot = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src')
$appsSourceRoot = [System.IO.Path]::Combine($repoRoot, 'apps', 'migration-admin-ui', 'src')
$docsRoot = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10')
$reportPath = [System.IO.Path]::Combine($docsRoot, 'P10.2AW-AdminWebNonOverwriteAppsParityRefresh-Report.md')

if (-not (Test-Path -Path $adminSourceRoot -PathType Container)) {
    throw ('Canonical Admin Web source root was not found: {0}' -f $adminSourceRoot)
}

if (-not (Test-Path -Path $appsSourceRoot -PathType Container)) {
    throw ('Reference apps source root was not found: {0}' -f $appsSourceRoot)
}

if (-not (Test-Path -Path $docsRoot -PathType Container)) {
    New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null
}

$report = New-Object System.Collections.ArrayList
Add-Line -Lines $report -Line '# P10.2AW - Admin Web Non-Overwrite Apps Parity Refresh Report'
Add-Line -Lines $report -Line ''
Add-Line -Lines $report -Line ('Repository root: `{0}`' -f $repoRoot)
Add-Line -Lines $report -Line ('Canonical Admin Web source: `{0}`' -f $adminSourceRoot)
Add-Line -Lines $report -Line ('Reference apps source: `{0}`' -f $appsSourceRoot)
Add-Line -Lines $report -Line ''

$sourceFamilies = @('features', 'components', 'auth', 'lib')
$copied = New-Object System.Collections.ArrayList
$skippedExisting = New-Object System.Collections.ArrayList
$missingFamilies = New-Object System.Collections.ArrayList

foreach ($family in $sourceFamilies) {
    $appsFamilyRoot = [System.IO.Path]::Combine($appsSourceRoot, $family)
    $adminFamilyRoot = [System.IO.Path]::Combine($adminSourceRoot, $family)

    if (-not (Test-Path -Path $appsFamilyRoot -PathType Container)) {
        [void]$missingFamilies.Add($family)
        continue
    }

    $files = @(Get-ChildItem -Path $appsFamilyRoot -Recurse -File)
    foreach ($file in $files) {
        $relative = Get-RelativePathFromRoot -RootPath $appsSourceRoot -FullPath $file.FullName
        $targetPath = [System.IO.Path]::Combine($adminSourceRoot, ($relative -replace '/', [System.IO.Path]::DirectorySeparatorChar))
        $targetFolder = [System.IO.Path]::GetDirectoryName($targetPath)

        if (Test-Path -Path $targetPath -PathType Leaf) {
            [void]$skippedExisting.Add($relative)
            continue
        }

        if (-not (Test-Path -Path $targetFolder -PathType Container)) {
            New-Item -ItemType Directory -Path $targetFolder -Force | Out-Null
        }

        Copy-Item -Path $file.FullName -Destination $targetPath -Force:$false
        [void]$copied.Add($relative)
        Write-Host ('Copied missing apps source file: {0}' -f $relative)
    }
}

Add-Line -Lines $report -Line '## Missing apps source copied into canonical Admin Web'
Add-Line -Lines $report -Line ''
if ($copied.Count -eq 0) {
    Add-Line -Lines $report -Line '- None. Canonical Admin Web already contained all checked apps source files.'
} else {
    foreach ($item in $copied) {
        Add-Line -Lines $report -Line ('- `{0}`' -f $item)
    }
}
Add-Line -Lines $report -Line ''

Add-Line -Lines $report -Line '## Apps source files already present in canonical Admin Web'
Add-Line -Lines $report -Line ''
if ($skippedExisting.Count -eq 0) {
    Add-Line -Lines $report -Line '- None.'
} else {
    foreach ($item in $skippedExisting) {
        Add-Line -Lines $report -Line ('- `{0}`' -f $item)
    }
}
Add-Line -Lines $report -Line ''

Add-Line -Lines $report -Line '## Missing checked source families under apps'
Add-Line -Lines $report -Line ''
if ($missingFamilies.Count -eq 0) {
    Add-Line -Lines $report -Line '- None.'
} else {
    foreach ($item in $missingFamilies) {
        Add-Line -Lines $report -Line ('- `{0}`' -f $item)
    }
}
Add-Line -Lines $report -Line ''

Add-Line -Lines $report -Line '## Remaining canonical flat folders'
Add-Line -Lines $report -Line ''
$flatFolders = @('pages', 'api', 'types')
foreach ($folderName in $flatFolders) {
    $folderPath = [System.IO.Path]::Combine($adminSourceRoot, $folderName)
    Add-Line -Lines $report -Line ('### `{0}`' -f $folderName)
    if (-not (Test-Path -Path $folderPath -PathType Container)) {
        Add-Line -Lines $report -Line '- Folder not present.'
        Add-Line -Lines $report -Line ''
        continue
    }

    $flatFiles = @(Get-ChildItem -Path $folderPath -Recurse -File)
    if ($flatFiles.Count -eq 0) {
        Add-Line -Lines $report -Line '- No files remain.'
    } else {
        foreach ($flatFile in $flatFiles) {
            $relativeFlat = Get-RelativePathFromRoot -RootPath $adminSourceRoot -FullPath $flatFile.FullName
            Add-Line -Lines $report -Line ('- `{0}`' -f $relativeFlat)
        }
    }
    Add-Line -Lines $report -Line ''
}

$report | Set-Content -Path $reportPath -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2AW Admin Web non-overwrite apps parity refresh applied.'
