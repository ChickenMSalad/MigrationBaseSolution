[CmdletBinding()]
param(
    [string]$ConfigPath = "config-samples/appsettings.AdminApi.LocalOperationalSql.sample.json"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host ("[P4.33] {0}" -f $Message)
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$resolvedConfigPath = Join-Path $repoRoot $ConfigPath

if (-not (Test-Path -LiteralPath $resolvedConfigPath)) {
    throw ("Configuration file not found: {0}" -f $resolvedConfigPath)
}

$json = Get-Content -LiteralPath $resolvedConfigPath -Raw | ConvertFrom-Json

if (-not ($json.PSObject.Properties.Name -contains "ConnectionStrings")) {
    throw "Missing ConnectionStrings section."
}

if (-not ($json.ConnectionStrings.PSObject.Properties.Name -contains "OperationalSql")) {
    throw "Missing ConnectionStrings:OperationalSql."
}

if (-not ($json.PSObject.Properties.Name -contains "OperationalSql")) {
    throw "Missing OperationalSql section."
}

if (-not ($json.OperationalSql.PSObject.Properties.Name -contains "ConnectionStringName")) {
    throw "Missing OperationalSql:ConnectionStringName."
}

if ($json.OperationalSql.ConnectionStringName -ne "OperationalSql") {
    throw ("OperationalSql:ConnectionStringName must be OperationalSql. Actual: {0}" -f $json.OperationalSql.ConnectionStringName)
}

Write-Step "Local operational SQL configuration validation passed."
