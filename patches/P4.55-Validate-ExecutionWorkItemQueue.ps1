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
$schemaPath = Join-Path $repoRoot "database/sql/operational/006_create_execution_work_items.sql"
$programPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Program.cs"
$compositionPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Endpoints/Operational/MigrationOperationalEndpointCompositionExtensions.cs"
$storePath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Operational/Execution/SqlExecutionWorkItemQueueStore.cs"

Assert-Contains -Path $schemaPath -Text "MigrationExecutionWorkItems"
Assert-Contains -Path $storePath -Text "ExecutionWorkItemsExpanded"
Assert-Contains -Path $storePath -Text "ExecutionWorkItemsLeased"
Assert-Contains -Path $storePath -Text "ExecutionWorkItemCompleted"
Assert-Contains -Path $storePath -Text "ExecutionWorkItemFailed"
Assert-OccursOnce -Path $programPath -Text "builder.Services.AddScoped<IExecutionWorkItemQueueStore, SqlExecutionWorkItemQueueStore>();"
Assert-OccursOnce -Path $compositionPath -Text "endpoints.MapExecutionWorkItemQueueEndpoints();"

Write-Host "[P4.55] Validation passed."
