Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    $current = Split-Path -Parent $PSCommandPath
    while ($null -ne $current -and $current.Length -gt 0) {
        $candidate = [System.IO.Path]::Combine($current, 'src', 'Admin', 'Migration.Admin.Web', 'src', 'App.tsx')
        if (Test-Path -Path $candidate -PathType Leaf) {
            return $current
        }
        $parent = Split-Path -Parent $current
        if ($parent -eq $current) {
            break
        }
        $current = $parent
    }
    throw 'Unable to locate repository root from script path.'
}

function Read-TextFile {
    param([Parameter(Mandatory=$true)][string] $Path)
    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Required file missing: {0}' -f $Path)
    }
    return [System.IO.File]::ReadAllText($Path)
}

function Assert-FileExists {
    param(
        [Parameter(Mandatory=$true)][string] $Path,
        [Parameter(Mandatory=$true)][string] $Label
    )
    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Expected {0} file was not found: {1}' -f $Label, $Path)
    }
}

function Assert-FileMissing {
    param(
        [Parameter(Mandatory=$true)][string] $Path,
        [Parameter(Mandatory=$true)][string] $Label
    )
    if (Test-Path -Path $Path -PathType Leaf) {
        throw ('Legacy {0} file should have been moved: {1}' -f $Label, $Path)
    }
}

function Assert-Matches {
    param(
        [Parameter(Mandatory=$true)][string] $Path,
        [Parameter(Mandatory=$true)][string] $Pattern,
        [Parameter(Mandatory=$true)][string] $Label
    )
    $content = Read-TextFile -Path $Path
    if ($content -notmatch $Pattern) {
        throw ('Expected import line missing for {0} in {1}' -f $Label, $Path)
    }
}

function Assert-DoesNotMatch {
    param(
        [Parameter(Mandatory=$true)][string] $Path,
        [Parameter(Mandatory=$true)][string] $Pattern,
        [Parameter(Mandatory=$true)][string] $Label
    )
    $content = Read-TextFile -Path $Path
    if ($content -match $Pattern) {
        throw ('Unexpected legacy import line found for {0} in {1}' -f $Label, $Path)
    }
}

function Assert-ScriptSafe {
    param([Parameter(Mandatory=$true)][string] $Path)
    $content = Read-TextFile -Path $Path
    if ($content -match '\$[A-Za-z_][A-Za-z0-9_]*:') {
        throw ('Unsafe variable-colon interpolation pattern found in {0}' -f $Path)
    }
    if ($content -match '@\(\s*\r?\n\s*@\(') {
        throw ('Unsafe nested PowerShell array pattern found in {0}' -f $Path)
    }
}

$repoRoot = Get-RepositoryRoot
$adminSrc = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src')
$featureRoot = [System.IO.Path]::Combine($adminSrc, 'features', 'security', 'credentialVault')

$pagePath = [System.IO.Path]::Combine($featureRoot, 'pages', 'CredentialVault.tsx')
$apiPath = [System.IO.Path]::Combine($featureRoot, 'api', 'credentialVaultApi.ts')
$typePath = [System.IO.Path]::Combine($featureRoot, 'types', 'credentialVault.ts')
$appPath = [System.IO.Path]::Combine($adminSrc, 'App.tsx')

Assert-FileExists -Path $pagePath -Label 'Credential Vault page'
Assert-FileExists -Path $apiPath -Label 'Credential Vault API'
Assert-FileExists -Path $typePath -Label 'Credential Vault types'
Assert-FileMissing -Path ([System.IO.Path]::Combine($adminSrc, 'pages', 'CredentialVault.tsx')) -Label 'Credential Vault page'
Assert-FileMissing -Path ([System.IO.Path]::Combine($adminSrc, 'api', 'credentialVaultApi.ts')) -Label 'Credential Vault API'
Assert-FileMissing -Path ([System.IO.Path]::Combine($adminSrc, 'types', 'credentialVault.ts')) -Label 'Credential Vault types'

Assert-Matches -Path $pagePath -Pattern '(?m)^import \{ credentialVaultApi \} from "\.\./api/credentialVaultApi";' -Label 'Credential Vault page API import'
Assert-Matches -Path $pagePath -Pattern '(?m)^import \{ Card, EmptyState, StatusPill \} from "\.\./\.\./\.\./\.\./components/Card";' -Label 'Credential Vault page Card import'
Assert-Matches -Path $pagePath -Pattern '(?m)^import \{ LoadingError \} from "\.\./\.\./\.\./\.\./components/LoadingError";' -Label 'Credential Vault page LoadingError import'
Assert-Matches -Path $pagePath -Pattern '(?m)^import type \{ ConnectorCredentialCatalogItem, ConnectorCredentialValidationResponse, ConnectorCredentialVaultSummary \} from "\.\./types/credentialVault";' -Label 'Credential Vault page type import'
Assert-DoesNotMatch -Path $pagePath -Pattern '(?m)^import \{ Card, EmptyState, StatusPill \} from "\.\./components/Card";' -Label 'Credential Vault legacy Card import'
Assert-DoesNotMatch -Path $pagePath -Pattern '(?m)^import \{ LoadingError \} from "\.\./components/LoadingError";' -Label 'Credential Vault legacy LoadingError import'

Assert-Matches -Path $apiPath -Pattern '(?m)^import \{ apiGet, apiPost \} from "\.\./\.\./\.\./\.\./api/core/adminApiClient";' -Label 'Credential Vault API core client import'
Assert-Matches -Path $apiPath -Pattern '(?m)^import type \{ ConnectorCredentialCatalogItem, ConnectorCredentialValidationRequest, ConnectorCredentialValidationResponse, ConnectorCredentialVaultSummary \} from "\.\./types/credentialVault";' -Label 'Credential Vault API type import'
Assert-DoesNotMatch -Path $apiPath -Pattern '(?m)^import \{ apiGet, apiPost \} from "\./core/adminApiClient";' -Label 'Credential Vault legacy API client import'

Assert-Matches -Path $appPath -Pattern '(?m)^import \{ CredentialVault \} from "\./features/security/credentialVault/pages/CredentialVault";' -Label 'App.tsx Credential Vault import'
Assert-DoesNotMatch -Path $appPath -Pattern '(?m)^import \{ CredentialVault \} from "\./pages/CredentialVault";' -Label 'App.tsx legacy Credential Vault import'

$toolRoot = [System.IO.Path]::Combine($repoRoot, 'tools', 'p10', 'P10.2AN-Repair')
$applyScript = [System.IO.Path]::Combine($toolRoot, 'Apply-P10.2AN-Repair-AdminWebCredentialVaultFeatureMove.ps1')
Assert-ScriptSafe -Path $applyScript

Write-Host 'P10.2AN Repair validation passed.'
