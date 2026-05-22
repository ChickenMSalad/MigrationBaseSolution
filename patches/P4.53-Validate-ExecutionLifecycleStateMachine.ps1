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
$schemaPath = Join-Path $repoRoot "database/sql/operational/004_create_execution_phase_history.sql"
$programPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Program.cs"
$compositionPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Endpoints/Operational/MigrationOperationalEndpointCompositionExtensions.cs"
$servicePath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Operational/Execution/SqlExecutionLifecycleService.cs"
$uiPath = Join-Path $repoRoot "apps/migration-admin-ui/src/features/executionSessions/ExecutionSessionWorkspace.tsx"

Assert-Contains -Path $schemaPath -Text "MigrationExecutionPhaseHistory"
Assert-Contains -Path $servicePath -Text "ExecutionPhaseTransitioned"
Assert-Contains -Path $uiPath -Text "Transition selected session"
Assert-OccursOnce -Path $programPath -Text "builder.Services.AddScoped<IExecutionLifecycleService, SqlExecutionLifecycleService>();"
Assert-OccursOnce -Path $compositionPath -Text "endpoints.MapExecutionLifecycleEndpoints();"

Write-Host "[P4.53] Validation passed."
