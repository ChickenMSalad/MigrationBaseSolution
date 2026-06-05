Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..')).Path
$adminRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$srcRoot = Join-Path $adminRoot 'src'
$appPath = Join-Path $srcRoot 'App.tsx'
$docPath = Join-Path $repoRoot 'docs\P10\P10.2CQ-AdminWebCanonicalBuilderRouteRestoration.Report.md'

if (-not (Test-Path -LiteralPath $appPath)) { throw ('App.tsx missing: {0}' -f $appPath) }
if (-not (Test-Path -LiteralPath $docPath)) { throw ('CQ report missing: {0}' -f $docPath) }

$app = Get-Content -LiteralPath $appPath -Raw
$report = Get-Content -LiteralPath $docPath -Raw

if (-not $report.Contains('Manifest Builder')) { throw 'CQ report does not mention Manifest Builder.' }
if (-not $report.Contains('Taxonomy Builder')) { throw 'CQ report does not mention Taxonomy Builder.' }
if (-not $report.Contains('Mapping Builder')) { throw 'CQ report does not mention Mapping Builder.' }

if ($app.Contains('.tsx"') -or $app.Contains(".tsx'")) { throw 'App.tsx contains an extension-bearing TSX import.' }
if ($app.Contains('reference/apps-migration-admin-ui')) { throw 'App.tsx references the Admin Web reference tree.' }
if ($app.Contains('apps/migration-admin-ui')) { throw 'App.tsx references the legacy apps tree.' }

$routes = @('/manifest-builder','/taxonomy-builder','/mapping-builder')
foreach ($route in $routes) {
    $quotedDouble = ('path="{0}"' -f $route)
    $quotedSingle = ("path='{0}'" -f $route)
    $count = 0
    if ($app.Contains($quotedDouble)) { $count++ }
    if ($app.Contains($quotedSingle)) { $count++ }
    if ($count -gt 1) { throw ('Duplicate route path found: {0}' -f $route) }
}

Write-Host 'P10.2CQ Admin Web canonical builder route restoration validation passed.'
