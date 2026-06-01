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

    $path = $repoRoot
    foreach ($part in ($RelativePath -split '[\\/]')) {
        if (-not [string]::IsNullOrWhiteSpace($part)) {
            $path = [System.IO.Path]::Combine($path, $part)
        }
    }
    return $path
}

$requiredFiles = @(
    'docs/p10/P10.2O-Admin-Web-Connector-Configuration-Route.md',
    'docs/operations/admin-web-connector-configuration-route.md',
    'config-samples/p10-admin-web-connector-configuration-route.sample.json',
    'tools/runtime/Apply-P102AdminWebConnectorConfigurationRoute.ps1',
    'src/Admin/Migration.Admin.Web/src/pages/ConnectorConfiguration.tsx',
    'src/Admin/Migration.Admin.Web/src/App.tsx',
    'src/Admin/Migration.Admin.Web/src/components/Layout.tsx'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-RepoPath -RelativePath $relativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw ('Required P10.2O file is missing: {0}' -f $relativePath)
    }
}

$scriptsToParse = @(
    'tools/runtime/Apply-P102AdminWebConnectorConfigurationRoute.ps1'
)

foreach ($relativeScript in $scriptsToParse) {
    $scriptPath = Join-RepoPath -RelativePath $relativeScript
    $parseErrors = $null
    $scriptText = Get-Content -LiteralPath $scriptPath -Raw
    [System.Management.Automation.PSParser]::Tokenize($scriptText, [ref]$parseErrors) | Out-Null
    if ($null -ne $parseErrors -and @($parseErrors).Count -gt 0) {
        $message = (@($parseErrors) | ForEach-Object { $_.Message }) -join '; '
        throw ('PowerShell parser errors in {0}: {1}' -f $relativeScript, $message)
    }
}

$checks = @(
    [pscustomobject]@{
        Path = 'src/Admin/Migration.Admin.Web/src/App.tsx'
        Terms = @(
            'ConnectorConfiguration',
            'path="/connector-configuration"'
        )
    },
    [pscustomobject]@{
        Path = 'src/Admin/Migration.Admin.Web/src/components/Layout.tsx'
        Terms = @(
            '/connector-configuration',
            'Connector Config'
        )
    },
    [pscustomobject]@{
        Path = 'src/Admin/Migration.Admin.Web/src/pages/ConnectorConfiguration.tsx'
        Terms = @(
            'Connector Configuration',
            'validateDraft'
        )
    },
    [pscustomobject]@{
        Path = 'docs/p10/P10.2O-Admin-Web-Connector-Configuration-Route.md'
        Terms = @(
            'src/Admin/Migration.Admin.Web',
            'apps/migration-admin-ui',
            '/connector-configuration'
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

$configPath = Join-RepoPath -RelativePath 'config-samples/p10-admin-web-connector-configuration-route.sample.json'
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
foreach ($propertyName in @('canonicalAdminUiPath', 'featureSourcePath', 'route', 'page', 'navigationLabel')) {
    if ($null -eq $config.PSObject.Properties[$propertyName]) {
        throw ('P10.2O config is missing property: {0}' -f $propertyName)
    }
}

if ($config.canonicalAdminUiPath -ne 'src/Admin/Migration.Admin.Web') {
    throw 'P10.2O canonicalAdminUiPath must remain src/Admin/Migration.Admin.Web.'
}
if ($config.featureSourcePath -ne 'apps/migration-admin-ui') {
    throw 'P10.2O featureSourcePath must remain apps/migration-admin-ui.'
}

Write-Host 'P10.2O Admin Web connector configuration route validation passed.'
