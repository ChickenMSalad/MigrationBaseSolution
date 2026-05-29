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
    [string] $SqlPath,

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

if ([string]::IsNullOrWhiteSpace($SqlPath)) {
    $SqlPath = [System.IO.Path]::Combine($repoRoot, 'database\sql\p7\022_runtime_canonical_sql_baseline_handoff_inventory.sql')
}
elseif (-not [System.IO.Path]::IsPathRooted($SqlPath)) {
    $SqlPath = [System.IO.Path]::Combine($repoRoot, $SqlPath)
}

if (-not (Test-Path -LiteralPath $SqlPath)) {
    throw ('SQL inventory file not found: {0}' -f $SqlPath)
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $artifactRoot = [System.IO.Path]::Combine($repoRoot, 'artifacts\runtime-sql-baseline')
    if (-not (Test-Path -LiteralPath $artifactRoot)) {
        New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
    }
    $OutputPath = [System.IO.Path]::Combine($artifactRoot, 'p710b-sql-baseline-inventory.txt')
}
elseif (-not [System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath = [System.IO.Path]::Combine($repoRoot, $OutputPath)
}

$outputParent = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($outputParent) -and -not (Test-Path -LiteralPath $outputParent)) {
    New-Item -ItemType Directory -Path $outputParent -Force | Out-Null
}

$serverName = $SqlServer
if ($serverName -notmatch '\.database\.windows\.net$') {
    $serverName = ('{0}.database.windows.net' -f $serverName)
}

$sqlcmd = Get-Command sqlcmd -ErrorAction Stop
& $sqlcmd.Source -S $serverName -d $Database -U $SqlAdmin -P $SqlPasswordPlain -i $SqlPath -o $OutputPath
if ($LASTEXITCODE -ne 0) {
    throw ('sqlcmd inventory failed with exit code {0}.' -f $LASTEXITCODE)
}

Write-Host ('P7.10B SQL baseline inventory written to {0}' -f $OutputPath)
