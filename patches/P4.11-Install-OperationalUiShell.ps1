[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [switch]$Apply
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Step {
    param([string]$Message)
    Write-Host "[P4.11] $Message"
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$payloadRoot = Join-Path $repoRoot 'payload'
$sourceUi = Join-Path $payloadRoot 'apps\migration-admin-ui'
$targetUi = Join-Path $repoRoot 'apps\migration-admin-ui'
$sourceDoc = Join-Path $payloadRoot '..\docs\ui\P4.11-operational-ui-shell.md'
$targetDocDir = Join-Path $repoRoot 'docs\ui'
$targetDoc = Join-Path $targetDocDir 'P4.11-operational-ui-shell.md'

Write-Step "Repo root: $repoRoot"

if (-not (Test-Path -LiteralPath $sourceUi)) {
    throw "Payload UI path not found: $sourceUi"
}

if ($Apply) {
    if (-not (Test-Path -LiteralPath (Split-Path -Parent $targetUi))) {
        New-Item -ItemType Directory -Path (Split-Path -Parent $targetUi) | Out-Null
    }

    if (Test-Path -LiteralPath $targetUi) {
        throw "Target UI path already exists: $targetUi"
    }

    if ($PSCmdlet.ShouldProcess($targetUi, 'Copy operational UI shell')) {
        Copy-Item -Recurse -LiteralPath $sourceUi -Destination $targetUi
        Write-Step "Copied apps\migration-admin-ui"
    }

    if (-not (Test-Path -LiteralPath $targetDocDir)) {
        New-Item -ItemType Directory -Path $targetDocDir | Out-Null
    }

    if ($PSCmdlet.ShouldProcess($targetDoc, 'Copy P4.11 UI documentation')) {
        if ((Resolve-Path -LiteralPath $sourceDoc).Path -ne (Resolve-Path -LiteralPath $targetDoc -ErrorAction SilentlyContinue).Path) {
	    Copy-Item -LiteralPath $sourceDoc -Destination $targetDoc -Force
	}
        Write-Step "Copied docs\ui\P4.11-operational-ui-shell.md"
    }
}
else {
    Write-Step "WOULD copy $sourceUi -> $targetUi"
    Write-Step "WOULD copy $sourceDoc -> $targetDoc"
}

Write-Step 'Complete. Next: ./patches/P4.11-Validate-OperationalUiShell.ps1; dotnet build; optional npm build under apps/migration-admin-ui.'
