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

$requiredFiles = @(
    'docs\p10\P10.2L-Admin-Web-Credential-Vault-Client.md',
    'docs\operations\admin-web-credential-vault-client.md',
    'config-samples\p10-admin-web-credential-vault-client.sample.json',
    'src\Admin\Migration.Admin.Web\src\types\credentialVault.ts',
    'src\Admin\Migration.Admin.Web\src\api\credentialVaultApi.ts',
    'src\Admin\Migration.Admin.Web\src\pages\CredentialVault.tsx'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = [System.IO.Path]::Combine($repoRoot, $relativePath)
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw ('Required P10.2L file is missing: {0}' -f $relativePath)
    }
}

$configPath = [System.IO.Path]::Combine($repoRoot, 'config-samples\p10-admin-web-credential-vault-client.sample.json')
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
foreach ($propertyName in @('canonicalAdminUiPath', 'featureSourcePath', 'addedFiles', 'apiEndpoints')) {
    if ($null -eq $config.PSObject.Properties[$propertyName]) {
        throw ('P10.2L config is missing property: {0}' -f $propertyName)
    }
}
if ($config.canonicalAdminUiPath -ne 'src/Admin/Migration.Admin.Web') {
    throw 'P10.2L canonicalAdminUiPath must remain src/Admin/Migration.Admin.Web.'
}
if ($config.featureSourcePath -ne 'apps/migration-admin-ui') {
    throw 'P10.2L featureSourcePath must remain apps/migration-admin-ui.'
}

$apiPath = [System.IO.Path]::Combine($repoRoot, 'src\Admin\Migration.Admin.Web\src\api\credentialVaultApi.ts')
$apiText = Get-Content -LiteralPath $apiPath -Raw
foreach ($term in @('/api/operational/connectors/credentials/summary', '/api/operational/connectors/credentials/catalog', '/api/operational/connectors/credentials/validate', 'apiGet', 'apiPost')) {
    if ($apiText.IndexOf($term, [System.StringComparison]::Ordinal) -lt 0) {
        throw ('Credential vault API client is missing expected term: {0}' -f $term)
    }
}

$pagePath = [System.IO.Path]::Combine($repoRoot, 'src\Admin\Migration.Admin.Web\src\pages\CredentialVault.tsx')
$pageText = Get-Content -LiteralPath $pagePath -Raw
foreach ($term in @('CredentialVault', 'credentialVaultApi', 'Validate credential reference', 'Credential catalog')) {
    if ($pageText.IndexOf($term, [System.StringComparison]::Ordinal) -lt 0) {
        throw ('Credential vault page is missing expected term: {0}' -f $term)
    }
}

$forbiddenFragments = @(
    'apps/migration-admin-ui/src',
    '../../lib/adminApi'
)
foreach ($fragment in $forbiddenFragments) {
    if ($apiText.IndexOf($fragment, [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $pageText.IndexOf($fragment, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw ('P10.2L canonical files must not import feature-source app paths: {0}' -f $fragment)
    }
}

Write-Host 'P10.2L Admin Web credential vault client validation passed.'
