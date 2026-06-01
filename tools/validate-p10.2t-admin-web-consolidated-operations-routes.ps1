[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$toolRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($toolRoot)) {
    if (-not [string]::IsNullOrWhiteSpace($PSCommandPath)) {
        $toolRoot = Split-Path -Parent $PSCommandPath
    }
}
if ([string]::IsNullOrWhiteSpace($toolRoot)) {
    throw 'Unable to resolve validator root.'
}

$repoRoot = Split-Path -Parent $toolRoot

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

$requiredFiles = @(
    'docs/p10/P10.2T-Admin-Web-Consolidated-Operations-Routes.md',
    'docs/operations/admin-web-consolidated-operations-routes.md',
    'config-samples/p10-admin-web-consolidated-operations-routes.sample.json',
    'tools/runtime/Apply-P102AdminWebConsolidatedOperationsRoutes.ps1'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-RepoPath -RelativePath $relativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw ('Required P10.2T file is missing: {0}' -f $relativePath)
    }
}

$scriptsToParse = @(
    'tools/runtime/Apply-P102AdminWebConsolidatedOperationsRoutes.ps1'
)

foreach ($relativeScript in $scriptsToParse) {
    $scriptPath = Join-RepoPath -RelativePath $relativeScript
    if (-not (Test-Path -LiteralPath $scriptPath -PathType Leaf)) {
        throw ('Script file is missing: {0}' -f $relativeScript)
    }

    $parseErrors = $null
    $scriptText = Get-Content -LiteralPath $scriptPath -Raw
    [System.Management.Automation.PSParser]::Tokenize($scriptText, [ref]$parseErrors) | Out-Null

    if ($null -ne $parseErrors -and @($parseErrors).Count -gt 0) {
        $message = (@($parseErrors) | ForEach-Object { $_.Message }) -join '; '
        throw ('PowerShell parser errors in {0}: {1}' -f $relativeScript, $message)
    }
}

$configPath = Join-RepoPath -RelativePath 'config-samples/p10-admin-web-consolidated-operations-routes.sample.json'
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
foreach ($propertyName in @('canonicalAdminUiPath', 'featureSourcePath', 'routes')) {
    if ($null -eq $config.PSObject.Properties[$propertyName]) {
        throw ('P10.2T config is missing property: {0}' -f $propertyName)
    }
}

if ($config.canonicalAdminUiPath -ne 'src/Admin/Migration.Admin.Web') {
    throw 'P10.2T canonicalAdminUiPath must remain src/Admin/Migration.Admin.Web.'
}
if ($config.featureSourcePath -ne 'apps/migration-admin-ui') {
    throw 'P10.2T featureSourcePath must remain apps/migration-admin-ui.'
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

$routeChecks = @(
    [pscustomobject]@{ Page = 'RuntimeDashboard'; Route = 'runtime-dashboard'; Nav = '/runtime-dashboard' },
    [pscustomobject]@{ Page = 'RuntimeRunDetail'; Route = 'runtime-runs/:runId'; Nav = '' },
    [pscustomobject]@{ Page = 'ExecutionSessions'; Route = 'execution-sessions'; Nav = '/execution-sessions' },
    [pscustomobject]@{ Page = 'FailureRetry'; Route = 'failure-retry'; Nav = '/failure-retry' },
    [pscustomobject]@{ Page = 'CredentialVault'; Route = 'credential-vault'; Nav = '/credential-vault' },
    [pscustomobject]@{ Page = 'ConnectorConfiguration'; Route = 'connector-configuration'; Nav = '/connector-configuration' },
    [pscustomobject]@{ Page = 'ExecutionWorkerTelemetry'; Route = 'execution-worker-telemetry'; Nav = '/execution-worker-telemetry' },
    [pscustomobject]@{ Page = 'NotificationRouting'; Route = 'notification-routing'; Nav = '/notification-routing' },
    [pscustomobject]@{ Page = 'AuditTrail'; Route = 'audit-trail'; Nav = '/audit-trail' },
    [pscustomobject]@{ Page = 'CommandCenter'; Route = 'command-center'; Nav = '/command-center' }
)

foreach ($check in $routeChecks) {
    $pagePath = Join-RepoPath -RelativePath ('src/Admin/Migration.Admin.Web/src/pages/{0}.tsx' -f $check.Page)
    if (Test-Path -LiteralPath $pagePath -PathType Leaf) {
        if ($appText.IndexOf([string]$check.Page, [System.StringComparison]::Ordinal) -lt 0) {
            throw ('Admin Web App.tsx is missing page import or usage: {0}' -f $check.Page)
        }
        if ($appText.IndexOf([string]$check.Route, [System.StringComparison]::Ordinal) -lt 0) {
            throw ('Admin Web App.tsx is missing route: {0}' -f $check.Route)
        }
        $navRoute = [string]$check.Nav
        if (-not [string]::IsNullOrWhiteSpace($navRoute)) {
            if ($layoutText.IndexOf($navRoute, [System.StringComparison]::Ordinal) -lt 0) {
                throw ('Admin Web Layout.tsx is missing nav route: {0}' -f $navRoute)
            }
        }
    }
}

foreach ($docPath in @('docs/p10/P10.2T-Admin-Web-Consolidated-Operations-Routes.md', 'docs/operations/admin-web-consolidated-operations-routes.md')) {
    $fullDocPath = Join-RepoPath -RelativePath $docPath
    $docText = Get-Content -LiteralPath $fullDocPath -Raw
    foreach ($term in @('src/Admin/Migration.Admin.Web', 'apps/migration-admin-ui')) {
        if ($docText.IndexOf($term, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
            throw ('Documentation file is missing consolidation term {0}: {1}' -f $term, $docPath)
        }
    }
}

Write-Host 'P10.2T Admin Web consolidated operations route validation passed.'
