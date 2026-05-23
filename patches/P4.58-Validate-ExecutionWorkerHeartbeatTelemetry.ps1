[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Assert-Contains {
    param([string]$Path, [string]$Text)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw ("Expected file not found: {0}" -f $Path)
    }

    $content = Get-Content -LiteralPath $Path -Raw
    if (-not $content.Contains($Text)) {
        throw ("Expected text not found in {0}: {1}" -f $Path, $Text)
    }
}

function Assert-OccursOnce {
    param([string]$Path, [string]$Text)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw ("Expected file not found: {0}" -f $Path)
    }

    $content = Get-Content -LiteralPath $Path -Raw
    $count = ([regex]::Matches($content, [regex]::Escape($Text))).Count
    if ($count -ne 1) {
        throw ("Expected text to occur once in {0}; found {1}: {2}" -f $Path, $count, $Text)
    }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$schemaPath = Join-Path $repoRoot "database/sql/operational/007_create_execution_worker_heartbeats.sql"
$programPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Program.cs"
$compositionPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Endpoints/Operational/MigrationOperationalEndpointCompositionExtensions.cs"
$workerPath = Join-Path $repoRoot "scripts/workers/P4.57-Invoke-LocalExecutionWorker.ps1"

Assert-Contains -Path $schemaPath -Text "MigrationExecutionWorkerHeartbeats"
Assert-Contains -Path $workerPath -Text "/api/operational/execution-workers/heartbeat"
Assert-OccursOnce -Path $programPath -Text "builder.Services.AddScoped<IExecutionWorkerHeartbeatStore, SqlExecutionWorkerHeartbeatStore>();"
Assert-OccursOnce -Path $compositionPath -Text "endpoints.MapExecutionWorkerTelemetryEndpoints();"

Write-Host "[P4.58] Validation passed."
