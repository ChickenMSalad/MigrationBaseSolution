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
$endpointPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Endpoints/Operational/Execution/ExecutionReplayAdmissionStatusEndpointExtensions.cs"
$compositionPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Endpoints/Operational/ExecutionReplayEndpointCompositionExtensions.cs"
$workspacePath = Join-Path $repoRoot "apps/migration-admin-ui/src/features/executionSessions/ExecutionSessionWorkspace.tsx"
$apiPath = Join-Path $repoRoot "apps/migration-admin-ui/src/features/executionSessions/executionReplayAdmissionBackgroundApi.ts"

Assert-Contains -Path $endpointPath -Text "GetExecutionReplayAdmissionBackgroundStatus"
Assert-Contains -Path $apiPath -Text "fetchExecutionReplayAdmissionBackgroundStatus"
Assert-Contains -Path $workspacePath -Text "Replay admission automation"
Assert-Contains -Path $workspacePath -Text "replayAdmissionBackgroundStatus"
Assert-OccursOnce -Path $compositionPath -Text "endpoints.MapExecutionReplayAdmissionStatusEndpoints();"

Write-Host "[P4.83] Validation passed."
