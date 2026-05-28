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

    [Parameter(Mandatory = $true)]
    [Guid] $RunId,

    [Parameter(Mandatory = $false)]
    [string] $SeedSqlPath
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

if ([string]::IsNullOrWhiteSpace($SeedSqlPath)) {
    $SeedSqlPath = Join-Path $repoRoot 'database\sql\p7\015_runtime_noop_smoke_seed.sql'
}

$seedSqlFullPath = $SeedSqlPath
if (-not [System.IO.Path]::IsPathRooted($seedSqlFullPath)) {
    $seedSqlFullPath = Join-Path $repoRoot $seedSqlFullPath
}

if (-not (Test-Path -LiteralPath $seedSqlFullPath)) {
    throw ('Runtime NoOp smoke seed SQL was not found: {0}' -f $seedSqlFullPath)
}

$sqlcmd = Get-Command sqlcmd -ErrorAction Stop
$sqlcmdPath = $sqlcmd.Source
$serverName = $SqlServer
if ($serverName -notmatch '\.database\.windows\.net$') {
    $serverName = ('{0}.database.windows.net' -f $serverName)
}

$arguments = @(
    '-S', $serverName,
    '-d', $Database,
    '-U', $SqlAdmin,
    '-P', $SqlPasswordPlain,
    '-v', ('RunId={0}' -f $RunId),
    '-i', $seedSqlFullPath
)

$process = Start-Process -FilePath $sqlcmdPath -ArgumentList $arguments -NoNewWindow -Wait -PassThru
if ($process.ExitCode -ne 0) {
    throw ('Runtime NoOp smoke seed failed with exit code {0}.' -f $process.ExitCode)
}

Write-Host ('Runtime NoOp smoke work item enqueued for RunId {0}.' -f $RunId)
