[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

$requiredFiles = @(
    'src/Core/Migration.Admin.Api/Endpoints/Operational/Connectors/OperationalConnectorExecutionProfileEndpointExtensions.cs',
    'apps/migration-admin-ui/src/features/executionProfiles/executionProfileTypes.ts',
    'apps/migration-admin-ui/src/features/executionProfiles/executionProfileApi.ts',
    'apps/migration-admin-ui/src/features/executionProfiles/ExecutionProfileWorkspace.tsx',
    'docs/operations/P4.20-connector-execution-profiles-runtime-policies.md'
)

foreach ($file in $requiredFiles) {
    $path = Join-Path $repoRoot $file
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required file not found: $file"
    }
}

$programPath = Join-Path $repoRoot 'src/Core/Migration.Admin.Api/Program.cs'
$programText = Get-Content -LiteralPath $programPath -Raw
if (-not $programText.Contains('MapOperationalConnectorExecutionProfileEndpoints')) {
    throw 'Program.cs does not register MapOperationalConnectorExecutionProfileEndpoints.'
}

$appPath = Join-Path $repoRoot 'apps/migration-admin-ui/src/App.tsx'
if (Test-Path -LiteralPath $appPath) {
    $appText = Get-Content -LiteralPath $appPath -Raw
    if (-not $appText.Contains('ExecutionProfileWorkspace')) {
        throw 'App.tsx does not reference ExecutionProfileWorkspace.'
    }
}

Write-Host '[P4.20] Validation passed.'
