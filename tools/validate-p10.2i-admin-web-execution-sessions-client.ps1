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

    return [System.IO.Path]::Combine($repoRoot, $RelativePath.Replace('/', [System.IO.Path]::DirectorySeparatorChar))
}

$requiredFiles = @(
    'docs/p10/P10.2I-Admin-Web-Execution-Sessions-Client.md',
    'docs/operations/admin-web-execution-sessions-client.md',
    'config-samples/p10-admin-web-execution-sessions-client.sample.json',
    'src/Admin/Migration.Admin.Web/src/types/executionSessions.ts',
    'src/Admin/Migration.Admin.Web/src/api/executionSessionsApi.ts',
    'src/Admin/Migration.Admin.Web/src/pages/ExecutionSessions.tsx'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-RepoPath -RelativePath $relativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw ('Required P10.2I file is missing: {0}' -f $relativePath)
    }
}

$configPath = Join-RepoPath -RelativePath 'config-samples/p10-admin-web-execution-sessions-client.sample.json'
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
foreach ($propertyName in @('canonicalAdminUiPath', 'featureSourcePath', 'featureFamily', 'addedFiles', 'routeWiringDeferred')) {
    if ($null -eq $config.PSObject.Properties[$propertyName]) {
        throw ('P10.2I config is missing property: {0}' -f $propertyName)
    }
}
if ($config.canonicalAdminUiPath -ne 'src/Admin/Migration.Admin.Web') {
    throw 'P10.2I canonicalAdminUiPath must remain src/Admin/Migration.Admin.Web.'
}
if ($config.featureSourcePath -ne 'apps/migration-admin-ui') {
    throw 'P10.2I featureSourcePath must remain apps/migration-admin-ui.'
}
if ($config.featureFamily -ne 'executionSessions') {
    throw 'P10.2I featureFamily must remain executionSessions.'
}
if ($config.routeWiringDeferred -ne $true) {
    throw 'P10.2I routeWiringDeferred must be true.'
}

$typePath = Join-RepoPath -RelativePath 'src/Admin/Migration.Admin.Web/src/types/executionSessions.ts'
$typeText = Get-Content -LiteralPath $typePath -Raw
foreach ($term in @('ExecutionSessionRecord', 'CreateExecutionSessionRequest', 'RecentExecutionSessionsResponse')) {
    if ($typeText.IndexOf($term, [System.StringComparison]::Ordinal) -lt 0) {
        throw ('Execution session type file is missing expected term: {0}' -f $term)
    }
}

$apiPath = Join-RepoPath -RelativePath 'src/Admin/Migration.Admin.Web/src/api/executionSessionsApi.ts'
$apiText = Get-Content -LiteralPath $apiPath -Raw
foreach ($term in @('/api/operational/execution-sessions/recent', '/api/operational/execution-sessions', '/api/operational/events/snapshot', 'executionSessionsApi')) {
    if ($apiText.IndexOf($term, [System.StringComparison]::Ordinal) -lt 0) {
        throw ('Execution session API client is missing expected term: {0}' -f $term)
    }
}

$pagePath = Join-RepoPath -RelativePath 'src/Admin/Migration.Admin.Web/src/pages/ExecutionSessions.tsx'
$pageText = Get-Content -LiteralPath $pagePath -Raw
foreach ($term in @('ExecutionSessions', 'executionSessionsApi.recent', 'Runtime operations', 'Execution sessions')) {
    if ($pageText.IndexOf($term, [System.StringComparison]::Ordinal) -lt 0) {
        throw ('Execution sessions page is missing expected term: {0}' -f $term)
    }
}

Write-Host 'P10.2I Admin Web execution sessions client validation passed.'
