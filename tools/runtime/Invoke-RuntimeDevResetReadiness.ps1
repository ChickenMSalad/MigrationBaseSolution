[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $SqlServer,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $Database,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $SqlAdmin,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $SqlPasswordPlain,

    [Parameter(Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string] $ReadinessSqlPath,

    [Parameter(Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string] $OutputPath
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    if (-not [string]::IsNullOrWhiteSpace($PSCommandPath)) {
        $scriptRoot = Split-Path -Parent $PSCommandPath
    }
}

if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    throw 'Unable to resolve script root.'
}

$repoRoot = Split-Path -Parent (Split-Path -Parent $scriptRoot)

if ([string]::IsNullOrWhiteSpace($ReadinessSqlPath)) {
    $ReadinessSqlPath = Join-Path $repoRoot 'database\sql\p7\016_runtime_dev_reset_readiness.sql'
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $outputDirectory = Join-Path $repoRoot 'artifacts\runtime-reset'
    $OutputPath = Join-Path $outputDirectory 'runtime-dev-reset-readiness.txt'
}

$readinessSqlFullPath = Resolve-Path -LiteralPath $ReadinessSqlPath
$outputFullPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)
$outputDirectoryPath = Split-Path -Parent $outputFullPath

if (-not (Test-Path -LiteralPath $outputDirectoryPath)) {
    New-Item -ItemType Directory -Path $outputDirectoryPath -Force | Out-Null
}

$sqlcmd = Get-Command sqlcmd -ErrorAction Stop
$serverName = $SqlServer
if ($serverName -notlike '*.database.windows.net') {
    $serverName = ($serverName + '.database.windows.net')
}

& $sqlcmd.Source `
    -S $serverName `
    -d $Database `
    -U $SqlAdmin `
    -P $SqlPasswordPlain `
    -i $readinessSqlFullPath.Path `
    -o $outputFullPath

if ($LASTEXITCODE -ne 0) {
    throw ('sqlcmd failed while running runtime dev reset readiness diagnostics. ExitCode={0}' -f $LASTEXITCODE)
}

Write-Host ('Runtime dev reset readiness written to {0}' -f $outputFullPath)
