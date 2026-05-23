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

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$servicePath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Operational/Execution/SqlExecutionControlService.cs"
$endpointPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Endpoints/Operational/Execution/ExecutionControlEndpointExtensions.cs"
$apiPath = Join-Path $repoRoot "apps/migration-admin-ui/src/features/executionSessions/executionControlApi.ts"

Assert-Contains -Path $servicePath -Text "ExecutionSessionCancelled"
Assert-Contains -Path $servicePath -Text "Status = 'cancelled'"
Assert-Contains -Path $endpointPath -Text 'MapPost("/cancel"'
Assert-Contains -Path $apiPath -Text "cancelExecutionSession"

Write-Host "[P4.64] Validation passed."
