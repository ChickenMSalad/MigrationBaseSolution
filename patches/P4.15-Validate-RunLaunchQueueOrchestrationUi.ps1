[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Assert-Path {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw ("Expected path not found: {0}" -f $Path)
    }
}

function Assert-Text {
    param(
        [string]$Path,
        [string]$Text
    )

    Assert-Path -Path $Path
    $content = Get-Content -LiteralPath $Path -Raw

    if (-not $content.Contains($Text)) {
        throw ("Expected text not found in {0}: {1}" -f $Path, $Text)
    }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$uiRoot = Join-Path $repoRoot "apps\migration-admin-ui"
$appTsx = Join-Path $uiRoot "src\App.tsx"

Assert-Path -Path (Join-Path $uiRoot "src\lib\runLaunchApi.ts")
Assert-Path -Path (Join-Path $uiRoot "src\components\RunLaunchPanel.tsx")
Assert-Path -Path (Join-Path $repoRoot "docs\ui\P4.15-run-launch-queue-orchestration-ui.md")

Assert-Text -Path (Join-Path $uiRoot "src\lib\runLaunchApi.ts") -Text "launchMigrationRun"
Assert-Text -Path (Join-Path $uiRoot "src\components\RunLaunchPanel.tsx") -Text "RunLaunchPanel"
Assert-Text -Path $appTsx -Text "import { RunLaunchPanel } from './components/RunLaunchPanel';"
Assert-Text -Path $appTsx -Text "<RunLaunchPanel />"

Write-Host "[P4.15] Validation passed."
