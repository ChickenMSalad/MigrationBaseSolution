[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Assert-Contains {
    param([string]$Path, [string]$Text)
    if (-not (Test-Path -LiteralPath $Path)) { throw ("Expected file not found: {0}" -f $Path) }
    $content = Get-Content -LiteralPath $Path -Raw
    if (-not $content.Contains($Text)) { throw ("Expected text not found in {0}: {1}" -f $Path, $Text) }
}

function Assert-OccursOnce {
    param([string]$Path, [string]$Text)
    if (-not (Test-Path -LiteralPath $Path)) { throw ("Expected file not found: {0}" -f $Path) }
    $content = Get-Content -LiteralPath $Path -Raw
    $count = ([regex]::Matches($content, [regex]::Escape($Text))).Count
    if ($count -ne 1) { throw ("Expected text to occur once in {0}; found {1}: {2}" -f $Path, $count, $Text) }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Assert-Contains -Path (Join-Path $repoRoot "database/sql/operational/008_add_execution_replay_lineage.sql") -Text "ReplaySourceExecutionSessionId"
Assert-Contains -Path (Join-Path $repoRoot "src/Core/Migration.Admin.Api/Operational/Execution/SqlExecutionReplayMaterializationService.cs") -Text "ExecutionReplayMaterialized"
Assert-Contains -Path (Join-Path $repoRoot "src/Core/Migration.Admin.Api/Operational/Execution/SqlExecutionReplayMaterializationService.cs") -Text "MaxReplayDepth"
Assert-Contains -Path (Join-Path $repoRoot "src/Core/Migration.Admin.Api/Endpoints/Operational/Execution/ExecutionReplayMaterializationEndpointExtensions.cs") -Text "MaterializeExecutionReplay"
Assert-Contains -Path (Join-Path $repoRoot "apps/migration-admin-ui/src/features/executionSessions/ExecutionSessionWorkspace.tsx") -Text "Materialize replay"
Assert-OccursOnce -Path (Join-Path $repoRoot "src/Core/Migration.Admin.Api/Program.cs") -Text "builder.Services.AddScoped<IExecutionReplayMaterializationService, SqlExecutionReplayMaterializationService>();"
Assert-OccursOnce -Path (Join-Path $repoRoot "src/Core/Migration.Admin.Api/Endpoints/Operational/MigrationOperationalEndpointCompositionExtensions.cs") -Text "endpoints.MapExecutionReplayMaterializationEndpoints();"

Write-Host "[P4.69] Validation passed."
