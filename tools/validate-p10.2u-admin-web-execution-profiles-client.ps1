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

    $fullPath = $repoRoot
    foreach ($part in $RelativePath.Split('/')) {
        if (-not [string]::IsNullOrWhiteSpace($part)) {
            $fullPath = [System.IO.Path]::Combine($fullPath, $part)
        }
    }
    return $fullPath
}

$checks = @(
    [pscustomobject]@{
        Path = 'src/Admin/Migration.Admin.Web/src/types/executionProfiles.ts'
        Terms = @('ConnectorExecutionProfileSummary', 'ConnectorExecutionProfileCatalogItem', 'ConnectorExecutionProfileValidationRequest')
    },
    [pscustomobject]@{
        Path = 'src/Admin/Migration.Admin.Web/src/api/executionProfilesApi.ts'
        Terms = @('/api/operational/connectors/execution-profiles/summary', '/api/operational/connectors/execution-profiles/catalog', '/api/operational/connectors/execution-profiles/validate')
    },
    [pscustomobject]@{
        Path = 'src/Admin/Migration.Admin.Web/src/pages/ExecutionProfiles.tsx'
        Terms = @('Execution Profiles', 'executionProfilesApi', 'validateProfile')
    },
    [pscustomobject]@{
        Path = 'docs/p10/P10.2U-Admin-Web-Execution-Profiles-Client.md'
        Terms = @('src/Admin/Migration.Admin.Web', 'apps/migration-admin-ui', 'executionProfiles')
    },
    [pscustomobject]@{
        Path = 'docs/operations/admin-web-execution-profiles-client.md'
        Terms = @('feature-source', 'src/Admin/Migration.Admin.Web', '/api/operational/connectors/execution-profiles/summary')
    },
    [pscustomobject]@{
        Path = 'config-samples/p10-admin-web-execution-profiles-client.sample.json'
        Terms = @('P10.2U', 'executionProfiles', 'src/Admin/Migration.Admin.Web')
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

Write-Host 'P10.2U Admin Web execution profiles client validation passed.'
