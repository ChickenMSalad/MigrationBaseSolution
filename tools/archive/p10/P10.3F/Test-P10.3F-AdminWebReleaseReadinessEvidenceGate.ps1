Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..')).Path

$requiredFiles = @(
    'tools\p10\P10.3F\Apply-P10.3F-AdminWebReleaseReadinessEvidenceGate.ps1',
    'tools\p10\P10.3F\Test-P10.3F-AdminWebReleaseReadinessEvidenceGate.ps1',
    'tools\p10\P10.3F\Run-P10.3F-AdminWebReleaseReadinessEvidenceGate.ps1',
    'docs\P10\P10.3F-AdminWebReleaseReadinessEvidenceGate.md'
)

foreach ($relativePath in $requiredFiles) {
    $path = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw ('Required file is missing: {0}' -f $path)
    }
}

$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
if (-not (Test-Path -LiteralPath $adminWebRoot)) {
    throw ('Admin Web root is missing: {0}' -f $adminWebRoot)
}

$packageJson = Join-Path $adminWebRoot 'package.json'
if (-not (Test-Path -LiteralPath $packageJson)) {
    throw ('Admin Web package.json is missing: {0}' -f $packageJson)
}

$runnerPath = Join-Path $repoRoot 'tools\p10\P10.3F\Run-P10.3F-AdminWebReleaseReadinessEvidenceGate.ps1'
$runnerContent = Get-Content -LiteralPath $runnerPath -Raw
if ($runnerContent -notlike '*param(*') {
    throw 'Release readiness runner does not contain a param block.'
}
if ($runnerContent -like '*return @(*') {
    throw 'Release readiness runner contains forbidden return array wrapping.'
}
if ($runnerContent -like '*+=*') {
    throw 'Release readiness runner contains forbidden array append operator.'
}

Write-Host 'P10.3F Admin Web release readiness evidence gate validation passed.'
