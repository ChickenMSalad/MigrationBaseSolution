Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..')).Path
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$docsRoot = Join-Path $repoRoot 'docs\P10'
$reportPath = Join-Path $docsRoot 'P10.3K-AdminWebCloudPublishReadiness.md'
$runScript = Join-Path $scriptRoot 'Run-P10.3K-AdminWebCloudPublishPackage.ps1'

$scriptFiles = Get-ChildItem -LiteralPath $scriptRoot -Filter '*.ps1' -File
foreach ($scriptFile in $scriptFiles) {
    $content = Get-Content -LiteralPath $scriptFile.FullName -Raw
    [void][scriptblock]::Create($content)
}

if (-not (Test-Path -LiteralPath $adminWebRoot -PathType Container)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}
if (-not (Test-Path -LiteralPath (Join-Path $adminWebRoot 'package.json') -PathType Leaf)) {
    throw 'Admin Web package.json is missing.'
}
if (-not (Test-Path -LiteralPath $reportPath -PathType Leaf)) {
    throw ('Expected report was not found: {0}' -f $reportPath)
}
if (-not (Test-Path -LiteralPath $runScript -PathType Leaf)) {
    throw ('Expected runner was not found: {0}' -f $runScript)
}

$reportContent = Get-Content -LiteralPath $reportPath -Raw
if ($reportContent.IndexOf('Admin Web Cloud Publish Readiness') -lt 0) {
    throw 'Report does not contain the expected title.'
}
if ($reportContent.IndexOf('Run-P10.3K-AdminWebCloudPublishPackage.ps1') -lt 0) {
    throw 'Report does not reference the publish package runner.'
}

Write-Host 'P10.3K Admin Web cloud publish readiness validation passed.'
