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
$programPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Program.cs"
$servicePath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Operational/Execution/ExecutionReplayAdmissionBackgroundService.cs"
$optionsPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Operational/Execution/ExecutionReplayAdmissionBackgroundOptions.cs"
$configPath = Join-Path $repoRoot "config-samples/appsettings.ExecutionReplayAdmissionBackground.sample.json"

Assert-Contains -Path $servicePath -Text "ExecutionReplayAdmissionBackgroundService"
Assert-Contains -Path $servicePath -Text "IExecutionReplayAdmissionService"
Assert-Contains -Path $optionsPath -Text "ExecutionReplayAdmissionBackground"
Assert-Contains -Path $configPath -Text "IntervalSeconds"
Assert-OccursOnce -Path $programPath -Text "builder.Services.Configure<ExecutionReplayAdmissionBackgroundOptions>(builder.Configuration.GetSection(ExecutionReplayAdmissionBackgroundOptions.SectionName));"
Assert-OccursOnce -Path $programPath -Text "builder.Services.AddHostedService<ExecutionReplayAdmissionBackgroundService>();"

Write-Host "[P4.78] Validation passed."
