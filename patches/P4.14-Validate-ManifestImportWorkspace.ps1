Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Assert-FileExists {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        throw ("Expected file not found: {0}" -f $Path)
    }
}

function Assert-TextExists {
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

Assert-FileExists -Path (Join-Path $uiRoot "src\lib\manifestImportApi.ts")
Assert-FileExists -Path (Join-Path $uiRoot "src\components\ManifestImportPanel.tsx")
Assert-FileExists -Path (Join-Path $repoRoot "docs\ui\P4.14-manifest-import-workspace.md")
Assert-TextExists -Path $appTsx -Text "import { ManifestImportPanel } from './components/ManifestImportPanel';"
Assert-TextExists -Path $appTsx -Text "<ManifestImportPanel />"

Write-Host "[P4.14] Validation passed."
