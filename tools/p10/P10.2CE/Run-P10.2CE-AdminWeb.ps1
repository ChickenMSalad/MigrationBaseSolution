param(
    [string]$AdminApiProxyTarget = 'https://localhost:55436',
    [string]$HostName = '127.0.0.1',
    [int]$Port = 5173
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    if ($PSScriptRoot) {
        $candidate = Resolve-Path -Path (Join-Path $PSScriptRoot '..\..\..')
        return $candidate.Path
    }

    return (Get-Location).Path
}

$repoRoot = Get-RepositoryRoot
$webRoot = Join-Path $repoRoot 'src\Admin\Migration.Admin.Web'
$artifactRoot = Join-Path $repoRoot 'artifacts\p10\P10.2CE'
if (-not (Test-Path -Path $artifactRoot -PathType Container)) {
    New-Item -Path $artifactRoot -ItemType Directory -Force | Out-Null
}

if (-not (Test-Path -Path (Join-Path $webRoot 'package.json') -PathType Leaf)) {
    throw ('Admin Web package.json was not found under: {0}' -f $webRoot)
}

$stdout = Join-Path $artifactRoot 'admin-web.stdout.log'
$stderr = Join-Path $artifactRoot 'admin-web.stderr.log'
$command = ('set VITE_ADMIN_API_PROXY_TARGET={0}&& npm run dev -- --host {1} --port {2}' -f $AdminApiProxyTarget, $HostName, $Port)

Write-Host ('Starting Admin Web on http://{0}:{1}' -f $HostName, $Port)
Write-Host ('Proxy target: {0}' -f $AdminApiProxyTarget)
Write-Host ('stdout: {0}' -f $stdout)
Write-Host ('stderr: {0}' -f $stderr)

$process = Start-Process -FilePath 'cmd.exe' -ArgumentList @('/c', $command) -WorkingDirectory $webRoot -RedirectStandardOutput $stdout -RedirectStandardError $stderr -PassThru
Write-Host ('Admin Web process id: {0}' -f $process.Id)
Write-Host 'Stop it later with Stop-Process if needed.'
