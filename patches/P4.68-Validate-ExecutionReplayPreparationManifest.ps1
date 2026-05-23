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
$servicePath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Operational/Execution/SqlExecutionReplayPreparationService.cs"
$endpointPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Endpoints/Operational/Execution/ExecutionReplayPreparationEndpointExtensions.cs"
$programPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Program.cs"
$compositionPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Endpoints/Operational/MigrationOperationalEndpointCompositionExtensions.cs"
$workspacePath = Join-Path $repoRoot "apps/migration-admin-ui/src/features/executionSessions/ExecutionSessionWorkspace.tsx"

Assert-Contains -Path $servicePath -Text "failed-only"
Assert-Contains -Path $servicePath -Text "incomplete-only"
Assert-Contains -Path $endpointPath -Text "PrepareExecutionReplayManifest"
Assert-Contains -Path $workspacePath -Text "Prepare replay manifest"
Assert-Contains -Path $workspacePath -Text "Replay preparation manifest"
Assert-OccursOnce -Path $programPath -Text "builder.Services.AddScoped<IExecutionReplayPreparationService, SqlExecutionReplayPreparationService>();"
Assert-OccursOnce -Path $compositionPath -Text "endpoints.MapExecutionReplayPreparationEndpoints();"

Write-Host "[P4.68] Validation passed."
