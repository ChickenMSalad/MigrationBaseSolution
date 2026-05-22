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
$schemaPath = Join-Path $repoRoot "database/sql/operational/005_create_execution_plan_steps.sql"
$programPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Program.cs"
$compositionPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Endpoints/Operational/MigrationOperationalEndpointCompositionExtensions.cs"
$servicePath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Operational/Execution/SqlExecutionPlanStore.cs"
$uiPath = Join-Path $repoRoot "apps/migration-admin-ui/src/features/executionSessions/ExecutionSessionWorkspace.tsx"

Assert-Contains -Path $schemaPath -Text "MigrationExecutionPlanSteps"
Assert-Contains -Path $servicePath -Text "ExecutionPlanSeeded"
Assert-Contains -Path $uiPath -Text "Seed execution plan"
Assert-OccursOnce -Path $programPath -Text "builder.Services.AddScoped<IExecutionPlanStore, SqlExecutionPlanStore>();"
Assert-OccursOnce -Path $compositionPath -Text "endpoints.MapExecutionPlanEndpoints();"

Write-Host "[P4.54] Validation passed."
