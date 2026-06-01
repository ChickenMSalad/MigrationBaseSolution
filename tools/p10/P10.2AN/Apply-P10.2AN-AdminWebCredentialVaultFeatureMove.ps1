Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    $scriptRoot = $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
        $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    }

    $current = [System.IO.DirectoryInfo]::new($scriptRoot)
    while ($null -ne $current) {
        $candidate = [System.IO.Path]::Combine($current.FullName, 'MigrationBaseSolution.sln')
        if (Test-Path -Path $candidate -PathType Leaf) {
            return $current.FullName
        }
        $current = $current.Parent
    }

    throw 'Could not locate repository root containing MigrationBaseSolution.sln.'
}

function Join-RepoPath {
    param(
        [Parameter(Mandatory = $true)][string] $Root,
        [Parameter(Mandatory = $true)][string[]] $Segments
    )

    $result = $Root
    foreach ($segment in $Segments) {
        $result = [System.IO.Path]::Combine($result, $segment)
    }
    return $result
}

function Read-TextFile {
    param([Parameter(Mandatory = $true)][string] $Path)
    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Required file was not found: {0}' -f $Path)
    }
    return [System.IO.File]::ReadAllText($Path)
}

function Write-TextFile {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Content
    )
    [System.IO.File]::WriteAllText($Path, $Content, [System.Text.UTF8Encoding]::new($false))
}

function Move-RequiredFile {
    param(
        [Parameter(Mandatory = $true)][string] $Label,
        [Parameter(Mandatory = $true)][string] $Source,
        [Parameter(Mandatory = $true)][string] $Destination
    )

    $destinationDirectory = Split-Path -Parent $Destination
    if (-not (Test-Path -Path $destinationDirectory -PathType Container)) {
        New-Item -Path $destinationDirectory -ItemType Directory -Force | Out-Null
    }

    if (Test-Path -Path $Destination -PathType Leaf) {
        Write-Host ('Already moved {0}: {1}' -f $Label, $Destination)
        return
    }

    if (-not (Test-Path -Path $Source -PathType Leaf)) {
        throw ('Required source file was not found for {0}: {1}' -f $Label, $Source)
    }

    Move-Item -Path $Source -Destination $Destination
    Write-Host ('Moved {0}: {1}' -f $Label, $Destination)
}

function Assert-MoveInputs {
    param([Parameter(Mandatory = $true)][object[]] $Moves)

    foreach ($move in $Moves) {
        if ($null -eq $move.PSObject.Properties['Source']) { throw 'Move definition missing Source.' }
        if ($null -eq $move.PSObject.Properties['Destination']) { throw 'Move definition missing Destination.' }
        if ((Test-Path -Path $move.Destination -PathType Leaf) -and (Test-Path -Path $move.Source -PathType Leaf)) {
            throw ('Both source and destination exist for {0}. Refusing ambiguous move.' -f $move.Label)
        }
        if (-not (Test-Path -Path $move.Destination -PathType Leaf) -and -not (Test-Path -Path $move.Source -PathType Leaf)) {
            throw ('Neither source nor destination exists for {0}. Source: {1} Destination: {2}' -f $move.Label, $move.Source, $move.Destination)
        }
    }
}

function Update-TextRegex {
    param(
        [Parameter(Mandatory = $true)][string] $Label,
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Pattern,
        [Parameter(Mandatory = $true)][string] $Replacement,
        [Parameter(Mandatory = $true)][string] $AlreadyPattern
    )

    $content = Read-TextFile -Path $Path
    if ([regex]::IsMatch($content, $AlreadyPattern, [System.Text.RegularExpressions.RegexOptions]::Multiline)) {
        Write-Host ('Already updated {0}: {1}' -f $Label, $Path)
        return
    }

    if (-not [regex]::IsMatch($content, $Pattern, [System.Text.RegularExpressions.RegexOptions]::Multiline)) {
        throw ('Unable to update {0}; expected import line was not found in {1}' -f $Label, $Path)
    }

    $updated = [regex]::Replace($content, $Pattern, $Replacement, [System.Text.RegularExpressions.RegexOptions]::Multiline)
    Write-TextFile -Path $Path -Content $updated
    Write-Host ('Updated {0}: {1}' -f $Label, $Path)
}

$repoRoot = Get-RepositoryRoot
$adminSrc = Join-RepoPath -Root $repoRoot -Segments @('src','Admin','Migration.Admin.Web','src')

$sourcePage = Join-RepoPath -Root $adminSrc -Segments @('pages','CredentialVault.tsx')
$sourceApi = Join-RepoPath -Root $adminSrc -Segments @('api','credentialVaultApi.ts')
$sourceTypes = Join-RepoPath -Root $adminSrc -Segments @('types','credentialVault.ts')

$featureRoot = Join-RepoPath -Root $adminSrc -Segments @('features','security','credentialVault')
$destPage = Join-RepoPath -Root $featureRoot -Segments @('pages','CredentialVault.tsx')
$destApi = Join-RepoPath -Root $featureRoot -Segments @('api','credentialVaultApi.ts')
$destTypes = Join-RepoPath -Root $featureRoot -Segments @('types','credentialVault.ts')
$appPath = Join-RepoPath -Root $adminSrc -Segments @('App.tsx')

$moves = @(
    [pscustomobject]@{ Label = 'Credential Vault page'; Source = $sourcePage; Destination = $destPage },
    [pscustomobject]@{ Label = 'Credential Vault API'; Source = $sourceApi; Destination = $destApi },
    [pscustomobject]@{ Label = 'Credential Vault types'; Source = $sourceTypes; Destination = $destTypes }
)

Assert-MoveInputs -Moves $moves

foreach ($move in $moves) {
    Move-RequiredFile -Label $move.Label -Source $move.Source -Destination $move.Destination
}

Update-TextRegex -Label 'Credential Vault page API import' -Path $destPage `
    -Pattern '^import \{ credentialVaultApi \} from "\.\./api/credentialVaultApi";' `
    -Replacement 'import { credentialVaultApi } from "../api/credentialVaultApi";' `
    -AlreadyPattern '^import \{ credentialVaultApi \} from "\.\./api/credentialVaultApi";'

Update-TextRegex -Label 'Credential Vault page Card import' -Path $destPage `
    -Pattern '^import \{ Card, EmptyState, StatusPill \} from "\.\./components/Card";' `
    -Replacement 'import { Card, EmptyState, StatusPill } from "../../../../components/Card";' `
    -AlreadyPattern '^import \{ Card, EmptyState, StatusPill \} from "\.\./\.\./\.\./\.\./components/Card";'

Update-TextRegex -Label 'Credential Vault page LoadingError import' -Path $destPage `
    -Pattern '^import \{ LoadingError \} from "\.\./components/LoadingError";' `
    -Replacement 'import { LoadingError } from "../../../../components/LoadingError";' `
    -AlreadyPattern '^import \{ LoadingError \} from "\.\./\.\./\.\./\.\./components/LoadingError";'

Update-TextRegex -Label 'Credential Vault page type import' -Path $destPage `
    -Pattern '^\} from "\.\./types/credentialVault";' `
    -Replacement '} from "../types/credentialVault";' `
    -AlreadyPattern '^\} from "\.\./types/credentialVault";'

Update-TextRegex -Label 'Credential Vault API core client import' -Path $destApi `
    -Pattern '^import \{ adminApiClient \} from "\.\/core\/client";' `
    -Replacement 'import { adminApiClient } from "../../../../api/core/client";' `
    -AlreadyPattern '^import \{ adminApiClient \} from "\.\./\.\./\.\./\.\./api/core/client";'

Update-TextRegex -Label 'App.tsx Credential Vault import' -Path $appPath `
    -Pattern '^import \{ CredentialVault \} from "\.\/pages\/CredentialVault";' `
    -Replacement 'import { CredentialVault } from "./features/security/credentialVault/pages/CredentialVault";' `
    -AlreadyPattern '^import \{ CredentialVault \} from "\.\/features\/security\/credentialVault\/pages\/CredentialVault";'

Write-Host 'P10.2AN Credential Vault feature move completed.'
