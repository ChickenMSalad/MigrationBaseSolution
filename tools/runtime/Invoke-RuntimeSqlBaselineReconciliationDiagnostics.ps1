[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $SqlServer,

    [Parameter(Mandatory = $true)]
    [string] $Database,

    [Parameter(Mandatory = $true)]
    [string] $SqlAdmin,

    [Parameter(Mandatory = $true)]
    [string] $SqlPasswordPlain,

    [Parameter(Mandatory = $false)]
    [string] $OutputPath
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Resolve-RepoRoot {
    param(
        [Parameter(Mandatory = $false)]
        [string] $StartPath
    )

    if ([string]::IsNullOrWhiteSpace($StartPath)) {
        $StartPath = $PSScriptRoot
    }

    if ([string]::IsNullOrWhiteSpace($StartPath)) {
        $StartPath = Split-Path -Parent $PSCommandPath
    }

    if ([string]::IsNullOrWhiteSpace($StartPath)) {
        throw 'Unable to resolve script root.'
    }

    return (Resolve-Path (Join-Path $StartPath '..\..')).Path
}

function Resolve-SqlServerEndpoint {
    param(
        [Parameter(Mandatory = $true)]
        [string] $SqlServer
    )

    if ($SqlServer.IndexOf('.') -ge 0) {
        return $SqlServer
    }

    return ('{0}.database.windows.net' -f $SqlServer)
}

function Invoke-SqlFileChecked {
    param(
        [Parameter(Mandatory = $true)]
        [string] $SqlServer,

        [Parameter(Mandatory = $true)]
        [string] $Database,

        [Parameter(Mandatory = $true)]
        [string] $SqlAdmin,

        [Parameter(Mandatory = $true)]
        [string] $SqlPasswordPlain,

        [Parameter(Mandatory = $true)]
        [string] $SqlFile,

        [Parameter(Mandatory = $false)]
        [string] $OutputPath
    )

    $serverEndpoint = Resolve-SqlServerEndpoint -SqlServer $SqlServer
    $arguments = @('-S', $serverEndpoint, '-d', $Database, '-U', $SqlAdmin, '-P', $SqlPasswordPlain, '-i', $SqlFile, '-b')

    if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
        $outputDirectory = Split-Path -Parent $OutputPath
        if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
            New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
        }
        $arguments += @('-o', $OutputPath)
    }

    & sqlcmd @arguments
    if ($LASTEXITCODE -ne 0) {
        throw ('sqlcmd failed for SQL file: {0}' -f $SqlFile)
    }
}

$repoRoot = Resolve-RepoRoot
$sqlFile = Join-Path $repoRoot 'database\sql\p7\019_runtime_sql_baseline_reconciliation_diagnostics.sql'

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $repoRoot 'artifacts\runtime-sql-baseline\p7.9h-diagnostics.txt'
}

Invoke-SqlFileChecked -SqlServer $SqlServer -Database $Database -SqlAdmin $SqlAdmin -SqlPasswordPlain $SqlPasswordPlain -SqlFile $sqlFile -OutputPath $OutputPath
Write-Host ('P7.9H diagnostics written to {0}' -f $OutputPath)
