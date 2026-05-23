[CmdletBinding(SupportsShouldProcess = $true)]
param([switch]$Apply)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step { param([string]$Message) Write-Host ("[P4.78] {0}" -f $Message) }

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

function Add-LineOnce {
    param([string]$Path, [string]$Line, [string]$Anchor)
    if (-not (Test-Path -LiteralPath $Path)) { throw ("File not found: {0}" -f $Path) }
    $content = Get-Content -LiteralPath $Path -Raw
    if ($content.Contains($Line)) { Write-Step ("Already present: {0}" -f $Line); return }
    if (-not $content.Contains($Anchor)) { throw ("Anchor not found in {0}: {1}" -f $Path, $Anchor) }
    if (-not $Apply) { Write-Step ("WOULD add line {0}" -f $Line); return }
    $updated = $content.Replace($Anchor, $Line + [Environment]::NewLine + $Anchor)
    Set-Content -LiteralPath $Path -Value $updated -Encoding UTF8
    Write-Step ("Added line {0}" -f $Line)
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$payloadRoot = Join-Path $repoRoot "payload"

Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Execution/ExecutionReplayAdmissionBackgroundOptions.cs"
Copy-PayloadFile "src/Core/Migration.Admin.Api/Operational/Execution/ExecutionReplayAdmissionBackgroundService.cs"
Copy-PayloadFile "config-samples/appsettings.ExecutionReplayAdmissionBackground.sample.json"
Copy-PayloadFile "docs/operations/P4.78-replay-admission-background-evaluator.md"

$programPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Program.cs"

Add-LineOnce `
    -Path $programPath `
    -Line "builder.Services.Configure<ExecutionReplayAdmissionBackgroundOptions>(builder.Configuration.GetSection(ExecutionReplayAdmissionBackgroundOptions.SectionName));" `
    -Anchor "builder.Services.Configure<ExecutionReplayAdmissionOptions>(builder.Configuration.GetSection(ExecutionReplayAdmissionOptions.SectionName));"

Add-LineOnce `
    -Path $programPath `
    -Line "builder.Services.AddHostedService<ExecutionReplayAdmissionBackgroundService>();" `
    -Anchor "builder.Services.AddScoped<IExecutionReplayAdmissionService, SqlExecutionReplayAdmissionService>();"

Write-Step "Complete."
