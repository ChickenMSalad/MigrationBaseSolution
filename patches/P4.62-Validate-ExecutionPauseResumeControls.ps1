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
$storePath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Operational/Execution/SqlExecutionWorkItemQueueStore.cs"
$programPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Program.cs"
$compositionPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Endpoints/Operational/MigrationOperationalEndpointCompositionExtensions.cs"
$endpointPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Endpoints/Operational/Execution/ExecutionControlEndpointExtensions.cs"

Assert-Contains -Path $storePath -Text "ExecutionWorkItemLeaseSkipped"
Assert-Contains -Path $storePath -Text "ReadSessionStatusAsync"
Assert-Contains -Path $endpointPath -Text 'MapPost("/pause"'
Assert-Contains -Path $endpointPath -Text 'MapPost("/resume"'
Assert-OccursOnce -Path $programPath -Text "builder.Services.AddScoped<IExecutionControlService, SqlExecutionControlService>();"
Assert-OccursOnce -Path $compositionPath -Text "endpoints.MapExecutionControlEndpoints();"

Write-Host "[P4.62] Validation passed."
