[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [switch]$Apply
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host ("[P4.27-REPAIR] {0}" -f $Message)
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

function Repair-AppTsx {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw ("File not found: {0}" -f $Path)
    }

    $content = Get-Content -LiteralPath $Path -Raw

    $directImportPatterns = @(
        "import\s+\{\s*AuditTrailWorkspace\s*\}\s+from\s+'\.\/features\/audit\/AuditTrailWorkspace';\r?\n?",
        "import\s+\{\s*CapacityForecastWorkspace\s*\}\s+from\s+'\.\/features\/capacity\/CapacityForecastWorkspace';\r?\n?",
        "import\s+\{\s*ConnectorConfigurationWorkspace\s*\}\s+from\s+'\.\/features\/connectors\/ConnectorConfigurationWorkspace';\r?\n?",
        "import\s+\{\s*CostAnalyticsWorkspace\s*\}\s+from\s+'\.\/features\/cost\/CostAnalyticsWorkspace';\r?\n?",
        "import\s+\{\s*CredentialVaultWorkspace\s*\}\s+from\s+'\.\/features\/credentials\/CredentialVaultWorkspace';\r?\n?",
        "import\s+\{\s*ExecutionProfileWorkspace\s*\}\s+from\s+'\.\/features\/executionProfiles\/ExecutionProfileWorkspace';\r?\n?",
        "import\s+\{\s*NotificationRoutingWorkspace\s*\}\s+from\s+'\.\/features\/notifications\/NotificationRoutingWorkspace';\r?\n?",
        "import\s+\{\s*SlaSloPolicyWorkspace\s*\}\s+from\s+'\.\/features\/slaSlo\/SlaSloPolicyWorkspace';\r?\n?",
        "import\s+\{\s*WorkerTelemetryWorkspace\s*\}\s+from\s+'\.\/features\/workers\/WorkerTelemetryWorkspace';\r?\n?",
        "import\s+\{\s*OperationalRuntimeDashboard\s*\}\s+from\s+'\.\/components\/OperationalRuntimeDashboard';\r?\n?",
        "import\s+\{\s*ManifestImportPanel\s*\}\s+from\s+'\.\/components\/ManifestImportPanel';\r?\n?",
        "import\s+\{\s*RunLaunchPanel\s*\}\s+from\s+'\.\/components\/RunLaunchPanel';\r?\n?",
        "import\s+\{\s*FailureRetryWorkspace\s*\}\s+from\s+'\.\/components\/FailureRetryWorkspace';\r?\n?"
    )

    foreach ($pattern in $directImportPatterns) {
        $content = [regex]::Replace($content, $pattern, "")
    }

    $componentPatterns = @(
        "\s*<AuditTrailWorkspace\s*\/>\r?\n?",
        "\s*<CapacityForecastWorkspace\s*\/>\r?\n?",
        "\s*<ConnectorConfigurationWorkspace\s*\/>\r?\n?",
        "\s*<CostAnalyticsWorkspace\s*\/>\r?\n?",
        "\s*<CredentialVaultWorkspace\s*\/>\r?\n?",
        "\s*<ExecutionProfileWorkspace\s*\/>\r?\n?",
        "\s*<NotificationRoutingWorkspace\s*\/>\r?\n?",
        "\s*<SlaSloPolicyWorkspace\s*\/>\r?\n?",
        "\s*<WorkerTelemetryWorkspace\s*\/>\r?\n?",
        "\s*<OperationalRuntimeDashboard\s*\/>\r?\n?",
        "\s*<ManifestImportPanel\s*\/>\r?\n?",
        "\s*<RunLaunchPanel\s*\/>\r?\n?",
        "\s*<FailureRetryWorkspace\s*\/>\r?\n?"
    )

    foreach ($pattern in $componentPatterns) {
        $content = [regex]::Replace($content, $pattern, [Environment]::NewLine)
    }

    $compositionImport = "import { OperationalWorkspaceComposition } from './features/operational/OperationalWorkspaceComposition';"
    $content = [regex]::Replace(
        $content,
        "import\s+\{\s*OperationalWorkspaceComposition\s*\}\s+from\s+'\.\/features\/operational\/OperationalWorkspaceComposition';\r?\n?",
        "")

    $lines = @($content -split "\r?\n")
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

    $content = ($lines -join [Environment]::NewLine)

    $content = [regex]::Replace($content, "\s*<OperationalWorkspaceComposition\s*\/>\r?\n?", [Environment]::NewLine)

    if (-not $content.Contains("</main>")) {
        throw ("Anchor not found in {0}: </main>" -f $Path)
    }

    $content = $content.Replace("</main>", "      <OperationalWorkspaceComposition />" + [Environment]::NewLine + "    </main>")

    if (-not $Apply) {
        Write-Step "WOULD repair App.tsx composition"
        return
    }

    Set-Content -LiteralPath $Path -Value $content -Encoding UTF8
    Write-Step "Repaired App.tsx composition"
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$payloadRoot = Join-Path $repoRoot "payload"

Copy-PayloadFile "apps/migration-admin-ui/src/features/operational/OperationalWorkspaceComposition.tsx"

$appPath = Join-Path $repoRoot "apps/migration-admin-ui/src/App.tsx"
Repair-AppTsx -Path $appPath

Write-Step "Complete."
