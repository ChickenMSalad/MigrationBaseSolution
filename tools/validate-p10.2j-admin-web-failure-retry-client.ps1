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
        [string[]] $Parts
    )

    $path = $repoRoot
    foreach ($part in $Parts) {
        $path = [System.IO.Path]::Combine($path, $part)
    }

    return $path
}

$requiredFiles = @(
    'docs/p10/P10.2J-Admin-Web-Failure-Retry-Client.md',
    'docs/operations/admin-web-failure-retry-client.md',
    'config-samples/p10-admin-web-failure-retry-client.sample.json',
    'src/Admin/Migration.Admin.Web/src/types/failureRetry.ts',
    'src/Admin/Migration.Admin.Web/src/api/failureRetryApi.ts',
    'src/Admin/Migration.Admin.Web/src/pages/FailureRetry.tsx'
)

foreach ($relativePath in $requiredFiles) {
    $parts = @($relativePath -split '/')
    $fullPath = Join-RepoPath -Parts $parts
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw ('Required P10.2J file is missing: {0}' -f $relativePath)
    }
}

$configPath = Join-RepoPath -Parts @('config-samples', 'p10-admin-web-failure-retry-client.sample.json')
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
foreach ($propertyName in @('canonicalAdminUiPath', 'featureSourcePath', 'pagePath', 'apiClientPath', 'typesPath', 'intendedRoute')) {
    if ($null -eq $config.PSObject.Properties[$propertyName]) {
        throw ('P10.2J config is missing property: {0}' -f $propertyName)
    }
}
if ($config.canonicalAdminUiPath -ne 'src/Admin/Migration.Admin.Web') {
    throw 'P10.2J canonicalAdminUiPath must remain src/Admin/Migration.Admin.Web.'
}
if ($config.featureSourcePath -ne 'apps/migration-admin-ui') {
    throw 'P10.2J featureSourcePath must remain apps/migration-admin-ui.'
}

$pagePath = Join-RepoPath -Parts @('src', 'Admin', 'Migration.Admin.Web', 'src', 'pages', 'FailureRetry.tsx')
$pageText = Get-Content -LiteralPath $pagePath -Raw
foreach ($term in @('FailureRetry', 'failureRetryApi', 'Runtime operations', 'Failed', 'Retryable')) {
    if ($pageText.IndexOf($term, [System.StringComparison]::Ordinal) -lt 0) {
        throw ('FailureRetry page is missing expected term: {0}' -f $term)
    }
}

$apiPath = Join-RepoPath -Parts @('src', 'Admin', 'Migration.Admin.Web', 'src', 'api', 'failureRetryApi.ts')
$apiText = Get-Content -LiteralPath $apiPath -Raw
foreach ($term in @('apiGet', '/api/runtime/dashboard/failures', 'FailureRetryResponse')) {
    if ($apiText.IndexOf($term, [System.StringComparison]::Ordinal) -lt 0) {
        throw ('Failure retry API client is missing expected term: {0}' -f $term)
    }
}

$typesPath = Join-RepoPath -Parts @('src', 'Admin', 'Migration.Admin.Web', 'src', 'types', 'failureRetry.ts')
$typesText = Get-Content -LiteralPath $typesPath -Raw
foreach ($term in @('FailureRetryWorkItem', 'FailureRetrySummary', 'FailureRetryResponse')) {
    if ($typesText.IndexOf($term, [System.StringComparison]::Ordinal) -lt 0) {
        throw ('Failure retry types file is missing expected term: {0}' -f $term)
    }
}

Write-Host 'P10.2J Admin Web failure retry client validation passed.'
