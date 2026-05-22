[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host ("[P4.16] {0}" -f $Message)
}

function Assert-FileExists {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw ("Expected file not found: {0}" -f $Path)
    }
}

function Assert-TextPresent {
    param(
        [string]$Path,
        [string]$Text
    )

    Assert-FileExists -Path $Path
    $content = Get-Content -LiteralPath $Path -Raw

    if (-not $content.Contains($Text)) {
        throw ("Expected text not found in {0}: {1}" -f $Path, $Text)
    }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$uiRoot = Join-Path $repoRoot "apps\migration-admin-ui"
$appTsx = Join-Path $uiRoot "src\App.tsx"

Assert-FileExists -Path (Join-Path $uiRoot "src\lib\failureRetryApi.ts")
Assert-FileExists -Path (Join-Path $uiRoot "src\components\FailureRetryWorkspace.tsx")
Assert-FileExists -Path (Join-Path $repoRoot "docs\ui\P4.16-failure-retry-workspace.md")
Assert-TextPresent -Path $appTsx -Text "import { FailureRetryWorkspace } from './components/FailureRetryWorkspace';"
Assert-TextPresent -Path $appTsx -Text "<FailureRetryWorkspace />"

Write-Step "Validation passed."
