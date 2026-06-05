Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$srcRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web\src'
$clientPath = Join-Path $srcRoot 'api\core\adminApiClient.ts'
$coreClientPath = Join-Path $srcRoot 'api\core\client.ts'
$cardPath = Join-Path $srcRoot 'components\Card.tsx'
$loadingPath = Join-Path $srcRoot 'components\LoadingError.tsx'
$reportPath = Join-Path $repoRoot 'docs\P10\P10.2BX-AdminWebSharedCompatibilityRepair.Report.md'

$requiredFiles = @(
    $clientPath,
    $coreClientPath,
    $cardPath,
    $loadingPath,
    $reportPath
)
foreach ($file in $requiredFiles) {
    if (-not (Test-Path -Path $file -PathType Leaf)) { throw ('Expected file missing: {0}' -f $file) }
}

$clientText = Get-Content -Path $clientPath -Raw
if ($clientText -notmatch 'export\s+const\s+adminApiClient') { throw 'adminApiClient export missing.' }
if ($clientText -notmatch 'apiPost<TResponse\s*=\s*unknown,\s*TRequest\s*=\s*unknown>') { throw 'apiPost compatibility generic defaults missing.' }
if ($clientText -notmatch 'apiDelete<TResponse\s*=\s*void>') { throw 'apiDelete generic compatibility missing.' }

$coreClientText = Get-Content -Path $coreClientPath -Raw
if ($coreClientText -notmatch 'adminApiClient') { throw 'core/client compatibility re-export missing adminApiClient.' }

$cardText = Get-Content -Path $cardPath -Raw
if ($cardText -notmatch 'description\?:\s*string') { throw 'Card description prop compatibility missing.' }

$loadingText = Get-Content -Path $loadingPath -Raw
if ($loadingText -notmatch 'title\?:\s*string') { throw 'LoadingError title prop compatibility missing.' }

$operationalEventsPage = Join-Path $srcRoot 'features\operations\operationalEvents\pages\OperationalEvents.tsx'
if (Test-Path -Path $operationalEventsPage -PathType Leaf) {
    $pageText = Get-Content -Path $operationalEventsPage -Raw
    if ($pageText.Contains('../components/Card')) { throw 'OperationalEvents still imports Card from ../components.' }
    if ($pageText.Contains('../components/LoadingError')) { throw 'OperationalEvents still imports LoadingError from ../components.' }
    if ($pageText -match '\.tsx[''\"]') { throw 'OperationalEvents has extension-bearing TSX import.' }
}

Write-Host 'P10.2BX validation passed.'
