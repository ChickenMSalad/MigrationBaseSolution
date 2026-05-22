[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [switch] $Apply,

    [Parameter(Mandatory = $false)]
    [switch] $WhatIf
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$payloadRoot = Join-Path $repoRoot "payload"

function Write-Step {
    param([Parameter(Mandatory = $true)][string] $Message)
    Write-Host ("[P4.8] {0}" -f $Message)
}

function Copy-FileIfChanged {
    param(
        [Parameter(Mandatory = $true)][string] $Source,
        [Parameter(Mandatory = $true)][string] $Destination
    )

    if (-not (Test-Path -LiteralPath $Source)) {
        throw ("Payload file not found: {0}" -f $Source)
    }

    $destinationDirectory = Split-Path -Parent $Destination
    if (-not (Test-Path -LiteralPath $destinationDirectory)) {
        if ($Apply) {
            New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
            Write-Step ("Created {0}" -f $destinationDirectory)
        }
        else {
            Write-Step ("WOULD create {0}" -f $destinationDirectory)
        }
    }

    if ($Apply) {
        Copy-Item -LiteralPath $Source -Destination $Destination -Force
        Write-Step ("Copied {0}" -f $Destination.Substring($repoRoot.Length + 1))
    }
    else {
        Write-Step ("WOULD copy {0} -> {1}" -f $Source, $Destination)
    }
}

if ($WhatIf) {
    $Apply = $false
}

Write-Step ("Repo root: {0}" -f $repoRoot)

$files = @(
    "deploy/azure/sql-servicebus-runtime/main.bicep",
    "deploy/azure/sql-servicebus-runtime/main.parameters.example.json",
    "deploy/azure/sql-servicebus-runtime/deploy-sql-servicebus-runtime.ps1",
    "deploy/azure/sql-servicebus-runtime/README.md",
    "config-samples/appsettings.P4.8.AzureRuntime.sample.json",
    "docs/azure/P4.8_SQL_SERVICEBUS_RUNTIME_SCAFFOLD.md"
)

foreach ($file in $files) {
    $source = Join-Path $payloadRoot $file
    $destination = Join-Path $repoRoot $file
    Copy-FileIfChanged -Source $source -Destination $destination
}

Write-Step "Complete. Next: ./patches/P4.8-Validate-SqlServiceBusRuntimeScaffold.ps1; dotnet build"
