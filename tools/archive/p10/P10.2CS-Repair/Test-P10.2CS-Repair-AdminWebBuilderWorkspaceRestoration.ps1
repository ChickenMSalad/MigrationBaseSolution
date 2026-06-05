Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$srcRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web\src'
$appPath = Join-Path $srcRoot 'App.tsx'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2CS-Repair-AdminWebBuilderWorkspaceRestoration.md'

if (-not (Test-Path -LiteralPath $appPath)) { throw ('App.tsx was not found: {0}' -f $appPath) }
if (-not (Test-Path -LiteralPath $reportPath)) { throw ('Expected report was not found: {0}' -f $reportPath) }

$appContent = Get-Content -LiteralPath $appPath -Raw
$expected = @(
    @{ Symbol='ManifestBuilder'; Route='/manifest-builder'; File='features\governance\manifestBuilder\pages\ManifestBuilder.tsx' },
    @{ Symbol='TaxonomyBuilder'; Route='/taxonomy-builder'; File='features\governance\taxonomyBuilder\pages\TaxonomyBuilder.tsx' },
    @{ Symbol='MappingBuilder'; Route='/mapping-builder'; File='features\governance\mappingBuilder\pages\MappingBuilder.tsx' }
)

foreach ($item in $expected) {
    $pagePath = Join-Path $srcRoot $item.File
    if (-not (Test-Path -LiteralPath $pagePath)) { throw ('Builder page missing: {0}' -f $pagePath) }

    $pageContent = Get-Content -LiteralPath $pagePath -Raw
    if ($pageContent.Contains('$' + '{response.status}')) { throw ('Builder page still contains unsafe TypeScript template interpolation token: {0}' -f $pagePath) }
    if ($pageContent -match 'from .*\.tsx') { throw ('Builder page contains extension-bearing TSX import: {0}' -f $pagePath) }
    if ($pageContent.Contains('reference/apps-migration-admin-ui')) { throw ('Builder page imports reference tree material: {0}' -f $pagePath) }

    if ($appContent -notmatch [regex]::Escape($item.Symbol)) { throw ('App.tsx does not reference builder symbol: {0}' -f $item.Symbol) }
    if ($appContent -notmatch [regex]::Escape($item.Route)) { throw ('App.tsx does not reference builder route: {0}' -f $item.Route) }
}

$reportContent = Get-Content -LiteralPath $reportPath -Raw
if ($reportContent -notmatch 'Builder Workspace Restoration') { throw 'Report does not contain the expected title.' }
if ($reportContent -notmatch 'Safety') { throw 'Report does not contain the expected safety section.' }

Write-Host 'P10.2CS Repair Admin Web builder workspace restoration validation passed.'
