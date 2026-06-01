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
        Path = 'docs/p10/P10.2S-Admin-Web-Command-Center-Client.md'
        Terms = @('src/Admin/Migration.Admin.Web', 'apps/migration-admin-ui', 'Command Center')
    },
    [pscustomobject]@{
        Path = 'docs/operations/admin-web-command-center-client.md'
        Terms = @('feature-source', 'src/Admin/Migration.Admin.Web', 'Command Center')
    },
    [pscustomobject]@{
        Path = 'config-samples/p10-admin-web-command-center-client.sample.json'
        Terms = @('canonicalAdminUiPath', 'featureSourcePath', 'commandCenter')
    },
    [pscustomobject]@{
        Path = 'src/Admin/Migration.Admin.Web/src/types/commandCenter.ts'
        Terms = @('CommandCenterSummary', 'CommandCenterEvent', 'CommandCenterHealthResponse')
    },
    [pscustomobject]@{
        Path = 'src/Admin/Migration.Admin.Web/src/api/commandCenterApi.ts'
        Terms = @('commandCenterApi', '/api/operational/command-center/summary', '/api/operational/command-center/health')
    },
    [pscustomobject]@{
        Path = 'src/Admin/Migration.Admin.Web/src/pages/CommandCenter.tsx'
        Terms = @('Command Center', 'commandCenterApi', 'loadCommandCenter')
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

Write-Host 'P10.2S Admin Web command center client validation passed.'
