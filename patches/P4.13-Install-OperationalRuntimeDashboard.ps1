[CmdletBinding()]
param(
    [switch]$WhatIf,
    [switch]$Apply
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host "[P4.13] $Message"
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

function Add-DashboardComponentIfMissing {
    param(
        [string]$Path,
        [switch]$Apply
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw ("File not found: {0}" -f $Path)
    }

    $content = Get-Content -LiteralPath $Path -Raw

    if ($content.Contains("<OperationalRuntimeDashboard />")) {
        Write-Step ("Dashboard component already wired in {0}" -f $Path)
        return
    }

    $marker = "</main>"
    $index = $content.IndexOf($marker, [System.StringComparison]::Ordinal)

    if ($index -lt 0) {
        Write-Step ("SKIP App.tsx component patch; </main> not found in {0}" -f $Path)
        return
    }

    $componentText = "        <OperationalRuntimeDashboard />`r`n"
    $updated = $content.Insert($index, $componentText)

    if ($Apply) {
        Set-Content -LiteralPath $Path -Value $updated -Encoding UTF8
        Write-Step ("Wired OperationalRuntimeDashboard into {0}" -f $Path)
    }
    else {
        Write-Step ("WOULD wire OperationalRuntimeDashboard into {0}" -f $Path)
    }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$payloadRoot = Join-Path $repoRoot "payload"
$uiRoot = Join-Path $repoRoot "apps\migration-admin-ui"
$appTsx = Join-Path $uiRoot "src\App.tsx"

Write-Step ("Repo root: {0}" -f $repoRoot)

if (-not (Test-Path -LiteralPath $uiRoot)) {
    throw ("UI root not found: {0}" -f $uiRoot)
}

$copyApply = [bool]$Apply

Copy-FileSafe `
    -Source (Join-Path $payloadRoot "apps\migration-admin-ui\src\lib\operationalRuntimeApi.ts") `
    -Destination (Join-Path $uiRoot "src\lib\operationalRuntimeApi.ts") `
    -Apply:$copyApply

Copy-FileSafe `
    -Source (Join-Path $payloadRoot "apps\migration-admin-ui\src\components\RuntimeStatusBadge.tsx") `
    -Destination (Join-Path $uiRoot "src\components\RuntimeStatusBadge.tsx") `
    -Apply:$copyApply

Copy-FileSafe `
    -Source (Join-Path $payloadRoot "apps\migration-admin-ui\src\components\OperationalRuntimeDashboard.tsx") `
    -Destination (Join-Path $uiRoot "src\components\OperationalRuntimeDashboard.tsx") `
    -Apply:$copyApply

Copy-FileSafe `
    -Source (Join-Path $payloadRoot "docs\ui\P4.13-operational-runtime-dashboard.md") `
    -Destination (Join-Path $repoRoot "docs\ui\P4.13-operational-runtime-dashboard.md") `
    -Apply:$copyApply

if (Test-Path -LiteralPath $appTsx) {
    Add-TextIfMissing `
        -Path $appTsx `
        -Text "import { OperationalRuntimeDashboard } from './components/OperationalRuntimeDashboard';" `
        -InsertAfterPattern "^import .+;$" `
        -Apply:$copyApply

    Add-DashboardComponentIfMissing `
        -Path $appTsx `
        -Apply:$copyApply
}
else {
    Write-Step ("SKIP App.tsx patch; file not found: {0}" -f $appTsx)
}

Write-Step "Complete. Next: ./patches/P4.13-Validate-OperationalRuntimeDashboard.ps1; npm run build from apps/migration-admin-ui"
