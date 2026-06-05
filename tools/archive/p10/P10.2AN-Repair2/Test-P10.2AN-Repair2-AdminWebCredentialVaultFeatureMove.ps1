Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Get-RepositoryRoot {
    $scriptRoot = $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
        $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    }

    $candidate = Resolve-Path -LiteralPath (Join-Path $scriptRoot (Join-Path '..' (Join-Path '..' '..')))
    return $candidate.ProviderPath
}

function Join-RepoPath {
    param(
        [Parameter(Mandatory = $true)][string] $Root,
        [Parameter(Mandatory = $true)][string[]] $Segments
    )

    $path = $Root
    foreach ($segment in $Segments) {
        $path = Join-Path $path $segment
    }
    return $path
}

function Assert-FileExists {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Label
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw ('Expected file missing for {0}: {1}' -f $Label, $Path)
    }
}

function Assert-FileAbsent {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Label
    )

    if (Test-Path -LiteralPath $Path -PathType Leaf) {
        throw ('Legacy file still exists for {0}: {1}' -f $Label, $Path)
    }
}

function Read-TextFile {
    param([Parameter(Mandatory = $true)][string] $Path)

    Assert-FileExists -Path $Path -Label 'read target'
    return Get-Content -LiteralPath $Path -Raw
}

function Assert-ImportPath {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $ExpectedPath,
        [Parameter(Mandatory = $true)][string] $Label
    )

    $content = Read-TextFile -Path $Path
    $pattern = 'from\s+["'']' + [regex]::Escape($ExpectedPath) + '["'']'
    if ($content -notmatch $pattern) {
        throw ('Expected import missing for {0}: {1}' -f $Label, $ExpectedPath)
    }
}

function Assert-NoLegacyImportPath {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $LegacyPath,
        [Parameter(Mandatory = $true)][string] $Label
    )

    $content = Read-TextFile -Path $Path
    $pattern = 'from\s+["'']' + [regex]::Escape($LegacyPath) + '["'']'
    if ($content -match $pattern) {
        throw ('Legacy import still present for {0}: {1}' -f $Label, $LegacyPath)
    }
}

$repoRoot = Get-RepositoryRoot
$adminSrc = Join-RepoPath -Root $repoRoot -Segments @('src','Admin','Migration.Admin.Web','src')
$featureRoot = Join-RepoPath -Root $adminSrc -Segments @('features','security','credentialVault')
$pagePath = Join-RepoPath -Root $featureRoot -Segments @('pages','CredentialVault.tsx')
$apiPath = Join-RepoPath -Root $featureRoot -Segments @('api','credentialVaultApi.ts')
$typePath = Join-RepoPath -Root $featureRoot -Segments @('types','credentialVault.ts')
$appPath = Join-RepoPath -Root $adminSrc -Segments @('App.tsx')

Assert-FileExists -Path $pagePath -Label 'Credential Vault page'
Assert-FileExists -Path $apiPath -Label 'Credential Vault API'
Assert-FileExists -Path $typePath -Label 'Credential Vault types'
Assert-FileExists -Path $appPath -Label 'App.tsx'

Assert-FileAbsent -Path (Join-RepoPath -Root $adminSrc -Segments @('pages','CredentialVault.tsx')) -Label 'Credential Vault page'
Assert-FileAbsent -Path (Join-RepoPath -Root $adminSrc -Segments @('api','credentialVaultApi.ts')) -Label 'Credential Vault API'
Assert-FileAbsent -Path (Join-RepoPath -Root $adminSrc -Segments @('types','credentialVault.ts')) -Label 'Credential Vault types'

Assert-ImportPath -Path $pagePath -ExpectedPath '../api/credentialVaultApi' -Label 'Credential Vault page API import'
Assert-ImportPath -Path $pagePath -ExpectedPath '../../../../components/Card' -Label 'Credential Vault page Card import'
Assert-ImportPath -Path $pagePath -ExpectedPath '../../../../components/LoadingError' -Label 'Credential Vault page LoadingError import'
Assert-ImportPath -Path $pagePath -ExpectedPath '../types/credentialVault' -Label 'Credential Vault page type import'
Assert-ImportPath -Path $apiPath -ExpectedPath '../../../../api/core/adminApiClient' -Label 'Credential Vault API core client import'
Assert-ImportPath -Path $apiPath -ExpectedPath '../types/credentialVault' -Label 'Credential Vault API type import'
Assert-ImportPath -Path $appPath -ExpectedPath './features/security/credentialVault/pages/CredentialVault' -Label 'App.tsx Credential Vault import'

Assert-NoLegacyImportPath -Path $pagePath -LegacyPath '../components/Card' -Label 'Credential Vault page Card import'
Assert-NoLegacyImportPath -Path $pagePath -LegacyPath '../components/LoadingError' -Label 'Credential Vault page LoadingError import'
Assert-NoLegacyImportPath -Path $appPath -LegacyPath './pages/CredentialVault' -Label 'App.tsx Credential Vault import'

Write-Host 'P10.2AN Repair2 validation passed.'
