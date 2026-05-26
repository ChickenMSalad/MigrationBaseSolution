[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Dispatcher', 'Executor')]
    [string] $Role,

    [Parameter(Mandatory = $false)]
    [string] $Configuration = 'Release',

    [Parameter(Mandatory = $false)]
    [switch] $SkipClean,

    [Parameter(Mandatory = $false)]
    [switch] $NoZip
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $current = Get-Location
    while ($null -ne $current) {
        $candidate = Join-Path $current.Path 'MigrationBaseSolution.sln'
        if (Test-Path -LiteralPath $candidate) {
            return $current.Path
        }

        $parent = Split-Path -Parent $current.Path
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $current.Path) {
            break
        }

        $current = Get-Item -LiteralPath $parent
    }

    throw 'Could not locate repo root. Run this script from inside MigrationBaseSolutionRepo.'
}

function Invoke-CheckedProcess {
    param(
        [Parameter(Mandatory = $true)][string] $FilePath,
        [Parameter(Mandatory = $true)][string[]] $Arguments
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code $($LASTEXITCODE): $FilePath"
    }
}

$repoRoot = Get-RepoRoot

if ($Role -eq 'Dispatcher') {
    $projectPath = Join-Path $repoRoot 'src\Workers\Migration.Workers.ServiceBusDispatcher\Migration.Workers.ServiceBusDispatcher.csproj'
    $publishName = 'sb-dispatcher'
}
else {
    $projectPath = Join-Path $repoRoot 'src\Workers\Migration.Workers.ServiceBusExecutor\Migration.Workers.ServiceBusExecutor.csproj'
    $publishName = 'sb-executor'
}

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "Project file was not found: $projectPath"
}

$publishRoot = Join-Path $repoRoot 'artifacts\publish'
$publishPath = Join-Path $publishRoot $publishName
$zipPath = Join-Path $publishRoot ($publishName + '.zip')

if (-not $SkipClean) {
    Invoke-CheckedProcess -FilePath 'dotnet' -Arguments @('clean', $projectPath)
}

if (Test-Path -LiteralPath $publishPath) {
    Remove-Item -LiteralPath $publishPath -Recurse -Force
}

if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

if (-not (Test-Path -LiteralPath $publishRoot)) {
    New-Item -ItemType Directory -Force -Path $publishRoot | Out-Null
}

Invoke-CheckedProcess -FilePath 'dotnet' -Arguments @('publish', $projectPath, '-c', $Configuration, '-o', $publishPath)

if (-not (Test-Path -LiteralPath $publishPath)) {
    throw "Publish output was not created: $publishPath"
}

$dllName = if ($Role -eq 'Dispatcher') { 'Migration.Workers.ServiceBusDispatcher.dll' } else { 'Migration.Workers.ServiceBusExecutor.dll' }
$dllPath = Join-Path $publishPath $dllName
if (-not (Test-Path -LiteralPath $dllPath)) {
    throw "Expected worker DLL was not found in publish output: $dllPath"
}

if (-not $NoZip) {
    Push-Location $publishPath
    try {
        Invoke-CheckedProcess -FilePath 'tar' -Arguments @('-a', '-c', '-f', $zipPath, '*')
    }
    finally {
        Pop-Location
    }

    if (-not (Test-Path -LiteralPath $zipPath)) {
        throw "ZIP was not created: $zipPath"
    }
}

[PSCustomObject]@{
    Role = $Role
    ProjectPath = $projectPath
    PublishPath = $publishPath
    ZipPath = if ($NoZip) { $null } else { $zipPath }
    WorkerDll = $dllPath
}
