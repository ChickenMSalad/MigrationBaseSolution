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

    $current = $repoRoot
    foreach ($part in $RelativePath.Split('/')) {
        if (-not [string]::IsNullOrWhiteSpace($part)) {
            $current = [System.IO.Path]::Combine($current, $part)
        }
    }
    return $current
}

$requiredFiles = @(
    'docs/p10/P10.2N-Admin-Web-Connector-Configuration-Client.md',
    'docs/operations/admin-web-connector-configuration-client.md',
    'config-samples/p10-admin-web-connector-configuration-client.sample.json',
    'src/Admin/Migration.Admin.Web/src/types/connectorConfiguration.ts',
    'src/Admin/Migration.Admin.Web/src/api/connectorConfigurationApi.ts',
    'src/Admin/Migration.Admin.Web/src/pages/ConnectorConfiguration.tsx'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-RepoPath -RelativePath $relativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw ('Required P10.2N file is missing: {0}' -f $relativePath)
    }
}

$checks = @(
    [pscustomobject]@{
        Path = 'src/Admin/Migration.Admin.Web/src/types/connectorConfiguration.ts'
        Terms = @(
            'ConnectorConfigurationSummary',
            'ConnectorConfigurationCatalogItem',
            'ConnectorConfigurationValidationRequest'
        )
    },
    [pscustomobject]@{
        Path = 'src/Admin/Migration.Admin.Web/src/api/connectorConfigurationApi.ts'
        Terms = @(
            '/api/operational/connectors/configuration/summary',
            '/api/operational/connectors/configuration/catalog',
            '/api/operational/connectors/configuration/validate'
        )
    },
    [pscustomobject]@{
        Path = 'src/Admin/Migration.Admin.Web/src/pages/ConnectorConfiguration.tsx'
        Terms = @(
            'Connector Configuration',
            'connectorConfigurationApi',
            'validateDraft'
        )
    },
    [pscustomobject]@{
        Path = 'docs/p10/P10.2N-Admin-Web-Connector-Configuration-Client.md'
        Terms = @(
            'src/Admin/Migration.Admin.Web',
            'apps/migration-admin-ui'
        )
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

$configPath = Join-RepoPath -RelativePath 'config-samples/p10-admin-web-connector-configuration-client.sample.json'
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
foreach ($propertyName in @('phase', 'canonicalAdminUiPath', 'featureSourcePath', 'apiEndpoints')) {
    if ($null -eq $config.PSObject.Properties[$propertyName]) {
        throw ('Sample config is missing property: {0}' -f $propertyName)
    }
}
if ($config.canonicalAdminUiPath -ne 'src/Admin/Migration.Admin.Web') {
    throw 'Sample config canonicalAdminUiPath must remain src/Admin/Migration.Admin.Web.'
}
if ($config.featureSourcePath -ne 'apps/migration-admin-ui') {
    throw 'Sample config featureSourcePath must remain apps/migration-admin-ui.'
}

Write-Host 'P10.2N Admin Web connector configuration client validation passed.'
