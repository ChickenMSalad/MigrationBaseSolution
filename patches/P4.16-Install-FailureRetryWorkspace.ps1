[CmdletBinding()]
param(
    [switch]$WhatIf,
    [switch]$Apply
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host ("[P4.16] {0}" -f $Message)
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
        Write-Step ("SKIP patch; pattern not found in {0}: {1}" -f $Path, $InsertAfterPattern)
        return
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

function Add-FailureRetryWorkspaceIfMissing {
    param(
        [string]$Path,
        [switch]$Apply
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw ("File not found: {0}" -f $Path)
    }

    $content = Get-Content -LiteralPath $Path -Raw

    if ($content.Contains("<FailureRetryWorkspace />")) {
        Write-Step ("FailureRetryWorkspace already wired in {0}" -f $Path)
        return
    }

    $marker = "</main>"
    $index = $content.IndexOf($marker, [System.StringComparison]::Ordinal)

    if ($index -lt 0) {
        Write-Step ("SKIP component patch; closing </main> marker not found in {0}" -f $Path)
        return
    }

    $componentText = "      <FailureRetryWorkspace />`r`n"
    $updated = $content.Insert($index, $componentText)

    if ($Apply) {
        Set-Content -LiteralPath $Path -Value $updated -Encoding UTF8
        Write-Step ("Wired FailureRetryWorkspace into {0}" -f $Path)
    }
    else {
        Write-Step ("WOULD wire FailureRetryWorkspace into {0}" -f $Path)
    }
}

if ($WhatIf -and $Apply) {
    throw "Use either -WhatIf or -Apply, not both."
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$payloadRoot = Join-Path $repoRoot "payload"
$uiRoot = Join-Path $repoRoot "apps\migration-admin-ui"
$appTsx = Join-Path $uiRoot "src\App.tsx"

Write-Step ("Repo root: {0}" -f $repoRoot)

if (-not (Test-Path -LiteralPath $uiRoot)) {
    throw ("UI root not found: {0}" -f $uiRoot)
}

Copy-FileSafe `
    -Source (Join-Path $payloadRoot "apps\migration-admin-ui\src\lib\failureRetryApi.ts") `
    -Destination (Join-Path $uiRoot "src\lib\failureRetryApi.ts") `
    -Apply:$Apply

Copy-FileSafe `
    -Source (Join-Path $payloadRoot "apps\migration-admin-ui\src\components\FailureRetryWorkspace.tsx") `
    -Destination (Join-Path $uiRoot "src\components\FailureRetryWorkspace.tsx") `
    -Apply:$Apply

Copy-FileSafe `
    -Source (Join-Path $payloadRoot "docs\ui\P4.16-failure-retry-workspace.md") `
    -Destination (Join-Path $repoRoot "docs\ui\P4.16-failure-retry-workspace.md") `
    -Apply:$Apply

Add-TextIfMissing `
    -Path $appTsx `
    -Text "import { FailureRetryWorkspace } from './components/FailureRetryWorkspace';" `
    -InsertAfterPattern "^import .*?;\s*$" `
    -Apply:$Apply

Add-FailureRetryWorkspaceIfMissing -Path $appTsx -Apply:$Apply

Write-Step "Complete. Next: ./patches/P4.16-Validate-FailureRetryWorkspace.ps1; cd apps/migration-admin-ui; npm run build"
