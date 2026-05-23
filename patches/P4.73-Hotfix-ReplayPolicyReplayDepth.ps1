[CmdletBinding(SupportsShouldProcess = $true)]
param([switch]$Apply)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step { param([string]$Message) Write-Host ("[P4.73-HOTFIX] {0}" -f $Message) }

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$payloadRoot = Join-Path $repoRoot "payload"
$relativePath = "src/Core/Migration.Admin.Api/Operational/Execution/SqlExecutionReplayPolicyService.cs"
$source = Join-Path $payloadRoot $relativePath
$target = Join-Path $repoRoot $relativePath

if (-not (Test-Path -LiteralPath $source)) {
    throw ("Payload file not found: {0}" -f $source)
}

if (-not $Apply) {
    Write-Step ("WOULD copy {0}" -f $relativePath)
    Write-Step "Complete."
    return
}

Copy-Item -LiteralPath $source -Destination $target -Force
Write-Step ("Copied {0}" -f $relativePath)
Write-Step "Complete."
