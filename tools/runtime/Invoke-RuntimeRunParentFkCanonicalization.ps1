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
    [string] $RepoRoot
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Resolve-RepoRoot {
    param(
        [Parameter(Mandatory = $false)]
        [string] $StartingPath
    )

    if ([string]::IsNullOrWhiteSpace($StartingPath)) {
        $StartingPath = $PSScriptRoot
    }

    if ([string]::IsNullOrWhiteSpace($StartingPath)) {
        $StartingPath = Split-Path -Parent $PSCommandPath
    }

    if ([string]::IsNullOrWhiteSpace($StartingPath)) {
        throw 'Unable to resolve script location.'
    }

    $candidate = Resolve-Path -LiteralPath $StartingPath
    $current = Get-Item -LiteralPath $candidate.Path

    while ($null -ne $current) {
        $solutionPath = Join-Path $current.FullName 'MigrationBaseSolution.sln'
        if (Test-Path -LiteralPath $solutionPath) {
            return $current.FullName
        }

        $current = $current.Parent
    }

    throw 'Unable to locate repository root containing MigrationBaseSolution.sln.'
}

function Invoke-SqlFile {
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
        [string] $SqlFilePath
    )

    $sqlcmd = Get-Command sqlcmd -ErrorAction Stop
    $serverName = $SqlServer
    if ($serverName -notmatch '\.database\.windows\.net$') {
        $serverName = ('{0}.database.windows.net' -f $serverName)
    }

    $arguments = @(
        '-S', $serverName,
        '-d', $Database,
        '-U', $SqlAdmin,
        '-P', $SqlPasswordPlain,
        '-b',
        '-i', $SqlFilePath
    )

    & $sqlcmd.Source @arguments

    if ($LASTEXITCODE -ne 0) {
        throw ('sqlcmd failed for script: {0}' -f $SqlFilePath)
    }
}


if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = Resolve-RepoRoot
}
else {
    $RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path
}

$sqlPath = Join-Path $RepoRoot 'database\sql\p7_runtime_run_parent_fk_canonicalization.sql'
if (-not (Test-Path -LiteralPath $sqlPath)) {
    throw ('Required SQL file not found: {0}' -f $sqlPath)
}

Write-Host 'Applying P7.9A runtime run parent FK canonicalization...'
Invoke-SqlFile -SqlServer $SqlServer -Database $Database -SqlAdmin $SqlAdmin -SqlPasswordPlain $SqlPasswordPlain -SqlFilePath $sqlPath
