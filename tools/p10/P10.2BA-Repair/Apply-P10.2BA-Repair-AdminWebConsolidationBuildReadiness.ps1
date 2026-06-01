Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $current = $PSScriptRoot
    while ($true) {
        if ([string]::IsNullOrWhiteSpace($current)) {
            throw 'Unable to locate repository root.'
        }

        $adminWeb = [System.IO.Path]::Combine($current, 'src', 'Admin', 'Migration.Admin.Web')
        $gitDir = [System.IO.Path]::Combine($current, '.git')
        if ((Test-Path -Path $adminWeb -PathType Container) -or (Test-Path -Path $gitDir -PathType Container)) {
            return $current
        }

        $parent = Split-Path -Path $current -Parent
        if ($parent -eq $current) {
            throw 'Unable to locate repository root.'
        }
        $current = $parent
    }
}

function Get-RelativePath {
    param(
        [Parameter(Mandatory = $true)][string]$BasePath,
        [Parameter(Mandatory = $true)][string]$TargetPath
    )

    $baseFull = [System.IO.Path]::GetFullPath($BasePath)
    $targetFull = [System.IO.Path]::GetFullPath($TargetPath)

    if (-not $baseFull.EndsWith([System.IO.Path]::DirectorySeparatorChar)) {
        $baseFull = $baseFull + [System.IO.Path]::DirectorySeparatorChar
    }

    $baseUri = New-Object System.Uri($baseFull)
    $targetUri = New-Object System.Uri($targetFull)
    $relativeUri = $baseUri.MakeRelativeUri($targetUri)
    $relativeText = [System.Uri]::UnescapeDataString($relativeUri.ToString())
    return ($relativeText -replace '/', [System.IO.Path]::DirectorySeparatorChar)
}

$repoRoot = Get-RepoRoot
$adminWebRoot = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web')
$sourceRoot = [System.IO.Path]::Combine($adminWebRoot, 'src')
$docsRoot = [System.IO.Path]::Combine($repoRoot, 'docs', 'P10')
$reportPath = [System.IO.Path]::Combine($docsRoot, 'P10.2BA-Repair-AdminWebConsolidationBuildReadiness.Report.md')

if (-not (Test-Path -Path $adminWebRoot -PathType Container)) {
    throw ('Canonical Admin Web root not found: {0}' -f $adminWebRoot)
}
if (-not (Test-Path -Path $sourceRoot -PathType Container)) {
    throw ('Canonical Admin Web src root not found: {0}' -f $sourceRoot)
}

New-Item -ItemType Directory -Path $docsRoot -Force | Out-Null

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2BA Repair - Admin Web Consolidation Build Readiness Report')
[void]$report.Add('')
[void]$report.Add(('Generated from local repo root: `{0}`' -f $repoRoot))
[void]$report.Add('')
[void]$report.Add('## Canonical Admin Web Paths')
[void]$report.Add('')
[void]$report.Add(('- Admin Web: `{0}`' -f (Get-RelativePath -BasePath $repoRoot -TargetPath $adminWebRoot)))
[void]$report.Add(('- Source root: `{0}`' -f (Get-RelativePath -BasePath $repoRoot -TargetPath $sourceRoot)))
[void]$report.Add('')

$requiredFiles = @(
    [pscustomobject]@{ Label = 'package.json'; Path = [System.IO.Path]::Combine($adminWebRoot, 'package.json') },
    [pscustomobject]@{ Label = 'vite config'; Path = [System.IO.Path]::Combine($adminWebRoot, 'vite.config.ts') },
    [pscustomobject]@{ Label = 'App.tsx'; Path = [System.IO.Path]::Combine($sourceRoot, 'App.tsx') },
    [pscustomobject]@{ Label = 'main.tsx'; Path = [System.IO.Path]::Combine($sourceRoot, 'main.tsx') }
)

[void]$report.Add('## Required Surface')
[void]$report.Add('')
foreach ($required in $requiredFiles) {
    $exists = Test-Path -Path $required.Path -PathType Leaf
    [void]$report.Add(('- {0}: {1}' -f $required.Label, $(if ($exists) { 'present' } else { 'missing' })))
}
[void]$report.Add('')

$topLevelFolders = @('api', 'auth', 'components', 'features', 'lib', 'styles', 'types')
[void]$report.Add('## Top-Level Source Folders')
[void]$report.Add('')
foreach ($folderName in $topLevelFolders) {
    $folderPath = [System.IO.Path]::Combine($sourceRoot, $folderName)
    $exists = Test-Path -Path $folderPath -PathType Container
    [void]$report.Add(('- {0}: {1}' -f $folderName, $(if ($exists) { 'present' } else { 'missing' })))
}
[void]$report.Add('')

[void]$report.Add('## Remaining Flat Files')
[void]$report.Add('')
$flatFolders = @('pages', 'api', 'types')
foreach ($flatFolderName in $flatFolders) {
    $flatFolder = [System.IO.Path]::Combine($sourceRoot, $flatFolderName)
    [void]$report.Add(('### src/{0}' -f $flatFolderName))
    [void]$report.Add('')
    if (Test-Path -Path $flatFolder -PathType Container) {
        $files = @(Get-ChildItem -Path $flatFolder -File -Recurse | Sort-Object FullName)
        if ($files.Length -eq 0) {
            [void]$report.Add('- none')
        } else {
            foreach ($file in $files) {
                [void]$report.Add(('- `{0}`' -f (Get-RelativePath -BasePath $repoRoot -TargetPath $file.FullName)))
            }
        }
    } else {
        [void]$report.Add('- folder not present')
    }
    [void]$report.Add('')
}

[void]$report.Add('## Apps Reference Scan')
[void]$report.Add('')
$scanExtensions = @('.ts', '.tsx', '.js', '.jsx', '.css', '.json')
$appReferenceHits = New-Object 'System.Collections.Generic.List[string]'
$sourceFiles = @(Get-ChildItem -Path $sourceRoot -File -Recurse | Where-Object { $scanExtensions -contains $_.Extension })
foreach ($sourceFile in $sourceFiles) {
    $content = Get-Content -Path $sourceFile.FullName -Raw
    if ($content -like '*apps/migration-admin-ui*' -or $content -like '*apps\migration-admin-ui*') {
        [void]$appReferenceHits.Add((Get-RelativePath -BasePath $repoRoot -TargetPath $sourceFile.FullName))
    }
}
if ($appReferenceHits.Count -eq 0) {
    [void]$report.Add('- no canonical source references to apps/migration-admin-ui found')
} else {
    foreach ($hit in $appReferenceHits) {
        [void]$report.Add(('- `{0}`' -f $hit))
    }
}
[void]$report.Add('')

[void]$report.Add('## Package Scripts')
[void]$report.Add('')
$packagePath = [System.IO.Path]::Combine($adminWebRoot, 'package.json')
if (Test-Path -Path $packagePath -PathType Leaf) {
    $packageText = Get-Content -Path $packagePath -Raw
    $hasBuild = $packageText -like '*"build"*'
    $hasTypecheck = $packageText -like '*"typecheck"*'
    $hasLint = $packageText -like '*"lint"*'
    [void]$report.Add(('- build script visible: {0}' -f $(if ($hasBuild) { 'yes' } else { 'no' })))
    [void]$report.Add(('- typecheck script visible: {0}' -f $(if ($hasTypecheck) { 'yes' } else { 'no' })))
    [void]$report.Add(('- lint script visible: {0}' -f $(if ($hasLint) { 'yes' } else { 'no' })))
} else {
    [void]$report.Add('- package.json missing')
}
[void]$report.Add('')
[void]$report.Add('## Result')
[void]$report.Add('')
[void]$report.Add('P10.2BA Repair completed without source rewrites. Use the report to guide the next consolidation batch or deployment-readiness package.')

Set-Content -Path $reportPath -Value $report -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2BA Repair build-readiness report applied.'
