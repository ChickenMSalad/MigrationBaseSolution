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
$extensionPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Operational/OperationalAdminServiceCollectionExtensions.cs"

Assert-Contains -Path $extensionPath -Text "AddOperationalAdminServices"
Assert-Contains -Path $extensionPath -Text "services.AddExecutionReplayServices(configuration);"

Assert-OccursOnce -Path $programPath -Text "builder.Services.AddOperationalAdminServices(builder.Configuration);"

Assert-NotContains -Path $programPath -Text "builder.Services.AddExecutionReplayServices(builder.Configuration);"
Assert-NotContains -Path $programPath -Text "builder.Services.AddScoped<IOperationalEventStore, SqlOperationalEventStore>();"
Assert-NotContains -Path $programPath -Text "builder.Services.AddScoped<IExecutionWorkItemQueueStore, SqlExecutionWorkItemQueueStore>();"
Assert-NotContains -Path $programPath -Text "builder.Services.AddScoped<IExecutionControlService, SqlExecutionControlService>();"
Assert-NotContains -Path $programPath -Text "builder.Services.AddScoped<IExecutionPlanStore, SqlExecutionPlanStore>();"

Write-Host "[P4.80] Validation passed."
