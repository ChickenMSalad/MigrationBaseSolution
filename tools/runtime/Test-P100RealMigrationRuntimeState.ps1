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
$sqlPath = Join-Path $repoRoot 'database\sql\p10\002_p10_real_migration_runtime_state_validator.sql'
if (-not (Test-Path -LiteralPath $sqlPath)) {
    throw ('Required SQL validator was not found: {0}' -f $sqlPath)
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $artifactRoot = Join-Path $repoRoot 'artifacts\p10-real-migration'
    if (-not (Test-Path -LiteralPath $artifactRoot)) {
        New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
    }
    $OutputPath = Join-Path $artifactRoot ('p10-real-migration-state-{0}.txt' -f $RunId)
}
elseif (-not [System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath = Join-Path $repoRoot $OutputPath
}

$outputParent = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($outputParent) -and -not (Test-Path -LiteralPath $outputParent)) {
    New-Item -ItemType Directory -Path $outputParent -Force | Out-Null
}

$sqlServerName = $SqlServer
if ($sqlServerName -notlike '*.database.windows.net') {
    $sqlServerName = ('{0}.database.windows.net' -f $sqlServerName)
}

& sqlcmd -S $sqlServerName -d $Database -U $SqlAdmin -P $SqlPasswordPlain -v RunId=$RunId -i $sqlPath -o $OutputPath
if ($LASTEXITCODE -ne 0) {
    throw ('sqlcmd failed while validating P10 real migration runtime state. Exit code: {0}' -f $LASTEXITCODE)
}

Get-Content -LiteralPath $OutputPath
Write-Host ('P10 real migration runtime state written to {0}.' -f $OutputPath)
