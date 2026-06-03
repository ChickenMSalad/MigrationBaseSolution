Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$runbookPath = Join-Path $repoRoot 'docs\P10\P10.3G-AdminWebSiteUpRunbook.md'
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$packageJson = Join-Path $adminWebRoot 'package.json'

if (-not (Test-Path -LiteralPath $runbookPath)) {
    throw ('Runbook was not found: {0}' -f $runbookPath)
}
if (-not (Test-Path -LiteralPath $adminWebRoot)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}
if (-not (Test-Path -LiteralPath $packageJson)) {
    throw ('Admin Web package.json was not found: {0}' -f $packageJson)
}

$content = Get-Content -LiteralPath $runbookPath -Raw
if ($content.IndexOf('P10.3G - Admin Web Site-Up Runbook') -lt 0) {
    throw 'Runbook title was not found.'
}
if ($content.IndexOf('http://localhost:5173') -lt 0) {
    throw 'Admin Web local URL was not documented.'
}
if ($content.IndexOf('https://localhost:55436') -lt 0) {
    throw 'Admin API local URL was not documented.'
}
if ($content.IndexOf('Builder parity restoration') -lt 0) {
    throw 'Deferred builder parity note was not documented.'
}

Write-Host 'P10.3G Admin Web site-up runbook validation passed.'
