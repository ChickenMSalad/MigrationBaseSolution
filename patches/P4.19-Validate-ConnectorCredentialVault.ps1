[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

$requiredFiles = @(
    'src/Core/Migration.Admin.Api/Endpoints/Operational/Credentials/OperationalConnectorCredentialVaultEndpointExtensions.cs',
    'src/Core/Migration.Admin.Api/Registration/AdminApiConnectorCredentialVaultRegistrationExtensions.cs',
    'src/Core/Migration.Application/Operational/Credentials/ConnectorCredentialReference.cs',
    'src/Core/Migration.Application/Operational/Credentials/IConnectorCredentialReferenceStore.cs',
    'src/Core/Migration.Infrastructure.Sql/Credentials/SqlConnectorCredentialReferenceStore.cs',
    'apps/migration-admin-ui/src/features/credentials/credentialVaultTypes.ts',
    'apps/migration-admin-ui/src/features/credentials/credentialVaultApi.ts',
    'apps/migration-admin-ui/src/features/credentials/CredentialVaultWorkspace.tsx',
    'docs/operations/P4.19-connector-credential-vault-secret-references.md'
)

foreach ($relativePath in $requiredFiles) {
    $path = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required file missing: $relativePath"
    }
}

$programPath = Join-Path $repoRoot 'src/Core/Migration.Admin.Api/Program.cs'
$programText = Get-Content -LiteralPath $programPath -Raw
foreach ($expectedText in @('MapOperationalConnectorCredentialVaultEndpoints', 'AddAdminApiConnectorCredentialVault')) {
    if (-not $programText.Contains($expectedText)) {
        throw ("Expected Program.cs text missing: {0}" -f $expectedText)
    }
}

$appPath = Join-Path $repoRoot 'apps/migration-admin-ui/src/App.tsx'
$appText = Get-Content -LiteralPath $appPath -Raw
if (-not $appText.Contains('CredentialVaultWorkspace')) {
    throw 'Expected CredentialVaultWorkspace reference missing from App.tsx'
}

Write-Host '[P4.19] Validation passed.'
