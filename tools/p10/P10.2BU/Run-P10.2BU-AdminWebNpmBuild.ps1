Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..')).Path
$adminRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$artifactDir = Join-Path $repoRoot 'artifacts\p10\P10.2BU'

if (-not (Test-Path -Path $adminRoot -PathType Container)) {
    throw ('Admin Web root was not found: {0}' -f $adminRoot)
}
if (-not (Test-Path -Path $artifactDir -PathType Container)) {
    New-Item -ItemType Directory -Path $artifactDir -Force | Out-Null
}

$npmCommand = Get-Command npm.cmd -ErrorAction SilentlyContinue
if ($null -eq $npmCommand) {
    $npmCommand = Get-Command npm -ErrorAction SilentlyContinue
}
if ($null -eq $npmCommand) {
    throw 'npm was not found on PATH.'
}

$stdoutLog = Join-Path $artifactDir 'npm-build.stdout.log'
$stderrLog = Join-Path $artifactDir 'npm-build.stderr.log'
$summaryPath = Join-Path $artifactDir 'npm-build.summary.md'

Push-Location $adminRoot
try {
    $npmArgs = @('run', 'build')
    $process = Start-Process -FilePath $npmCommand.Source -ArgumentList $npmArgs -WorkingDirectory $adminRoot -NoNewWindow -Wait -PassThru -RedirectStandardOutput $stdoutLog -RedirectStandardError $stderrLog
    $exitCode = [int]$process.ExitCode
}
finally {
    Pop-Location
}

$summary = New-Object 'System.Collections.Generic.List[string]'
[void]$summary.Add('# P10.2BU - Admin Web npm build')
[void]$summary.Add('')
[void]$summary.Add(('Admin Web root: `{0}`' -f $adminRoot))
[void]$summary.Add(('npm command: `{0}`' -f $npmCommand.Source))
[void]$summary.Add(('Exit code: `{0}`' -f $exitCode))
[void]$summary.Add(('stdout log: `{0}`' -f $stdoutLog))
[void]$summary.Add(('stderr log: `{0}`' -f $stderrLog))
Set-Content -Path $summaryPath -Value $summary -Encoding UTF8

Write-Host ('Wrote build summary: {0}' -f $summaryPath)
if ($exitCode -ne 0) {
    throw ('npm run build failed with exit code {0}. Review {1} and {2}.' -f $exitCode, $stdoutLog, $stderrLog)
}
Write-Host 'P10.2BU Admin Web npm build completed.'
