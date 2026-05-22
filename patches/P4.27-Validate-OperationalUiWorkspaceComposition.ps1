[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Assert-Contains {
    param(
        [string]$Path,
        [string]$Text
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw ("File not found: {0}" -f $Path)
    }

    $content = Get-Content -LiteralPath $Path -Raw
    if (-not $content.Contains($Text)) {
        throw ("Expected text not found in {0}: {1}" -f $Path, $Text)
    }
}

function Assert-NotContains {
    param(
        [string]$Path,
        [string]$Text
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw ("File not found: {0}" -f $Path)
    }

    $content = Get-Content -LiteralPath $Path -Raw
    if ($content.Contains($Text)) {
        throw ("Unexpected text found in {0}: {1}" -f $Path, $Text)
    }
}

function Assert-OccursOnce {
    param(
        [string]$Path,
        [string]$Text
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw ("File not found: {0}" -f $Path)
    }

    $content = Get-Content -LiteralPath $Path -Raw
    $count = ([regex]::Matches($content, [regex]::Escape($Text))).Count

    if ($count -ne 1) {
        throw ("Expected text to occur once in {0}; found {1}: {2}" -f $Path, $count, $Text)
    }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$appPath = Join-Path $repoRoot "apps/migration-admin-ui/src/App.tsx"
$compositionPath = Join-Path $repoRoot "apps/migration-admin-ui/src/features/operational/OperationalWorkspaceComposition.tsx"

Assert-OccursOnce `
    -Path $appPath `
    -Text "import { OperationalWorkspaceComposition } from './features/operational/OperationalWorkspaceComposition';"

Assert-OccursOnce `
    -Path $appPath `
    -Text "<OperationalWorkspaceComposition />"

Assert-NotContains `
    -Path $appPath `
    -Text "<WorkerTelemetryWorkspace />"

Assert-NotContains `
    -Path $appPath `
    -Text "<ConnectorConfigurationWorkspace />"

Assert-Contains `
    -Path $compositionPath `
    -Text "import { OperationalRuntimeDashboard } from '../../components/OperationalRuntimeDashboard';"

Assert-Contains `
    -Path $compositionPath `
    -Text "import { FailureRetryWorkspace } from '../../components/FailureRetryWorkspace';"

Assert-Contains `
    -Path $compositionPath `
    -Text "import { ExecutionProfileWorkspace } from '../executionProfiles/ExecutionProfileWorkspace';"

Assert-Contains `
    -Path $compositionPath `
    -Text "<ExecutionProfileWorkspace />"

Write-Host "[P4.27-REPAIR] Validation passed."
