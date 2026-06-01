Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    $current = $PSScriptRoot
    while (-not [string]::IsNullOrWhiteSpace($current)) {
        $gitPath = [System.IO.Path]::Combine($current, '.git')
        $srcPath = [System.IO.Path]::Combine($current, 'src')
        $appsPath = [System.IO.Path]::Combine($current, 'apps')
        if ((Test-Path -Path $srcPath -PathType Container) -and (Test-Path -Path $appsPath -PathType Container)) {
            return $current
        }
        $parent = [System.IO.Directory]::GetParent($current)
        if ($null -eq $parent) { break }
        $current = $parent.FullName
    }
    throw 'Unable to locate repository root from script location.'
}

function Get-RelativePathSafe {
    param(
        [Parameter(Mandatory = $true)][string]$BasePath,
        [Parameter(Mandatory = $true)][string]$FullPath
    )

    $baseFull = [System.IO.Path]::GetFullPath($BasePath).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    $targetFull = [System.IO.Path]::GetFullPath($FullPath)
    $baseUri = New-Object System.Uri(($baseFull + [System.IO.Path]::DirectorySeparatorChar))
    $targetUri = New-Object System.Uri($targetFull)
    $relativeUri = $baseUri.MakeRelativeUri($targetUri)
    $relative = [System.Uri]::UnescapeDataString($relativeUri.ToString())
    return ($relative -replace '/', [System.IO.Path]::DirectorySeparatorChar)
}

$repoRoot = Get-RepositoryRoot
$appsSrcRoot = [System.IO.Path]::Combine($repoRoot, 'apps', 'migration-admin-ui', 'src')
$adminSrcRoot = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src')
$reportPath = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10', 'P10.2AU-AdminWebAppsResidualParityReconcile.Report.md')

if (-not (Test-Path -Path $appsSrcRoot -PathType Container)) {
    throw ('Reference Admin UI source root was not found: {0}' -f $appsSrcRoot)
}

if (-not (Test-Path -Path $adminSrcRoot -PathType Container)) {
    throw ('Canonical Admin Web source root was not found: {0}' -f $adminSrcRoot)
}

$reportLines = New-Object System.Collections.ArrayList
$null = $reportLines.Add('# P10.2AU - Admin Web Apps Residual Parity Reconcile Report')
$null = $reportLines.Add('')
$null = $reportLines.Add(('Repository root: `{0}`' -f $repoRoot))
$null = $reportLines.Add(('Reference source: `{0}`' -f $appsSrcRoot))
$null = $reportLines.Add(('Canonical source: `{0}`' -f $adminSrcRoot))
$null = $reportLines.Add('')

$rootsToReconcile = @('features', 'components', 'auth', 'lib')
$copied = New-Object System.Collections.ArrayList
$existing = New-Object System.Collections.ArrayList
$missingRoots = New-Object System.Collections.ArrayList

foreach ($rootName in $rootsToReconcile) {
    $sourceRoot = [System.IO.Path]::Combine($appsSrcRoot, $rootName)
    $targetRoot = [System.IO.Path]::Combine($adminSrcRoot, $rootName)

    if (-not (Test-Path -Path $sourceRoot -PathType Container)) {
        $null = $missingRoots.Add($rootName)
        continue
    }

    if (-not (Test-Path -Path $targetRoot -PathType Container)) {
        New-Item -Path $targetRoot -ItemType Directory -Force | Out-Null
    }

    $sourceFiles = @(Get-ChildItem -Path $sourceRoot -Recurse -File | Where-Object {
        $segments = @($_.FullName -split '[\\/]')
        -not ($segments -contains 'bin') -and -not ($segments -contains 'obj') -and -not ($segments -contains 'node_modules')
    })

    foreach ($sourceFile in $sourceFiles) {
        $relativePath = Get-RelativePathSafe -BasePath $sourceRoot -FullPath $sourceFile.FullName
        $targetPath = [System.IO.Path]::Combine($targetRoot, $relativePath)
        $targetDirectory = [System.IO.Path]::GetDirectoryName($targetPath)
        if ([string]::IsNullOrWhiteSpace($targetDirectory)) {
            throw ('Unable to resolve target directory for {0}' -f $targetPath)
        }

        if (Test-Path -Path $targetPath -PathType Leaf) {
            $null = $existing.Add(('{0}/{1}' -f $rootName, ($relativePath -replace '\\', '/')))
            continue
        }

        if (-not (Test-Path -Path $targetDirectory -PathType Container)) {
            New-Item -Path $targetDirectory -ItemType Directory -Force | Out-Null
        }

        Copy-Item -Path $sourceFile.FullName -Destination $targetPath -Force
        $null = $copied.Add(('{0}/{1}' -f $rootName, ($relativePath -replace '\\', '/')))
        Write-Host ('Copied residual source: {0}' -f $targetPath)
    }
}

$null = $reportLines.Add('## Summary')
$null = $reportLines.Add('')
$copiedItems = @($copied.ToArray())
$existingItems = @($existing.ToArray())
$missingRootItems = @($missingRoots.ToArray())
$null = $reportLines.Add(('- Copied missing files: {0}' -f $copiedItems.Length))
$null = $reportLines.Add(('- Existing canonical files skipped: {0}' -f $existingItems.Length))
$null = $reportLines.Add(('- Missing reference roots: {0}' -f $missingRootItems.Length))
$null = $reportLines.Add('')

$null = $reportLines.Add('## Copied Missing Files')
$null = $reportLines.Add('')
if ($copiedItems.Length -eq 0) {
    $null = $reportLines.Add('- None')
} else {
    foreach ($item in $copiedItems) { $null = $reportLines.Add(('- `{0}`' -f $item)) }
}
$null = $reportLines.Add('')

$null = $reportLines.Add('## Existing Canonical Files Skipped')
$null = $reportLines.Add('')
if ($existingItems.Length -eq 0) {
    $null = $reportLines.Add('- None')
} else {
    foreach ($item in $existingItems) { $null = $reportLines.Add(('- `{0}`' -f $item)) }
}
$null = $reportLines.Add('')

$null = $reportLines.Add('## Missing Reference Roots')
$null = $reportLines.Add('')
if ($missingRootItems.Length -eq 0) {
    $null = $reportLines.Add('- None')
} else {
    foreach ($item in $missingRootItems) { $null = $reportLines.Add(('- `{0}`' -f $item)) }
}

$reportDirectory = [System.IO.Path]::GetDirectoryName($reportPath)
if (-not (Test-Path -Path $reportDirectory -PathType Container)) {
    New-Item -Path $reportDirectory -ItemType Directory -Force | Out-Null
}

[System.IO.File]::WriteAllLines($reportPath, [string[]]$reportLines)
Write-Host ('Wrote reconcile report: {0}' -f $reportPath)
Write-Host 'P10.2AU Admin Web apps residual parity reconcile applied.'
