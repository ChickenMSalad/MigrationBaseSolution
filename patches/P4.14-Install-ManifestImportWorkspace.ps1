[CmdletBinding()]
param(
    [switch]$WhatIf,
    [switch]$Apply
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host ("[P4.14] {0}" -f $Message)
}

function Copy-FileSafe {
    param(
        [string]$Source,
        [string]$Destination,
        [switch]$Apply
    )

    if (-not (Test-Path -LiteralPath $Source)) {
        throw ("Source file not found: {0}" -f $Source)
    }

    $destinationDirectory = Split-Path -Parent $Destination

    if (-not (Test-Path -LiteralPath $destinationDirectory)) {
        if ($Apply) {
            New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
            Write-Step ("Created {0}" -f $destinationDirectory)
        }
        else {
            Write-Step ("WOULD create {0}" -f $destinationDirectory)
        }
    }

    if ($Apply) {
        Copy-Item -LiteralPath $Source -Destination $Destination -Force
        Write-Step ("Copied {0}" -f $Destination)
    }
    else {
        Write-Step ("WOULD copy {0} -> {1}" -f $Source, $Destination)
    }
}

function Add-TextIfMissing {
    param(
        [string]$Path,
        [string]$Text,
        [string]$InsertAfterPattern,
        [switch]$Apply
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw ("File not found: {0}" -f $Path)
    }

    $content = Get-Content -LiteralPath $Path -Raw

    if ($content.Contains($Text)) {
        Write-Step ("Already present in {0}: {1}" -f $Path, $Text)
        return
    }

    $match = [regex]::Match($content, $InsertAfterPattern, [System.Text.RegularExpressions.RegexOptions]::Multiline)

    if (-not $match.Success) {
        throw ("Insert pattern not found in {0}: {1}" -f $Path, $InsertAfterPattern)
    }

    $updated = $content.Insert($match.Index + $match.Length, "`r`n" + $Text)

    if ($Apply) {
        Set-Content -LiteralPath $Path -Value $updated -Encoding UTF8
        Write-Step ("Updated {0}" -f $Path)
    }
    else {
        Write-Step ("WOULD update {0}" -f $Path)
    }
}

function Add-ManifestPanelIfMissing {
    param(
        [string]$Path,
        [switch]$Apply
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw ("File not found: {0}" -f $Path)
    }

    $content = Get-Content -LiteralPath $Path -Raw

    if ($content.Contains("<ManifestImportPanel />")) {
        Write-Step ("ManifestImportPanel already wired in {0}" -f $Path)
        return
    }

    $marker = "</main>"
    $index = $content.IndexOf($marker, [System.StringComparison]::Ordinal)

    if ($index -lt 0) {
        throw ("Closing </main> marker not found in {0}" -f $Path)
    }

    $componentText = "      <ManifestImportPanel />`r`n"
    $updated = $content.Insert($index, $componentText)

    if ($Apply) {
        Set-Content -LiteralPath $Path -Value $updated -Encoding UTF8
        Write-Step ("Wired ManifestImportPanel into {0}" -f $Path)
    }
    else {
        Write-Step ("WOULD wire ManifestImportPanel into {0}" -f $Path)
    }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$payloadRoot = Join-Path $repoRoot "payload"
$uiRoot = Join-Path $repoRoot "apps\migration-admin-ui"
$appTsx = Join-Path $uiRoot "src\App.tsx"
$copyApply = [bool]$Apply

Write-Step ("Repo root: {0}" -f $repoRoot)

if (-not (Test-Path -LiteralPath $uiRoot)) {
    throw ("UI root not found: {0}" -f $uiRoot)
}

Copy-FileSafe `
    -Source (Join-Path $payloadRoot "apps\migration-admin-ui\src\lib\manifestImportApi.ts") `
    -Destination (Join-Path $uiRoot "src\lib\manifestImportApi.ts") `
    -Apply:$copyApply

Copy-FileSafe `
    -Source (Join-Path $payloadRoot "apps\migration-admin-ui\src\components\ManifestImportPanel.tsx") `
    -Destination (Join-Path $uiRoot "src\components\ManifestImportPanel.tsx") `
    -Apply:$copyApply

Copy-FileSafe `
    -Source (Join-Path $payloadRoot "docs\ui\P4.14-manifest-import-workspace.md") `
    -Destination (Join-Path $repoRoot "docs\ui\P4.14-manifest-import-workspace.md") `
    -Apply:$copyApply

Add-TextIfMissing `
    -Path $appTsx `
    -Text "import { ManifestImportPanel } from './components/ManifestImportPanel';" `
    -InsertAfterPattern "^import .+;$" `
    -Apply:$copyApply

Add-ManifestPanelIfMissing `
    -Path $appTsx `
    -Apply:$copyApply

Write-Step "Complete. Next: ./patches/P4.14-Validate-ManifestImportWorkspace.ps1; npm run build from apps/migration-admin-ui"
