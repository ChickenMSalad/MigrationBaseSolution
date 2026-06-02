Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$artifactsDir = Join-Path $repoRoot 'artifacts\p10\P10.2CF'
$stdoutLog = Join-Path $artifactsDir 'npm-build.stdout.log'
$stderrLog = Join-Path $artifactsDir 'npm-build.stderr.log'
$summaryPath = Join-Path $artifactsDir 'npm-build.summary.md'

if (-not (Test-Path -Path $adminWebRoot -PathType Container)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}

New-Item -ItemType Directory -Force -Path $artifactsDir | Out-Null

$npm = Get-Command npm.cmd -ErrorAction SilentlyContinue
if ($null -eq $npm) {
    $npm = Get-Command npm -ErrorAction SilentlyContinue
}
if ($null -eq $npm) {
    throw 'npm was not found on PATH.'
}

Push-Location $adminWebRoot
try {
    & $npm.Source run build 1> $stdoutLog 2> $stderrLog
    $exitCode = $LASTEXITCODE
} finally {
    Pop-Location
}

$summary = New-Object 'System.Collections.Generic.List[string]'
[void]$summary.Add('# P10.2CF - Admin Web Production Build')
[void]$summary.Add('')
[void]$summary.Add(('Admin Web root: `{0}`' -f $adminWebRoot))
[void]$summary.Add(('npm command: `{0}`' -f $npm.Source))
[void]$summary.Add(('stdout log: `{0}`' -f $stdoutLog))
[void]$summary.Add(('stderr log: `{0}`' -f $stderrLog))
[void]$summary.Add(('Exit code: `{0}`' -f $exitCode))
[void]$summary.Add('')

$distIndex = Join-Path $adminWebRoot 'dist\index.html'
$distAssets = Join-Path $adminWebRoot 'dist\assets'
if ($exitCode -eq 0 -and (Test-Path -Path $distIndex -PathType Leaf) -and (Test-Path -Path $distAssets -PathType Container)) {
    [void]$summary.Add('Result: production build succeeded and dist output exists.')
} elseif ($exitCode -eq 0) {
    [void]$summary.Add('Result: npm build exited successfully but expected dist output was not found.')
} else {
    [void]$summary.Add('Result: npm build failed. Review stdout/stderr logs.')
}

$summary | Set-Content -Path $summaryPath -Encoding UTF8
Write-Host ('Wrote build summary: {0}' -f $summaryPath)

if ($exitCode -ne 0) {
    throw ('npm run build failed with exit code {0}. Review {1} and {2}.' -f $exitCode, $stdoutLog, $stderrLog)
}
if (-not (Test-Path -Path $distIndex -PathType Leaf)) {
    throw ('Build completed but dist index was not found: {0}' -f $distIndex)
}
if (-not (Test-Path -Path $distAssets -PathType Container)) {
    throw ('Build completed but dist assets folder was not found: {0}' -f $distAssets)
}

Write-Host 'P10.2CF Admin Web production build verification passed.'
