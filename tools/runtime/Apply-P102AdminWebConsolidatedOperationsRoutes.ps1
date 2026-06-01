[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    if (-not [string]::IsNullOrWhiteSpace($PSCommandPath)) {
        $scriptRoot = Split-Path -Parent $PSCommandPath
    }
}
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    throw 'Unable to resolve script root.'
}

$repoRoot = Split-Path -Parent (Split-Path -Parent $scriptRoot)

function Join-RepoPath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $RelativePath
    )

    $parts = $RelativePath -split '/'
    $fullPath = $repoRoot
    foreach ($part in $parts) {
        if (-not [string]::IsNullOrWhiteSpace($part)) {
            $fullPath = [System.IO.Path]::Combine($fullPath, $part)
        }
    }
    return $fullPath
}

function Add-TextBeforeMarker {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Text,

        [Parameter(Mandatory = $true)]
        [string] $InsertText,

        [Parameter(Mandatory = $true)]
        [string] $Marker,

        [Parameter(Mandatory = $true)]
        [string] $Description
    )

    if ($Text.IndexOf($InsertText.Trim(), [System.StringComparison]::Ordinal) -ge 0) {
        return $Text
    }

    $markerIndex = $Text.IndexOf($Marker, [System.StringComparison]::Ordinal)
    if ($markerIndex -lt 0) {
        throw ('Unable to find insertion marker for {0}: {1}' -f $Description, $Marker)
    }

    return $Text.Insert($markerIndex, ($InsertText.TrimEnd() + [Environment]::NewLine))
}

function Add-RouteBeforeFallback {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Text,

        [Parameter(Mandatory = $true)]
        [string] $RouteText
    )

    if ($Text.IndexOf($RouteText.Trim(), [System.StringComparison]::Ordinal) -ge 0) {
        return $Text
    }

    $fallbackMarkers = @(
        '  <Route path="*" element={<Navigate to="/" replace />} />',
        '<Route path="*" element={<Navigate to="/" replace />} />'
    )

    foreach ($marker in $fallbackMarkers) {
        $index = $Text.IndexOf($marker, [System.StringComparison]::Ordinal)
        if ($index -ge 0) {
            return $Text.Insert($index, ($RouteText.TrimEnd() + [Environment]::NewLine))
        }
    }

    throw 'Unable to find fallback route anchor in Admin Web App.tsx.'
}

function Add-NavBeforeArtifacts {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Text,

        [Parameter(Mandatory = $true)]
        [string] $NavText
    )

    if ($Text.IndexOf($NavText.Trim(), [System.StringComparison]::Ordinal) -ge 0) {
        return $Text
    }

    $markers = @(
        '  { to: "/artifacts", label: "Artifacts", icon: Amphora },',
        '{ to: "/artifacts", label: "Artifacts", icon: Amphora },'
    )

    foreach ($marker in $markers) {
        $index = $Text.IndexOf($marker, [System.StringComparison]::Ordinal)
        if ($index -ge 0) {
            return $Text.Insert($index, ($NavText.TrimEnd() + [Environment]::NewLine))
        }
    }

    throw 'Unable to find Artifacts nav anchor in Admin Web Layout.tsx.'
}

$appPath = Join-RepoPath -RelativePath 'src/Admin/Migration.Admin.Web/src/App.tsx'
$layoutPath = Join-RepoPath -RelativePath 'src/Admin/Migration.Admin.Web/src/components/Layout.tsx'

if (-not (Test-Path -LiteralPath $appPath -PathType Leaf)) {
    throw 'Admin Web App.tsx is missing.'
}
if (-not (Test-Path -LiteralPath $layoutPath -PathType Leaf)) {
    throw 'Admin Web Layout.tsx is missing.'
}

$appText = Get-Content -LiteralPath $appPath -Raw
$layoutText = Get-Content -LiteralPath $layoutPath -Raw

$imports = @(
    [pscustomobject]@{ Page = 'RuntimeDashboard'; Text = 'import { RuntimeDashboard } from "./pages/RuntimeDashboard";' },
    [pscustomobject]@{ Page = 'RuntimeRunDetail'; Text = 'import { RuntimeRunDetail } from "./pages/RuntimeRunDetail";' },
    [pscustomobject]@{ Page = 'ExecutionSessions'; Text = 'import { ExecutionSessions } from "./pages/ExecutionSessions";' },
    [pscustomobject]@{ Page = 'FailureRetry'; Text = 'import { FailureRetry } from "./pages/FailureRetry";' },
    [pscustomobject]@{ Page = 'CredentialVault'; Text = 'import { CredentialVault } from "./pages/CredentialVault";' },
    [pscustomobject]@{ Page = 'ConnectorConfiguration'; Text = 'import { ConnectorConfiguration } from "./pages/ConnectorConfiguration";' },
    [pscustomobject]@{ Page = 'ExecutionWorkerTelemetry'; Text = 'import { ExecutionWorkerTelemetry } from "./pages/ExecutionWorkerTelemetry";' },
    [pscustomobject]@{ Page = 'NotificationRouting'; Text = 'import { NotificationRouting } from "./pages/NotificationRouting";' },
    [pscustomobject]@{ Page = 'AuditTrail'; Text = 'import { AuditTrail } from "./pages/AuditTrail";' },
    [pscustomobject]@{ Page = 'CommandCenter'; Text = 'import { CommandCenter } from "./pages/CommandCenter";' }
)

foreach ($import in $imports) {
    $pagePath = Join-RepoPath -RelativePath ('src/Admin/Migration.Admin.Web/src/pages/{0}.tsx' -f $import.Page)
    if (Test-Path -LiteralPath $pagePath -PathType Leaf) {
        $appText = Add-TextBeforeMarker -Text $appText -InsertText $import.Text -Marker 'export default function App()' -Description ('import for {0}' -f $import.Page)
    }
}

$routes = @(
    [pscustomobject]@{ Page = 'RuntimeDashboard'; Text = '  <Route path="runtime-dashboard" element={<RuntimeDashboard />} />' },
    [pscustomobject]@{ Page = 'RuntimeRunDetail'; Text = '  <Route path="runtime-runs/:runId" element={<RuntimeRunDetail />} />' },
    [pscustomobject]@{ Page = 'ExecutionSessions'; Text = '  <Route path="execution-sessions" element={<ExecutionSessions />} />' },
    [pscustomobject]@{ Page = 'FailureRetry'; Text = '  <Route path="failure-retry" element={<FailureRetry />} />' },
    [pscustomobject]@{ Page = 'CredentialVault'; Text = '  <Route path="credential-vault" element={<CredentialVault />} />' },
    [pscustomobject]@{ Page = 'ConnectorConfiguration'; Text = '  <Route path="connector-configuration" element={<ConnectorConfiguration />} />' },
    [pscustomobject]@{ Page = 'ExecutionWorkerTelemetry'; Text = '  <Route path="execution-worker-telemetry" element={<ExecutionWorkerTelemetry />} />' },
    [pscustomobject]@{ Page = 'NotificationRouting'; Text = '  <Route path="notification-routing" element={<NotificationRouting />} />' },
    [pscustomobject]@{ Page = 'AuditTrail'; Text = '  <Route path="audit-trail" element={<AuditTrail />} />' },
    [pscustomobject]@{ Page = 'CommandCenter'; Text = '  <Route path="command-center" element={<CommandCenter />} />' }
)

foreach ($route in $routes) {
    $pagePath = Join-RepoPath -RelativePath ('src/Admin/Migration.Admin.Web/src/pages/{0}.tsx' -f $route.Page)
    if (Test-Path -LiteralPath $pagePath -PathType Leaf) {
        $appText = Add-RouteBeforeFallback -Text $appText -RouteText $route.Text
    }
}

$navItems = @(
    [pscustomobject]@{ Page = 'RuntimeDashboard'; Text = '  { to: "/runtime-dashboard", label: "Runtime Dashboard", icon: Activity },' },
    [pscustomobject]@{ Page = 'ExecutionSessions'; Text = '  { to: "/execution-sessions", label: "Execution Sessions", icon: Activity },' },
    [pscustomobject]@{ Page = 'FailureRetry'; Text = '  { to: "/failure-retry", label: "Failure Retry", icon: Activity },' },
    [pscustomobject]@{ Page = 'CredentialVault'; Text = '  { to: "/credential-vault", label: "Credential Vault", icon: KeyRound },' },
    [pscustomobject]@{ Page = 'ConnectorConfiguration'; Text = '  { to: "/connector-configuration", label: "Connector Configuration", icon: PlugZap },' },
    [pscustomobject]@{ Page = 'ExecutionWorkerTelemetry'; Text = '  { to: "/execution-worker-telemetry", label: "Worker Telemetry", icon: Activity },' },
    [pscustomobject]@{ Page = 'NotificationRouting'; Text = '  { to: "/notification-routing", label: "Notification Routing", icon: GitBranch },' },
    [pscustomobject]@{ Page = 'AuditTrail'; Text = '  { to: "/audit-trail", label: "Audit Trail", icon: Activity },' },
    [pscustomobject]@{ Page = 'CommandCenter'; Text = '  { to: "/command-center", label: "Command Center", icon: Boxes },' }
)

foreach ($navItem in $navItems) {
    $pagePath = Join-RepoPath -RelativePath ('src/Admin/Migration.Admin.Web/src/pages/{0}.tsx' -f $navItem.Page)
    if (Test-Path -LiteralPath $pagePath -PathType Leaf) {
        $layoutText = Add-NavBeforeArtifacts -Text $layoutText -NavText $navItem.Text
    }
}

Set-Content -LiteralPath $appPath -Value $appText -Encoding UTF8
Set-Content -LiteralPath $layoutPath -Value $layoutText -Encoding UTF8

Write-Host 'Admin Web consolidated operations routes applied.'
