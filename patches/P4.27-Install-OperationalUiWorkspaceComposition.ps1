[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [switch]$Apply
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host ("[P4.27] {0}" -f $Message)
}

function Copy-PayloadFile {
    param([string]$RelativePath)

    $source = Join-Path $payloadRoot $RelativePath
    $target = Join-Path $repoRoot $RelativePath

    if (-not (Test-Path -LiteralPath $source)) {
        throw ("Payload file not found: {0}" -f $source)
    }

    if (-not $Apply) {
        Write-Step ("WOULD copy {0}" -f $RelativePath)
        return
    }

    $directory = Split-Path -Parent $target
    if (-not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    Copy-Item -LiteralPath $source -Destination $target -Force
    Write-Step ("Copied {0}" -f $RelativePath)
}

function Remove-LineIfPresent {
    param(
        [string[]]$Lines,
        [string]$ExactLine
    )

    return @($Lines | Where-Object { $_ -ne $ExactLine })
}

function Normalize-AppTsx {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw ("File not found: {0}" -f $Path)
    }

    $content = Get-Content -LiteralPath $Path -Raw

    if ($content.Contains("import { OperationalWorkspaceComposition } from './features/operational/OperationalWorkspaceComposition';") -and
        $content.Contains("<OperationalWorkspaceComposition />")) {
        Write-Step "Operational workspace composition already installed"
        return
    }

    $removeImports = @(
        "import { AuditTrailWorkspace } from './features/audit/AuditTrailWorkspace';",
        "import { CapacityForecastWorkspace } from './features/capacity/CapacityForecastWorkspace';",
        "import { ConnectorConfigurationWorkspace } from './features/connectors/ConnectorConfigurationWorkspace';",
        "import { CostAnalyticsWorkspace } from './features/cost/CostAnalyticsWorkspace';",
        "import { CredentialVaultWorkspace } from './features/credentials/CredentialVaultWorkspace';",
        "import { FailureRetryWorkspace } from './features/failures/FailureRetryWorkspace';",
        "import { ManifestImportWorkspace } from './features/manifests/ManifestImportWorkspace';",
        "import { NotificationRoutingWorkspace } from './features/notifications/NotificationRoutingWorkspace';",
        "import { OperationalRuntimeDashboard } from './features/runtime/OperationalRuntimeDashboard';",
        "import { RunLaunchWorkspace } from './features/runs/RunLaunchWorkspace';",
        "import { SlaSloPolicyWorkspace } from './features/slaSlo/SlaSloPolicyWorkspace';",
        "import { WorkerTelemetryWorkspace } from './features/workers/WorkerTelemetryWorkspace';"
    )

    $removeComponents = @(
        "      <AuditTrailWorkspace />",
        "      <CapacityForecastWorkspace />",
        "      <ConnectorConfigurationWorkspace />",
        "      <CostAnalyticsWorkspace />",
        "      <CredentialVaultWorkspace />",
        "      <FailureRetryWorkspace />",
        "      <ManifestImportWorkspace />",
        "      <NotificationRoutingWorkspace />",
        "      <OperationalRuntimeDashboard />",
        "      <RunLaunchWorkspace />",
        "      <SlaSloPolicyWorkspace />",
        "      <WorkerTelemetryWorkspace />"
    )

    $lines = @(Get-Content -LiteralPath $Path)

    foreach ($line in $removeImports) {
        $lines = Remove-LineIfPresent -Lines $lines -ExactLine $line
    }

    foreach ($line in $removeComponents) {
        $lines = Remove-LineIfPresent -Lines $lines -ExactLine $line
    }

    $compositionImport = "import { OperationalWorkspaceComposition } from './features/operational/OperationalWorkspaceComposition';"

    if (-not ($lines -contains $compositionImport)) {
        $lastImportIndex = -1

        for ($i = 0; $i -lt $lines.Count; $i++) {
            if ($lines[$i] -match '^import\s+') {
                $lastImportIndex = $i
            }
        }

        if ($lastImportIndex -lt 0) {
            $lines = @($compositionImport) + $lines
        }
        else {
            $before = $lines[0..$lastImportIndex]
            $after = @()
            if ($lastImportIndex + 1 -lt $lines.Count) {
                $after = $lines[($lastImportIndex + 1)..($lines.Count - 1)]
            }

            $lines = $before + @($compositionImport) + $after
        }
    }

    $content = ($lines -join [Environment]::NewLine)

    if (-not $content.Contains("<OperationalWorkspaceComposition />")) {
        if (-not $content.Contains("</main>")) {
            throw ("Anchor not found in {0}: </main>" -f $Path)
        }

        $content = $content.Replace("</main>", "      <OperationalWorkspaceComposition />" + [Environment]::NewLine + "    </main>")
    }

    if (-not $Apply) {
        Write-Step "WOULD normalize App.tsx workspace composition"
        return
    }

    Set-Content -LiteralPath $Path -Value $content -Encoding UTF8
    Write-Step "Normalized App.tsx workspace composition"
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$payloadRoot = Join-Path $repoRoot "payload"

Copy-PayloadFile "apps/migration-admin-ui/src/features/operational/OperationalWorkspaceComposition.tsx"
Copy-PayloadFile "docs/ui/P4.27-operational-ui-workspace-composition.md"

$appPath = Join-Path $repoRoot "apps/migration-admin-ui/src/App.tsx"
Normalize-AppTsx -Path $appPath

Write-Step "Complete."
