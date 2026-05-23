[CmdletBinding(SupportsShouldProcess = $true)]
param([switch]$Apply)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step { param([string]$Message) Write-Host ("[P4.79] {0}" -f $Message) }

function Copy-PayloadFile {
    param([string]$RelativePath)

    $source = Join-Path $payloadRoot $RelativePath
    $target = Join-Path $repoRoot $RelativePath

    if (-not (Test-Path -LiteralPath $source)) {
        throw ("Payload file not found: {0}" -f $source)
    }

    if (-not $Apply) {
        Write-Step ("WOULD copy {0}" -f $RelativePath)
        return
    }

    $directory = Split-Path -Parent $target
    if (-not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    Copy-Item -LiteralPath $source -Destination $target -Force
    Write-Step ("Copied {0}" -f $RelativePath)
}

function Remove-LineAll {
    param([string]$Path, [string]$Line)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw ("File not found: {0}" -f $Path)
    }

    $content = Get-Content -LiteralPath $Path -Raw
    if (-not $content.Contains($Line)) {
        return
    }

    if (-not $Apply) {
        $count = ([regex]::Matches($content, [regex]::Escape($Line))).Count
        Write-Step ("WOULD remove {0} occurrence(s): {1}" -f $count, $Line)
        return
    }

    $escaped = [regex]::Escape($Line)
    $updated = [regex]::Replace($content, "^[ \t]*$escaped[ \t]*\r?\n", "", [System.Text.RegularExpressions.RegexOptions]::Multiline)
    if ($updated -eq $content) {
        $updated = $content.Replace($Line, "")
    }

    Set-Content -LiteralPath $Path -Value $updated -Encoding UTF8
    Write-Step ("Removed line: {0}" -f $Line)
}

function Add-LineBefore {
    param([string]$Path, [string]$Line, [string]$Anchor)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw ("File not found: {0}" -f $Path)
    }

    $content = Get-Content -LiteralPath $Path -Raw

    if ($content.Contains($Line)) {
        Write-Step ("Already present: {0}" -f $Line)
        return
    }

    if (-not $content.Contains($Anchor)) {
        throw ("Anchor not found in {0}: {1}" -f $Path, $Anchor)
    }

    if (-not $Apply) {
        Write-Step ("WOULD add line {0}" -f $Line)
        return
    }

    $updated = $content.Replace($Anchor, $Line + [Environment]::NewLine + $Anchor)
    Set-Content -LiteralPath $Path -Value $updated -Encoding UTF8
    Write-Step ("Added line: {0}" -f $Line)
}

function Add-LineAfter {
    param([string]$Path, [string]$Line, [string]$Anchor)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw ("File not found: {0}" -f $Path)
    }

    $content = Get-Content -LiteralPath $Path -Raw

    if ($content.Contains($Line)) {
        Write-Step ("Already present: {0}" -f $Line)
        return
    }

    if (-not $content.Contains($Anchor)) {
        throw ("Anchor not found in {0}: {1}" -f $Path, $Anchor)
    }

    if (-not $Apply) {
        Write-Step ("WOULD add line {0}" -f $Line)
        return
    }

    $updated = $content.Replace($Anchor, $Anchor + [Environment]::NewLine + $Line)
    Set-Content -LiteralPath $Path -Value $updated -Encoding UTF8
    Write-Step ("Added line: {0}" -f $Line)
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$payloadRoot = Join-Path $repoRoot "payload"

Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Execution/ExecutionReplayServiceCollectionExtensions.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Endpoints/Operational/ExecutionReplayEndpointCompositionExtensions.cs"
Copy-PayloadFile "docs/operations/P4.79-replay-composition-consolidation.md"

$programPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Program.cs"
$compositionPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Endpoints/Operational/MigrationOperationalEndpointCompositionExtensions.cs"

$serviceLines = @(
    "builder.Services.Configure<ExecutionReplayAdmissionOptions>(builder.Configuration.GetSection(ExecutionReplayAdmissionOptions.SectionName));",
    "builder.Services.Configure<ExecutionReplayAdmissionBackgroundOptions>(builder.Configuration.GetSection(ExecutionReplayAdmissionBackgroundOptions.SectionName));",
    "builder.Services.AddScoped<IExecutionDiagnosticExportService, SqlExecutionDiagnosticExportService>();",
    "builder.Services.AddScoped<IExecutionReplayAnalysisService, SqlExecutionReplayAnalysisService>();",
    "builder.Services.AddScoped<IExecutionReplayPreparationService, SqlExecutionReplayPreparationService>();",
    "builder.Services.AddScoped<IExecutionReplayMaterializationService, SqlExecutionReplayMaterializationService>();",
    "builder.Services.AddScoped<IExecutionReplayLineageService, SqlExecutionReplayLineageService>();",
    "builder.Services.AddScoped<IExecutionReplayApprovalService, SqlExecutionReplayApprovalService>();",
    "builder.Services.AddScoped<IExecutionReplayPolicyService, SqlExecutionReplayPolicyService>();",
    "builder.Services.AddScoped<IExecutionReplayPolicyOverrideService, SqlExecutionReplayPolicyOverrideService>();",
    "builder.Services.AddScoped<IExecutionReplayAdmissionService, SqlExecutionReplayAdmissionService>();",
    "builder.Services.AddHostedService<ExecutionReplayAdmissionBackgroundService>();"
)

foreach ($line in $serviceLines) {
    Remove-LineAll -Path $programPath -Line $line
}

Add-LineBefore `
    -Path $programPath `
    -Line "builder.Services.AddExecutionReplayServices(builder.Configuration);" `
    -Anchor "var app = builder.Build();"

$endpointLines = @(
    "        endpoints.MapExecutionDiagnosticExportEndpoints();",
    "        endpoints.MapExecutionReplayAnalysisEndpoints();",
    "        endpoints.MapExecutionReplayPreparationEndpoints();",
    "        endpoints.MapExecutionReplayMaterializationEndpoints();",
    "        endpoints.MapExecutionReplayLineageEndpoints();",
    "        endpoints.MapExecutionReplayApprovalEndpoints();",
    "        endpoints.MapExecutionReplayPolicyEndpoints();",
    "        endpoints.MapExecutionReplayPolicyOverrideEndpoints();",
    "        endpoints.MapExecutionReplayAdmissionEndpoints();"
)

foreach ($line in $endpointLines) {
    Remove-LineAll -Path $compositionPath -Line $line
}

Add-LineBefore `
    -Path $compositionPath `
    -Line "        endpoints.MapExecutionReplayEndpoints();" `
    -Anchor "        return endpoints;"

Write-Step "Complete."
