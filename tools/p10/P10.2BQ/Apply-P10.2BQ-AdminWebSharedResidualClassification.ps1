Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..')).Path
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$sourceRoot = Join-Path $adminWebRoot 'src'
$apiRoot = Join-Path $sourceRoot 'api'
$typesRoot = Join-Path $sourceRoot 'types'
$featuresRoot = Join-Path $sourceRoot 'features'
$docsRoot = Join-Path $repoRoot 'docs\P10'
$reportPath = Join-Path $docsRoot 'P10.2BQ-AdminWebSharedResidualClassification.Report.md'

if (-not (Test-Path -Path $sourceRoot -PathType Container)) {
    throw ('Admin Web source root was not found: {0}' -f $sourceRoot)
}

if (-not (Test-Path -Path $docsRoot -PathType Container)) {
    New-Item -Path $docsRoot -ItemType Directory -Force | Out-Null
}

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add('# P10.2BQ - Admin Web Shared Residual Classification')
[void]$report.Add('')
[void]$report.Add(('Generated: {0:u}' -f (Get-Date)))
[void]$report.Add('')
[void]$report.Add('This report classifies remaining flat canonical Admin Web `src/api` and `src/types` assets after feature consolidation. It is report-only.')
[void]$report.Add('')

$sourceFiles = @()
if (Test-Path -Path $sourceRoot -PathType Container) {
    $sourceFiles = @(Get-ChildItem -Path $sourceRoot -Recurse -File -Include '*.ts','*.tsx' | Where-Object {
        $full = $_.FullName
        ($full -notlike '*\reference\*') -and
        ($full -notlike '*\node_modules\*') -and
        ($full -notlike '*\dist\*')
    })
}

$featureFiles = @()
if (Test-Path -Path $featuresRoot -PathType Container) {
    $featureFiles = @(Get-ChildItem -Path $featuresRoot -Recurse -File -Include '*.ts','*.tsx')
}

$featureApiBaseNames = New-Object 'System.Collections.Generic.HashSet[string]'
$featureTypeBaseNames = New-Object 'System.Collections.Generic.HashSet[string]'
foreach ($file in $featureFiles) {
    $path = $file.FullName
    if ($path -like '*\api\*') {
        [void]$featureApiBaseNames.Add($file.BaseName)
    }
    if ($path -like '*\types\*') {
        [void]$featureTypeBaseNames.Add($file.BaseName)
    }
}

$flatApiFiles = @()
if (Test-Path -Path $apiRoot -PathType Container) {
    $flatApiFiles = @(Get-ChildItem -Path $apiRoot -Recurse -File -Include '*.ts')
}

$flatTypeFiles = @()
if (Test-Path -Path $typesRoot -PathType Container) {
    $flatTypeFiles = @(Get-ChildItem -Path $typesRoot -Recurse -File -Include '*.ts')
}

function Get-RelativePathText {
    param(
        [Parameter(Mandatory=$true)][string]$BasePath,
        [Parameter(Mandatory=$true)][string]$TargetPath
    )

    $baseUri = New-Object System.Uri(($BasePath.TrimEnd('\') + '\'))
    $targetUri = New-Object System.Uri($TargetPath)
    $relativeUri = $baseUri.MakeRelativeUri($targetUri)
    return [System.Uri]::UnescapeDataString($relativeUri.ToString()).Replace('/', '\')
}

function Get-ReferenceCount {
    param(
        [Parameter(Mandatory=$true)][string]$BaseName,
        [Parameter(Mandatory=$true)]$Files
    )

    $count = 0
    foreach ($file in @($Files)) {
        if (-not (Test-Path -Path $file.FullName -PathType Leaf)) { continue }
        $content = Get-Content -Path $file.FullName -Raw
        if ($null -eq $content) { $content = '' }
        if (($content -like ('*api/' + $BaseName + '*')) -or
            ($content -like ('*api\' + $BaseName + '*')) -or
            ($content -like ('*types/' + $BaseName + '*')) -or
            ($content -like ('*types\' + $BaseName + '*')) -or
            ($content -like ('*' + $BaseName + '*'))) {
            $count = $count + 1
        }
    }
    return $count
}

[void]$report.Add('## Summary')
[void]$report.Add('')
[void]$report.Add(('- Flat API files: {0}' -f @($flatApiFiles).Length))
[void]$report.Add(('- Flat type files: {0}' -f @($flatTypeFiles).Length))
[void]$report.Add(('- Feature API basenames: {0}' -f $featureApiBaseNames.Count))
[void]$report.Add(('- Feature type basenames: {0}' -f $featureTypeBaseNames.Count))
[void]$report.Add('')

[void]$report.Add('## Flat API Classification')
[void]$report.Add('')
if (@($flatApiFiles).Length -eq 0) {
    [void]$report.Add('- No flat API files remain under `src/api`.')
} else {
    foreach ($file in @($flatApiFiles | Sort-Object FullName)) {
        $relative = Get-RelativePathText -BasePath $repoRoot -TargetPath $file.FullName
        $classification = 'shared-or-unclassified'
        if ($file.FullName -like '*\api\core\*') {
            $classification = 'shared-core'
        } elseif ($featureApiBaseNames.Contains($file.BaseName)) {
            $classification = 'duplicate-candidate-feature-api'
        } elseif ($file.BaseName -like '*Client*' -or $file.BaseName -like '*client*') {
            $classification = 'shared-client-candidate'
        }
        $refCount = Get-ReferenceCount -BaseName $file.BaseName -Files $sourceFiles
        [void]$report.Add(('- `{0}` — {1}; local reference hits: {2}' -f $relative, $classification, $refCount))
    }
}
[void]$report.Add('')

[void]$report.Add('## Flat Type Classification')
[void]$report.Add('')
if (@($flatTypeFiles).Length -eq 0) {
    [void]$report.Add('- No flat type files remain under `src/types`.')
} else {
    foreach ($file in @($flatTypeFiles | Sort-Object FullName)) {
        $relative = Get-RelativePathText -BasePath $repoRoot -TargetPath $file.FullName
        $classification = 'shared-or-unclassified'
        if ($featureTypeBaseNames.Contains($file.BaseName)) {
            $classification = 'duplicate-candidate-feature-types'
        } elseif ($file.BaseName -like '*common*' -or $file.BaseName -like '*shared*') {
            $classification = 'shared-type-candidate'
        }
        $refCount = Get-ReferenceCount -BaseName $file.BaseName -Files $sourceFiles
        [void]$report.Add(('- `{0}` — {1}; local reference hits: {2}' -f $relative, $classification, $refCount))
    }
}
[void]$report.Add('')

[void]$report.Add('## Next Cleanup Guidance')
[void]$report.Add('')
[void]$report.Add('- Keep `src/api/core` assets as shared infrastructure unless the Admin Web build proves they are unused.')
[void]$report.Add('- Do not delete duplicate candidates until the local TypeScript build and route coverage reports confirm no compiled source imports them.')
[void]$report.Add('- Prefer a later targeted cleanup package that removes only zero-reference duplicate candidates.')
[void]$report.Add('')

Set-Content -Path $reportPath -Value $report -Encoding UTF8
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2BQ Admin Web shared residual classification applied.'
