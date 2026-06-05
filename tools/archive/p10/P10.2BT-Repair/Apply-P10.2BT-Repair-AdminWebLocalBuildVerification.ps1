Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$toolsDir = Join-Path $repoRoot 'tools\p10\P10.2BT-Repair'
$docsDir = Join-Path $repoRoot 'docs\P10'

if (-not (Test-Path -Path $adminWebRoot -PathType Container)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}
if (-not (Test-Path -Path (Join-Path $adminWebRoot 'package.json') -PathType Leaf)) {
    throw ('Admin Web package.json was not found: {0}' -f (Join-Path $adminWebRoot 'package.json'))
}
if (-not (Test-Path -Path $toolsDir -PathType Container)) {
    New-Item -Path $toolsDir -ItemType Directory -Force | Out-Null
}
if (-not (Test-Path -Path $docsDir -PathType Container)) {
    New-Item -Path $docsDir -ItemType Directory -Force | Out-Null
}

$runnerPath = Join-Path $toolsDir 'Run-P10.2BT-Repair-AdminWebNpmBuild.ps1'
$runner = @'
Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..\..')
$adminWebRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$artifactRoot = Join-Path $repoRoot 'artifacts\p10\P10.2BT-Repair'

if (-not (Test-Path -Path $adminWebRoot -PathType Container)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}
if (-not (Test-Path -Path (Join-Path $adminWebRoot 'package.json') -PathType Leaf)) {
    throw ('Admin Web package.json was not found: {0}' -f (Join-Path $adminWebRoot 'package.json'))
}
if (-not (Test-Path -Path $artifactRoot -PathType Container)) {
    New-Item -Path $artifactRoot -ItemType Directory -Force | Out-Null
}

$stdoutLog = Join-Path $artifactRoot 'npm-build.stdout.log'
$stderrLog = Join-Path $artifactRoot 'npm-build.stderr.log'
$summaryLog = Join-Path $artifactRoot 'npm-build.summary.md'

if (Test-Path -Path $stdoutLog -PathType Leaf) { Remove-Item -Path $stdoutLog -Force }
if (Test-Path -Path $stderrLog -PathType Leaf) { Remove-Item -Path $stderrLog -Force }
if (Test-Path -Path $summaryLog -PathType Leaf) { Remove-Item -Path $summaryLog -Force }

$npmCommand = Get-Command npm.cmd -ErrorAction SilentlyContinue
if ($null -eq $npmCommand) {
    $npmCommand = Get-Command npm -ErrorAction SilentlyContinue
}
if ($null -eq $npmCommand) {
    throw 'npm was not found on PATH.'
}

$report = New-Object System.Collections.Generic.List[string]
[void]$report.Add('# P10.2BT Repair - Admin Web npm build')
[void]$report.Add('')
[void]$report.Add(('Admin Web root: `{0}`' -f $adminWebRoot))
[void]$report.Add(('npm command: `{0}`' -f $npmCommand.Source))
[void]$report.Add(('stdout log: `{0}`' -f $stdoutLog))
[void]$report.Add(('stderr log: `{0}`' -f $stderrLog))
[void]$report.Add('')
[void]$report.Add('Command: `npm run build`')
[void]$report.Add('')

Push-Location $adminWebRoot
try {
    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = 'cmd.exe'
    $startInfo.Arguments = '/d /c npm run build'
    $startInfo.WorkingDirectory = $adminWebRoot
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.CreateNoWindow = $true

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $startInfo
    [void]$process.Start()

    $standardOutput = $process.StandardOutput.ReadToEnd()
    $standardError = $process.StandardError.ReadToEnd()
    $process.WaitForExit()

    [System.IO.File]::WriteAllText($stdoutLog, $standardOutput)
    [System.IO.File]::WriteAllText($stderrLog, $standardError)

    [void]$report.Add(('Exit code: `{0}`' -f $process.ExitCode))
    [void]$report.Add('')
    if ($process.ExitCode -eq 0) {
        [void]$report.Add('Result: build succeeded.')
    } else {
        [void]$report.Add('Result: build failed. Review stderr/stdout logs above for the complete Node/npm output.')
    }

    [System.IO.File]::WriteAllLines($summaryLog, $report.ToArray())
    Write-Host ('Wrote build summary: {0}' -f $summaryLog)
    Write-Host ('Wrote stdout log: {0}' -f $stdoutLog)
    Write-Host ('Wrote stderr log: {0}' -f $stderrLog)

    if ($process.ExitCode -ne 0) {
        exit $process.ExitCode
    }
}
finally {
    Pop-Location
}
'@
[System.IO.File]::WriteAllText($runnerPath, $runner)

$reportPath = Join-Path $docsDir 'P10.2BT-Repair-AdminWebLocalBuildVerification.md'
$report = New-Object System.Collections.Generic.List[string]
[void]$report.Add('# P10.2BT Repair - Admin Web Local Build Verification')
[void]$report.Add('')
[void]$report.Add('Applied repair runner for Admin Web npm build logging.')
[void]$report.Add('')
[void]$report.Add(('Runner: `{0}`' -f $runnerPath))
[void]$report.Add('')
[void]$report.Add('Run:')
[void]$report.Add('')
[void]$report.Add('```powershell')
[void]$report.Add('powershell -ExecutionPolicy Bypass -File .\tools\p10\P10.2BT-Repair\Run-P10.2BT-Repair-AdminWebNpmBuild.ps1')
[void]$report.Add('```')
[System.IO.File]::WriteAllLines($reportPath, $report.ToArray())

Write-Host ('Wrote runner: {0}' -f $runnerPath)
Write-Host ('Wrote report: {0}' -f $reportPath)
Write-Host 'P10.2BT Repair Admin Web local build verification applied.'
