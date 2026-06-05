Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path -Path (Join-Path -Path $PSScriptRoot -ChildPath '..\..\..')).Path
$adminWebRoot = Join-Path -Path $repoRoot -ChildPath 'src\Admin\Migration.Admin.Web'
$artifactRoot = Join-Path -Path $repoRoot -ChildPath 'artifacts\p10\P10.2CC'
$stdoutLog = Join-Path -Path $artifactRoot -ChildPath 'vite-dev.stdout.log'
$stderrLog = Join-Path -Path $artifactRoot -ChildPath 'vite-dev.stderr.log'
$summaryPath = Join-Path -Path $artifactRoot -ChildPath 'vite-dev.summary.md'

if (-not (Test-Path -Path $artifactRoot -PathType Container)) {
    New-Item -Path $artifactRoot -ItemType Directory -Force | Out-Null
}

if (-not (Test-Path -Path $adminWebRoot -PathType Container)) {
    throw ('Admin Web root was not found: {0}' -f $adminWebRoot)
}

$npmCommand = (Get-Command npm.cmd -ErrorAction SilentlyContinue)
if ($null -eq $npmCommand) {
    throw 'npm.cmd was not found on PATH.'
}

$packageJson = Join-Path -Path $adminWebRoot -ChildPath 'package.json'
if (-not (Test-Path -Path $packageJson -PathType Leaf)) {
    throw ('package.json was not found: {0}' -f $packageJson)
}

if (Test-Path -Path $stdoutLog -PathType Leaf) { Remove-Item -Path $stdoutLog -Force }
if (Test-Path -Path $stderrLog -PathType Leaf) { Remove-Item -Path $stderrLog -Force }

$arguments = @('run', 'dev', '--', '--host', '127.0.0.1', '--port', '5173')
$process = Start-Process -FilePath $npmCommand.Source -ArgumentList $arguments -WorkingDirectory $adminWebRoot -RedirectStandardOutput $stdoutLog -RedirectStandardError $stderrLog -PassThru -WindowStyle Hidden

$uri = 'http://127.0.0.1:5173/'
$success = $false
$statusCode = ''
$errorMessage = ''

try {
    for ($attempt = 1; $attempt -le 30; $attempt++) {
        Start-Sleep -Seconds 1
        try {
            $response = Invoke-WebRequest -Uri $uri -UseBasicParsing -TimeoutSec 3
            $statusCode = [string]$response.StatusCode
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 500) {
                $success = $true
                break
            }
        }
        catch {
            $errorMessage = $_.Exception.Message
        }

        if ($process.HasExited) {
            break
        }
    }
}
finally {
    if (-not $process.HasExited) {
        Stop-Process -Id $process.Id -Force
        $process.WaitForExit()
    }
}

$summary = New-Object 'System.Collections.Generic.List[string]'
[void]$summary.Add('# P10.2CC - Admin Web Vite Dev Smoke')
[void]$summary.Add('')
[void]$summary.Add(('Admin Web root: `{0}`' -f $adminWebRoot))
[void]$summary.Add(('Smoke URL: `{0}`' -f $uri))
[void]$summary.Add(('stdout log: `{0}`' -f $stdoutLog))
[void]$summary.Add(('stderr log: `{0}`' -f $stderrLog))
[void]$summary.Add(('Process exit code: `{0}`' -f $process.ExitCode))
[void]$summary.Add(('HTTP status code: `{0}`' -f $statusCode))
[void]$summary.Add(('Result: `{0}`' -f $(if ($success) { 'passed' } else { 'failed' })))
if (-not $success -and -not [string]::IsNullOrWhiteSpace($errorMessage)) {
    [void]$summary.Add('')
    [void]$summary.Add(('Last error: `{0}`' -f $errorMessage))
}
[System.IO.File]::WriteAllLines($summaryPath, $summary.ToArray(), [System.Text.Encoding]::UTF8)

Write-Host ('Wrote smoke summary: {0}' -f $summaryPath)

if (-not $success) {
    throw ('Admin Web Vite dev smoke failed. Review {0}, {1}, and {2}.' -f $summaryPath, $stdoutLog, $stderrLog)
}

Write-Host 'P10.2CC Admin Web Vite dev smoke passed.'
