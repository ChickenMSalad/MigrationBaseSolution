Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..')).Path
$sourceRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web\src'
$appPath = Join-Path $sourceRoot 'App.tsx'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2CS-AdminWebBuilderWorkspaceRestoration.md'

if (-not (Test-Path -LiteralPath $appPath -PathType Leaf)) {
    throw ('App.tsx was not found: {0}' -f $appPath)
}

if (-not (Test-Path -LiteralPath $reportPath -PathType Leaf)) {
    throw ('Report was not found: {0}' -f $reportPath)
}

$appText = [System.IO.File]::ReadAllText($appPath)
$reportText = [System.IO.File]::ReadAllText($reportPath)

$specs = @(
    [pscustomobject]@{ Component = 'ManifestBuilder'; Folder = 'manifest'; FileName = 'ManifestBuilder.tsx'; Route = '/builders/manifest' },
    [pscustomobject]@{ Component = 'TaxonomyBuilder'; Folder = 'taxonomy'; FileName = 'TaxonomyBuilder.tsx'; Route = '/builders/taxonomy' },
    [pscustomobject]@{ Component = 'MappingBuilder'; Folder = 'mapping'; FileName = 'MappingBuilder.tsx'; Route = '/builders/mapping' }
)

foreach ($spec in $specs) {
    $pagePath = Join-Path $sourceRoot ('features\builders\{0}\pages\{1}' -f $spec.Folder, $spec.FileName)
    if (-not (Test-Path -LiteralPath $pagePath -PathType Leaf)) {
        throw ('Expected builder page was not found: {0}' -f $pagePath)
    }

    $pageText = [System.IO.File]::ReadAllText($pagePath)
    if (-not $pageText.Contains(('export function {0}' -f $spec.Component))) {
        throw ('Expected component export was not found in {0}.' -f $pagePath)
    }

    if (-not $appText.Contains(('path="{0}"' -f $spec.Route))) {
        throw ('Expected App.tsx route was not found: {0}' -f $spec.Route)
    }

    $importPath = ('./features/builders/{0}/pages/{1}' -f $spec.Folder, ($spec.FileName -replace '\.tsx$', ''))
    if (-not $appText.Contains($importPath)) {
        throw ('Expected App.tsx import path was not found: {0}' -f $importPath)
    }

    if ($appText.Contains(($importPath + '.tsx'))) {
        throw ('App.tsx contains an extension-bearing TSX import path: {0}.tsx' -f $importPath)
    }
}

if (-not $reportText.Contains('## Builder routes')) {
    throw 'Report does not contain the Builder routes section.'
}

if ($appText.Contains('reference/apps-migration-admin-ui')) {
    throw 'App.tsx imports or references the Admin Web reference tree.'
}

Write-Host 'P10.2CS Admin Web builder workspace restoration validation passed.'
