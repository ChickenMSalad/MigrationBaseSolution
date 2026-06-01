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
        throw ('Required file was not found: {0}' -f $Path)
    }
    return [System.IO.File]::ReadAllText($Path)
}

function Write-TextFile {
    param(
        [Parameter(Mandatory=$true)][string] $Path,
        [Parameter(Mandatory=$true)][string] $Content
    )
    [System.IO.File]::WriteAllText($Path, $Content, [System.Text.UTF8Encoding]::new($false))
}

function Move-IfNeeded {
    param(
        [Parameter(Mandatory=$true)][string] $Source,
        [Parameter(Mandatory=$true)][string] $Destination,
        [Parameter(Mandatory=$true)][string] $Label
    )
    if (Test-Path -Path $Destination -PathType Leaf) {
        Write-Host ('Already moved {0}: {1}' -f $Label, $Destination)
        return
    }
    if (-not (Test-Path -Path $Source -PathType Leaf)) {
        throw ('Required source file was not found for {0}: {1}' -f $Label, $Source)
    }
    $destinationDirectory = Split-Path -Parent $Destination
    if (-not (Test-Path -Path $destinationDirectory -PathType Container)) {
        New-Item -ItemType Directory -Path $destinationDirectory | Out-Null
    }
    Move-Item -Path $Source -Destination $Destination
    Write-Host ('Moved {0}: {1}' -f $Label, $Destination)
}

function Replace-RegexOnceOrNoop {
    param(
        [Parameter(Mandatory=$true)][string] $Path,
        [Parameter(Mandatory=$true)][string] $Pattern,
        [Parameter(Mandatory=$true)][string] $Replacement,
        [Parameter(Mandatory=$true)][string] $AlreadyPattern,
        [Parameter(Mandatory=$true)][string] $Label
    )
    $content = Read-TextFile -Path $Path
    if ($content -match $AlreadyPattern) {
        Write-Host ('Already updated {0}: {1}' -f $Label, $Path)
        return
    }
    if ($content -notmatch $Pattern) {
        throw ('Unable to update {0}; expected import line was not found in {1}' -f $Label, $Path)
    }
    $updated = [System.Text.RegularExpressions.Regex]::Replace($content, $Pattern, $Replacement, 1)
    Write-TextFile -Path $Path -Content $updated
    Write-Host ('Updated {0}: {1}' -f $Label, $Path)
}

$repoRoot = Get-RepositoryRoot
$adminSrc = [System.IO.Path]::Combine($repoRoot, 'src', 'Admin', 'Migration.Admin.Web', 'src')

$pageSource = [System.IO.Path]::Combine($adminSrc, 'pages', 'CredentialVault.tsx')
$apiSource = [System.IO.Path]::Combine($adminSrc, 'api', 'credentialVaultApi.ts')
$typeSource = [System.IO.Path]::Combine($adminSrc, 'types', 'credentialVault.ts')

$featureRoot = [System.IO.Path]::Combine($adminSrc, 'features', 'security', 'credentialVault')
$pageDestination = [System.IO.Path]::Combine($featureRoot, 'pages', 'CredentialVault.tsx')
$apiDestination = [System.IO.Path]::Combine($featureRoot, 'api', 'credentialVaultApi.ts')
$typeDestination = [System.IO.Path]::Combine($featureRoot, 'types', 'credentialVault.ts')
$appPath = [System.IO.Path]::Combine($adminSrc, 'App.tsx')

Move-IfNeeded -Source $pageSource -Destination $pageDestination -Label 'Credential Vault page'
Move-IfNeeded -Source $apiSource -Destination $apiDestination -Label 'Credential Vault API'
Move-IfNeeded -Source $typeSource -Destination $typeDestination -Label 'Credential Vault types'

Replace-RegexOnceOrNoop -Path $pageDestination -Pattern '(?m)^import \{ credentialVaultApi \} from "\.\./api/credentialVaultApi";' -Replacement 'import { credentialVaultApi } from "../api/credentialVaultApi";' -AlreadyPattern '(?m)^import \{ credentialVaultApi \} from "\.\./api/credentialVaultApi";' -Label 'Credential Vault page API import'
Replace-RegexOnceOrNoop -Path $pageDestination -Pattern '(?m)^import \{ Card, EmptyState, StatusPill \} from "\.\./components/Card";' -Replacement 'import { Card, EmptyState, StatusPill } from "../../../../components/Card";' -AlreadyPattern '(?m)^import \{ Card, EmptyState, StatusPill \} from "\.\./\.\./\.\./\.\./components/Card";' -Label 'Credential Vault page Card import'
Replace-RegexOnceOrNoop -Path $pageDestination -Pattern '(?m)^import \{ LoadingError \} from "\.\./components/LoadingError";' -Replacement 'import { LoadingError } from "../../../../components/LoadingError";' -AlreadyPattern '(?m)^import \{ LoadingError \} from "\.\./\.\./\.\./\.\./components/LoadingError";' -Label 'Credential Vault page LoadingError import'
Replace-RegexOnceOrNoop -Path $pageDestination -Pattern '(?m)^import type \{ ConnectorCredentialCatalogItem, ConnectorCredentialValidationResponse, ConnectorCredentialVaultSummary \} from "\.\./types/credentialVault";' -Replacement 'import type { ConnectorCredentialCatalogItem, ConnectorCredentialValidationResponse, ConnectorCredentialVaultSummary } from "../types/credentialVault";' -AlreadyPattern '(?m)^import type \{ ConnectorCredentialCatalogItem, ConnectorCredentialValidationResponse, ConnectorCredentialVaultSummary \} from "\.\./types/credentialVault";' -Label 'Credential Vault page type import'

Replace-RegexOnceOrNoop -Path $apiDestination -Pattern '(?m)^import \{ apiGet, apiPost \} from "\./core/adminApiClient";' -Replacement 'import { apiGet, apiPost } from "../../../../api/core/adminApiClient";' -AlreadyPattern '(?m)^import \{ apiGet, apiPost \} from "\.\./\.\./\.\./\.\./api/core/adminApiClient";' -Label 'Credential Vault API core client import'

Replace-RegexOnceOrNoop -Path $appPath -Pattern '(?m)^import \{ CredentialVault \} from "\./pages/CredentialVault";' -Replacement 'import { CredentialVault } from "./features/security/credentialVault/pages/CredentialVault";' -AlreadyPattern '(?m)^import \{ CredentialVault \} from "\./features/security/credentialVault/pages/CredentialVault";' -Label 'App.tsx Credential Vault import'

Write-Host 'P10.2AN Repair completed.'
