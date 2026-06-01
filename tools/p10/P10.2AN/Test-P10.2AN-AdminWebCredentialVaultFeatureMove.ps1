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

function Assert-FileExists {
    param(
        [Parameter(Mandatory = $true)][string] $Label,
        [Parameter(Mandatory = $true)][string] $Path
    )
    if (-not (Test-Path -Path $Path -PathType Leaf)) {
        throw ('Expected file missing for {0}: {1}' -f $Label, $Path)
    }
}

function Assert-FileMissing {
    param(
        [Parameter(Mandatory = $true)][string] $Label,
        [Parameter(Mandatory = $true)][string] $Path
    )
    if (Test-Path -Path $Path -PathType Leaf) {
        throw ('Legacy file should have been moved for {0}: {1}' -f $Label, $Path)
    }
}

function Assert-LineMatch {
    param(
        [Parameter(Mandatory = $true)][string] $Label,
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Pattern
    )
    $content = Read-TextFile -Path $Path
    if (-not [regex]::IsMatch($content, $Pattern, [System.Text.RegularExpressions.RegexOptions]::Multiline)) {
        throw ('Expected import line missing for {0}: {1}' -f $Label, $Path)
    }
}

function Assert-LineMissing {
    param(
        [Parameter(Mandatory = $true)][string] $Label,
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Pattern
    )
    $content = Read-TextFile -Path $Path
    if ([regex]::IsMatch($content, $Pattern, [System.Text.RegularExpressions.RegexOptions]::Multiline)) {
        throw ('Unexpected legacy import line found for {0}: {1}' -f $Label, $Path)
    }
}

$repoRoot = Get-RepositoryRoot
$adminSrc = Join-RepoPath -Root $repoRoot -Segments @('src','Admin','Migration.Admin.Web','src')
$featureRoot = Join-RepoPath -Root $adminSrc -Segments @('features','security','credentialVault')

$page = Join-RepoPath -Root $featureRoot -Segments @('pages','CredentialVault.tsx')
$api = Join-RepoPath -Root $featureRoot -Segments @('api','credentialVaultApi.ts')
$types = Join-RepoPath -Root $featureRoot -Segments @('types','credentialVault.ts')
$appPath = Join-RepoPath -Root $adminSrc -Segments @('App.tsx')

Assert-FileExists -Label 'Credential Vault page' -Path $page
Assert-FileExists -Label 'Credential Vault API' -Path $api
Assert-FileExists -Label 'Credential Vault types' -Path $types
Assert-FileMissing -Label 'legacy Credential Vault page' -Path (Join-RepoPath -Root $adminSrc -Segments @('pages','CredentialVault.tsx'))
Assert-FileMissing -Label 'legacy Credential Vault API' -Path (Join-RepoPath -Root $adminSrc -Segments @('api','credentialVaultApi.ts'))
Assert-FileMissing -Label 'legacy Credential Vault types' -Path (Join-RepoPath -Root $adminSrc -Segments @('types','credentialVault.ts'))

Assert-LineMatch -Label 'Credential Vault page API import' -Path $page -Pattern '^import \{ credentialVaultApi \} from "\.\./api/credentialVaultApi";'
Assert-LineMatch -Label 'Credential Vault page Card import' -Path $page -Pattern '^import \{ Card, EmptyState, StatusPill \} from "\.\./\.\./\.\./\.\./components/Card";'
Assert-LineMatch -Label 'Credential Vault page LoadingError import' -Path $page -Pattern '^import \{ LoadingError \} from "\.\./\.\./\.\./\.\./components/LoadingError";'
Assert-LineMatch -Label 'Credential Vault page type import' -Path $page -Pattern '^\} from "\.\./types/credentialVault";'
Assert-LineMatch -Label 'Credential Vault API core client import' -Path $api -Pattern '^import \{ adminApiClient \} from "\.\./\.\./\.\./\.\./api/core/client";'
Assert-LineMatch -Label 'App.tsx Credential Vault import' -Path $appPath -Pattern '^import \{ CredentialVault \} from "\.\/features\/security\/credentialVault\/pages\/CredentialVault";'

Assert-LineMissing -Label 'App.tsx legacy Credential Vault import' -Path $appPath -Pattern '^import \{ CredentialVault \} from "\.\/pages\/CredentialVault";'
Assert-LineMissing -Label 'Credential Vault page legacy Card import' -Path $page -Pattern '^import \{ Card, EmptyState, StatusPill \} from "\.\./components/Card";'
Assert-LineMissing -Label 'Credential Vault page legacy LoadingError import' -Path $page -Pattern '^import \{ LoadingError \} from "\.\./components/LoadingError";'

Write-Host 'P10.2AN Credential Vault feature move validation passed.'
