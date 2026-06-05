param(
    [string]$ApiUrl = 'https://localhost:55436'
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
$artifactRoot = Join-Path $repoRoot 'artifacts\p10\P10.2CE'
if (-not (Test-Path -Path $artifactRoot -PathType Container)) {
    New-Item -Path $artifactRoot -ItemType Directory -Force | Out-Null
}

$projectPath = Join-Path $repoRoot 'src\Core\Migration.Admin.Api\Migration.Admin.Api.csproj'
if (-not (Test-Path -Path $projectPath -PathType Leaf)) {
    throw ('Admin API project was not found: {0}' -f $projectPath)
}

$stdout = Join-Path $artifactRoot 'admin-api.stdout.log'
$stderr = Join-Path $artifactRoot 'admin-api.stderr.log'

$arguments = @('run', '--project', $projectPath, '--urls', $ApiUrl)
Write-Host ('Starting Admin API on {0}' -f $ApiUrl)
Write-Host ('stdout: {0}' -f $stdout)
Write-Host ('stderr: {0}' -f $stderr)

$process = Start-Process -FilePath 'dotnet' -ArgumentList $arguments -WorkingDirectory $repoRoot -RedirectStandardOutput $stdout -RedirectStandardError $stderr -PassThru
Write-Host ('Admin API process id: {0}' -f $process.Id)
Write-Host 'Stop it later with Stop-Process if needed.'
