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

function Assert-NotContains {
    param([string]$Path, [string]$Text)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw ("Expected file not found: {0}" -f $Path)
    }

    $content = Get-Content -LiteralPath $Path -Raw

    if ($content.Contains($Text)) {
        throw ("Unexpected text found in {0}: {1}" -f $Path, $Text)
    }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$programPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Program.cs"
$compositionPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Endpoints/Operational/MigrationOperationalEndpointCompositionExtensions.cs"
$serviceExtensionPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Operational/Execution/ExecutionReplayServiceCollectionExtensions.cs"
$endpointExtensionPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Endpoints/Operational/ExecutionReplayEndpointCompositionExtensions.cs"

Assert-Contains -Path $serviceExtensionPath -Text "AddExecutionReplayServices"
Assert-Contains -Path $endpointExtensionPath -Text "MapExecutionReplayEndpoints"

Assert-OccursOnce -Path $programPath -Text "builder.Services.AddExecutionReplayServices(builder.Configuration);"
Assert-OccursOnce -Path $compositionPath -Text "endpoints.MapExecutionReplayEndpoints();"

Assert-NotContains -Path $programPath -Text "builder.Services.Configure<ExecutionReplayAdmissionBackgroundOptions>(builder.Configuration.GetSection(ExecutionReplayAdmissionBackgroundOptions.SectionName));"
Assert-NotContains -Path $programPath -Text "builder.Services.AddScoped<IExecutionReplayAdmissionService, SqlExecutionReplayAdmissionService>();"
Assert-NotContains -Path $programPath -Text "builder.Services.AddHostedService<ExecutionReplayAdmissionBackgroundService>();"

Assert-NotContains -Path $compositionPath -Text "endpoints.MapExecutionReplayAdmissionEndpoints();"
Assert-NotContains -Path $compositionPath -Text "endpoints.MapExecutionReplayPolicyOverrideEndpoints();"
Assert-NotContains -Path $compositionPath -Text "endpoints.MapExecutionReplayPolicyEndpoints();"

Write-Host "[P4.79] Validation passed."
