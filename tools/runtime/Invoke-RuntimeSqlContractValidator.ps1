[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Server,

    [Parameter(Mandatory = $true)]
    [string]$Database,

    [Parameter(Mandatory = $true)]
    [string]$User,

    [Parameter(Mandatory = $true)]
    [string]$Password,

    [Parameter(Mandatory = $false)]
    [string]$ValidatorPath = ".\database\sql\p7\009_runtime_sql_contract_validator.sql"
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Resolve-RepoRoot {
    if (-not [string]::IsNullOrWhiteSpace($PSScriptRoot)) {
        $candidate = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..") -ErrorAction SilentlyContinue
        if ($null -ne $candidate) {
            return $candidate.ProviderPath
        }
    }

    return (Get-Location).Path
}

function Resolve-SqlcmdPath {
    $command = Get-Command sqlcmd -ErrorAction SilentlyContinue
    if ($null -eq $command) {
        throw "sqlcmd was not found on PATH. Install sqlcmd or run this from a shell where sqlcmd is available."
    }

    return $command.Source
}

$repoRoot = Resolve-RepoRoot
if ([System.IO.Path]::IsPathRooted($ValidatorPath)) {
    $resolvedValidator = $ValidatorPath
} else {
    $resolvedValidator = Join-Path $repoRoot $ValidatorPath
}

if (-not (Test-Path -LiteralPath $resolvedValidator -PathType Leaf)) {
    throw "Validator SQL file not found: $resolvedValidator"
}

$sqlcmd = Resolve-SqlcmdPath
& $sqlcmd -S $Server -d $Database -U $User -P $Password -i $resolvedValidator
if ($LASTEXITCODE -ne 0) {
    throw "Runtime SQL contract validator failed with exit code $LASTEXITCODE."
}

Write-Host "Runtime SQL contract validator passed."
