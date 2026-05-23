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
$servicePath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Operational/Execution/SqlExecutionReplayAdmissionManualService.cs"
$endpointPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Endpoints/Operational/Execution/ExecutionReplayAdmissionManualEndpointExtensions.cs"
$serviceExtensionPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Operational/Execution/ExecutionReplayServiceCollectionExtensions.cs"
$endpointCompositionPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Endpoints/Operational/ExecutionReplayEndpointCompositionExtensions.cs"
$apiPath = Join-Path $repoRoot "apps/migration-admin-ui/src/features/executionSessions/executionReplayAdmissionManualApi.ts"

Assert-Contains -Path $servicePath -Text "ExecutionReplayForceAdmitted"
Assert-Contains -Path $servicePath -Text "ExecutionReplayForceDeferred"
Assert-Contains -Path $endpointPath -Text "ForceAdmitExecutionReplay"
Assert-Contains -Path $endpointPath -Text "ForceDeferExecutionReplay"
Assert-Contains -Path $apiPath -Text "forceAdmitExecutionReplay"
Assert-Contains -Path $apiPath -Text "forceDeferExecutionReplay"
Assert-OccursOnce -Path $serviceExtensionPath -Text "services.AddScoped<IExecutionReplayAdmissionManualService, SqlExecutionReplayAdmissionManualService>();"
Assert-OccursOnce -Path $endpointCompositionPath -Text "endpoints.MapExecutionReplayAdmissionManualEndpoints();"

Write-Host "[P4.84] Validation passed."
