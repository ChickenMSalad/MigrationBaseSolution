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
        [ValidateNotNullOrEmpty()]
        [string] $RelativePath
    )

    $normalized = $RelativePath.Replace('/', [System.IO.Path]::DirectorySeparatorChar)
    return [System.IO.Path]::Combine($repoRoot, $normalized)
}

$checks = @(
    [pscustomobject]@{
        Path = 'docs/p10/P10.2Q-Admin-Web-Notification-Routing-Client.md'
        Terms = @('src/Admin/Migration.Admin.Web', 'apps/migration-admin-ui', 'feature-source')
    },
    [pscustomobject]@{
        Path = 'docs/operations/admin-web-notification-routing-client.md'
        Terms = @('src/Admin/Migration.Admin.Web', 'apps/migration-admin-ui', 'feature-source')
    },
    [pscustomobject]@{
        Path = 'config-samples/p10-admin-web-notification-routing-client.sample.json'
        Terms = @('canonicalAdminUiPath', 'featureSourcePath', '/api/operational/notifications/summary')
    },
    [pscustomobject]@{
        Path = 'src/Admin/Migration.Admin.Web/src/types/notificationRouting.ts'
        Terms = @('NotificationSummary', 'NotificationRoute', 'AlertPreviewItem')
    },
    [pscustomobject]@{
        Path = 'src/Admin/Migration.Admin.Web/src/api/notificationRoutingApi.ts'
        Terms = @('notificationRoutingApi', '/api/operational/notifications/routes', 'apiGet')
    },
    [pscustomobject]@{
        Path = 'src/Admin/Migration.Admin.Web/src/pages/NotificationRouting.tsx'
        Terms = @('Notification Routing', 'notificationRoutingApi', 'Alert preview')
    }
)

foreach ($check in $checks) {
    $pathProperty = $check.PSObject.Properties['Path']
    $termsProperty = $check.PSObject.Properties['Terms']

    if ($null -eq $pathProperty -or $null -eq $termsProperty) {
        throw 'Validator check entry is malformed.'
    }

    $relativePath = [string]$pathProperty.Value
    if ([string]::IsNullOrWhiteSpace($relativePath)) {
        throw 'Validator check entry has an empty Path.'
    }

    $fullPath = Join-RepoPath -RelativePath $relativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw ('Required file is missing: {0}' -f $relativePath)
    }

    $text = Get-Content -LiteralPath $fullPath -Raw
    foreach ($term in @($termsProperty.Value)) {
        $termText = [string]$term
        if ([string]::IsNullOrWhiteSpace($termText)) {
            throw ('Validator check entry has an empty expected term for file: {0}' -f $relativePath)
        }

        if ($text.IndexOf($termText, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
            throw ('File {0} is missing expected term: {1}' -f $relativePath, $termText)
        }
    }
}

Write-Host 'P10.2Q Admin Web notification routing client validation passed.'
