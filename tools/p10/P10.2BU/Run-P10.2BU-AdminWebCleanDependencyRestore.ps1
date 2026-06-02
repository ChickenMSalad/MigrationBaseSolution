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

$typescriptDir = Join-Path $adminRoot 'node_modules\typescript'
$binDir = Join-Path $adminRoot 'node_modules\.bin'
$tscCmd = Join-Path $binDir 'tsc.cmd'
$tscPs1 = Join-Path $binDir 'tsc.ps1'
$tscShim = Join-Path $binDir 'tsc'

$removed = New-Object 'System.Collections.Generic.List[string]'
foreach ($path in @($typescriptDir, $tscCmd, $tscPs1, $tscShim)) {
    if (Test-Path -Path $path) {
        Remove-Item -Path $path -Recurse -Force
        [void]$removed.Add($path)
    }
}

$stdoutLog = Join-Path $artifactDir 'npm-restore.stdout.log'
$stderrLog = Join-Path $artifactDir 'npm-restore.stderr.log'
$summaryPath = Join-Path $artifactDir 'npm-restore.summary.md'

Push-Location $adminRoot
try {
    $npmArgs = @('install', '--include=dev')
    $process = Start-Process -FilePath $npmCommand.Source -ArgumentList $npmArgs -WorkingDirectory $adminRoot -NoNewWindow -Wait -PassThru -RedirectStandardOutput $stdoutLog -RedirectStandardError $stderrLog
    $exitCode = [int]$process.ExitCode
}
finally {
    Pop-Location
}

$tscEntry = Join-Path $adminRoot 'node_modules\typescript\bin\tsc'
$tscEntryCmd = Join-Path $adminRoot 'node_modules\typescript\bin\tsc.cmd'
$hasTsc = (Test-Path -Path $tscEntry -PathType Leaf) -or (Test-Path -Path $tscEntryCmd -PathType Leaf)

$summary = New-Object 'System.Collections.Generic.List[string]'
[void]$summary.Add('# P10.2BU - Admin Web npm clean dependency restore')
[void]$summary.Add('')
[void]$summary.Add(('Admin Web root: `{0}`' -f $adminRoot))
[void]$summary.Add(('npm command: `{0}`' -f $npmCommand.Source))
[void]$summary.Add(('Exit code: `{0}`' -f $exitCode))
[void]$summary.Add(('TypeScript compiler entry exists: `{0}`' -f $hasTsc))
[void]$summary.Add('')
[void]$summary.Add('## Removed stale artifacts')
if ($removed.Count -eq 0) {
    [void]$summary.Add('- none')
} else {
    foreach ($item in $removed) {
        [void]$summary.Add(('- `{0}`' -f $item))
    }
}
[void]$summary.Add('')
[void]$summary.Add(('stdout log: `{0}`' -f $stdoutLog))
[void]$summary.Add(('stderr log: `{0}`' -f $stderrLog))
Set-Content -Path $summaryPath -Value $summary -Encoding UTF8

Write-Host ('Wrote restore summary: {0}' -f $summaryPath)
if ($exitCode -ne 0) {
    throw ('npm install --include=dev failed with exit code {0}. Review {1} and {2}.' -f $exitCode, $stdoutLog, $stderrLog)
}
if (-not $hasTsc) {
    throw ('TypeScript compiler entry is still missing after restore: {0}' -f $tscEntry)
}
Write-Host 'P10.2BU Admin Web clean dependency restore completed.'
