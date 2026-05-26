#Requires -Version 5.1
[CmdletBinding(DefaultParameterSetName = 'SqlAuth')]
param(
    [Parameter(Mandatory = $true)]
    [string] $SqlServer,

    [Parameter(Mandatory = $true)]
    [string] $DatabaseName,

    [Parameter(Mandatory = $true, ParameterSetName = 'SqlAuth')]
    [string] $SqlUser,

    [Parameter(Mandatory = $true, ParameterSetName = 'SqlAuth')]
    [string] $SqlPassword,

    [Parameter(Mandatory = $true, ParameterSetName = 'Integrated')]
    [switch] $IntegratedSecurity
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}
$repoRoot = Resolve-Path (Join-Path $scriptRoot '..\..')
$sqlPath = Join-Path $repoRoot 'database\sql\p7\008_runtime_contract_validator.sql'

if (-not (Test-Path -LiteralPath $sqlPath)) {
    throw "SQL validator not found: $sqlPath"
}

$args = @('-S', $SqlServer, '-d', $DatabaseName, '-i', $sqlPath, '-b')
if ($PSCmdlet.ParameterSetName -eq 'SqlAuth') {
    $args += @('-U', $SqlUser, '-P', $SqlPassword)
}
else {
    $args += @('-E')
}

& sqlcmd @args
if ($LASTEXITCODE -ne 0) {
    throw "Runtime SQL contract validation failed with exit code $LASTEXITCODE."
}
