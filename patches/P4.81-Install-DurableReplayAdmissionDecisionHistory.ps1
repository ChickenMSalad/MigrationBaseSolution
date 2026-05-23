[CmdletBinding(SupportsShouldProcess = $true)]
param([switch]$Apply)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step { param([string]$Message) Write-Host ("[P4.81] {0}" -f $Message) }

function Copy-PayloadFile {
    param([string]$RelativePath)
    $source = Join-Path $payloadRoot $RelativePath
    $target = Join-Path $repoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $source)) { throw ("Payload file not found: {0}" -f $source) }
    if (-not $Apply) { Write-Step ("WOULD copy {0}" -f $RelativePath); return }
    $directory = Split-Path -Parent $target
    if (-not (Test-Path -LiteralPath $directory)) { New-Item -ItemType Directory -Path $directory -Force | Out-Null }
    Copy-Item -LiteralPath $source -Destination $target -Force
    Write-Step ("Copied {0}" -f $RelativePath)
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$payloadRoot = Join-Path $repoRoot "payload"

Copy-PayloadFile "database/sql/operational/012_create_execution_replay_admission_decisions.sql"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Execution/ExecutionReplayAdmissionModels.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Execution/IExecutionReplayAdmissionService.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Execution/SqlExecutionReplayAdmissionService.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Endpoints/Operational/Execution/ExecutionReplayAdmissionEndpointExtensions.cs"
Copy-PayloadFile "apps/migration-admin-ui/src/features/executionSessions/executionReplayAdmissionTypes.ts"
Copy-PayloadFile "apps/migration-admin-ui/src/features/executionSessions/executionReplayAdmissionApi.ts"
Copy-PayloadFile "docs/operations/P4.81-durable-replay-admission-decision-history.md"

Write-Step "Complete."
