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
Assert-Contains -Path (Join-Path $repoRoot "src/Core/Migration.Admin.Api/Operational/Execution/SqlExecutionReplayPolicyService.cs") -Text "dead-letter-pressure-high"
Assert-Contains -Path (Join-Path $repoRoot "src/Core/Migration.Admin.Api/Operational/Execution/SqlExecutionReplayMaterializationService.cs") -Text "Replay materialization was blocked by replay policy"
Assert-Contains -Path (Join-Path $repoRoot "src/Core/Migration.Admin.Api/Endpoints/Operational/Execution/ExecutionReplayPolicyEndpointExtensions.cs") -Text "EvaluateExecutionReplayPolicy"
Assert-Contains -Path (Join-Path $repoRoot "apps/migration-admin-ui/src/features/executionSessions/ExecutionSessionWorkspace.tsx") -Text "Replay policy"
Assert-Contains -Path (Join-Path $repoRoot "apps/migration-admin-ui/src/features/executionSessions/ExecutionSessionWorkspace.tsx") -Text "Evaluate policy"
Assert-OccursOnce -Path (Join-Path $repoRoot "src/Core/Migration.Admin.Api/Program.cs") -Text "builder.Services.AddScoped<IExecutionReplayPolicyService, SqlExecutionReplayPolicyService>();"
Assert-OccursOnce -Path (Join-Path $repoRoot "src/Core/Migration.Admin.Api/Endpoints/Operational/MigrationOperationalEndpointCompositionExtensions.cs") -Text "endpoints.MapExecutionReplayPolicyEndpoints();"

Write-Host "[P4.73] Validation passed."
