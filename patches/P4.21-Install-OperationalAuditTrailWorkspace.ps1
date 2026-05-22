[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [switch]$Apply
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host "[P4.21] $Message"
}

function Copy-PayloadFile {
    param(
        [string]$RelativePath
    )

    $source = Join-Path $payloadRoot $RelativePath
    $target = Join-Path $repoRoot $RelativePath

    if (-not (Test-Path -LiteralPath $source)) {
        throw "Payload file not found: $source"
    }

    if (-not $Apply) {
        Write-Step ("WOULD copy {0}" -f $RelativePath)
        return
    }

    $targetDirectory = Split-Path -Parent $target
    if (-not (Test-Path -LiteralPath $targetDirectory)) {
        New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
    }

    Copy-Item -LiteralPath $source -Destination $target -Force
    Write-Step ("Copied {0}" -f $RelativePath)
}

function Add-TextOnce {
    param(
        [string]$Path,
        [string]$Text,
        [string]$Anchor,
        [string]$Description
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "File not found: $Path"
    }

    $content = Get-Content -LiteralPath $Path -Raw

    if ($content.Contains($Text)) {
        Write-Step ("Already present: {0}" -f $Description)
        return
    }

    if (-not $content.Contains($Anchor)) {
        throw ("Anchor not found in {0}: {1}" -f $Path, $Anchor)
    }

    if (-not $Apply) {
        Write-Step ("WOULD add {0}" -f $Description)
        return
    }

    $updated = $content.Replace($Anchor, $Text + [Environment]::NewLine + $Anchor)
    Set-Content -LiteralPath $Path -Value $updated -Encoding UTF8
    Write-Step ("Added {0}" -f $Description)
}

function Add-LineAfterLastImport {
    param(
        [string]$Path,
        [string]$ImportLine
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "File not found: $Path"
    }

    $content = Get-Content -LiteralPath $Path -Raw

    if ($content.Contains($ImportLine)) {
        Write-Step ("Already present: {0}" -f $ImportLine)
        return
    }

    if (-not $Apply) {
        Write-Step ("WOULD add import {0}" -f $ImportLine)
        return
    }

    $lines = @(Get-Content -LiteralPath $Path)
    $lastImportIndex = -1

    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^import\s+') {
            $lastImportIndex = $i
        }
    }

    if ($lastImportIndex -lt 0) {
        $newLines = @($ImportLine) + $lines
    }
    else {
        $before = @()
        if ($lastImportIndex -ge 0) {
            $before = $lines[0..$lastImportIndex]
        }

        $after = @()
        if ($lastImportIndex + 1 -lt $lines.Count) {
            $after = $lines[($lastImportIndex + 1)..($lines.Count - 1)]
        }

        $newLines = $before + @($ImportLine) + $after
    }

    Set-Content -LiteralPath $Path -Value $newLines -Encoding UTF8
    Write-Step ("Added import {0}" -f $ImportLine)
}

function Add-ComponentBeforeMainClose {
    param(
        [string]$Path,
        [string]$ComponentLine
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "File not found: $Path"
    }

    $content = Get-Content -LiteralPath $Path -Raw

    if ($content.Contains($ComponentLine)) {
        Write-Step ("Already present: {0}" -f $ComponentLine.Trim())
        return
    }

    if (-not $content.Contains("</main>")) {
        throw ("Anchor not found in {0}: </main>" -f $Path)
    }

    if (-not $Apply) {
        Write-Step ("WOULD add component {0}" -f $ComponentLine.Trim())
        return
    }

    $updated = $content.Replace("</main>", $ComponentLine + [Environment]::NewLine + "    </main>")
    Set-Content -LiteralPath $Path -Value $updated -Encoding UTF8
    Write-Step ("Added component {0}" -f $ComponentLine.Trim())
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$payloadRoot = Join-Path $repoRoot "payload"

Write-Step ("Repo root: {0}" -f $repoRoot)

Copy-PayloadFile "src/Core/Migration.Admin.Api/Endpoints/Operational/Audit/OperationalAuditTrailEndpointExtensions.cs"
Copy-PayloadFile "apps/migration-admin-ui/src/features/audit/auditTrailTypes.ts"
Copy-PayloadFile "apps/migration-admin-ui/src/features/audit/auditTrailApi.ts"
Copy-PayloadFile "apps/migration-admin-ui/src/features/audit/AuditTrailWorkspace.tsx"
Copy-PayloadFile "docs/operations/P4.21-operational-audit-trail-workspace.md"

$programPath = Join-Path $repoRoot "src/Core/Migration.Admin.Api/Program.cs"
Add-LineAfterLastImport $programPath "using Migration.Admin.Api.Endpoints.Operational.Audit;"
Add-TextOnce `
    -Path $programPath `
    -Text "app.MapOperationalAuditTrailEndpoints();" `
    -Anchor "app.MapOperationalConnectorExecutionProfileEndpoints();" `
    -Description "audit trail endpoint registration"

$appPath = Join-Path $repoRoot "apps/migration-admin-ui/src/App.tsx"
Add-LineAfterLastImport $appPath "import { AuditTrailWorkspace } from './features/audit/AuditTrailWorkspace';"
Add-ComponentBeforeMainClose $appPath "      <AuditTrailWorkspace />"

Write-Step "Complete."
