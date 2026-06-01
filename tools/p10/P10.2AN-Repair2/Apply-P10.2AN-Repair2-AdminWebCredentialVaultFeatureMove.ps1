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

function Ensure-Directory {
    param([Parameter(Mandatory = $true)][string] $Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
    }
}

function Move-FileIfNeeded {
    param(
        [Parameter(Mandatory = $true)][string] $Source,
        [Parameter(Mandatory = $true)][string] $Destination,
        [Parameter(Mandatory = $true)][string] $Label
    )

    if (Test-Path -LiteralPath $Destination -PathType Leaf) {
        Write-Host ('Already moved {0}: {1}' -f $Label, $Destination)
        return
    }

    if (-not (Test-Path -LiteralPath $Source -PathType Leaf)) {
        throw ('Required file for {0} was not found at source or destination. Source: {1} Destination: {2}' -f $Label, $Source, $Destination)
    }

    $destinationDirectory = Split-Path -Parent $Destination
    Ensure-Directory -Path $destinationDirectory
    Move-Item -LiteralPath $Source -Destination $Destination
    Write-Host ('Moved {0}: {1}' -f $Label, $Destination)
}

function Read-TextFile {
    param([Parameter(Mandatory = $true)][string] $Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw ('Required file was not found: {0}' -f $Path)
    }

    return Get-Content -LiteralPath $Path -Raw
}

function Write-TextFile {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Content
    )

    Set-Content -LiteralPath $Path -Value $Content -Encoding UTF8
}

function Replace-ImportPath {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $ImportTargetName,
        [Parameter(Mandatory = $true)][string] $ReplacementPath,
        [Parameter(Mandatory = $true)][string] $Label
    )

    $content = Read-TextFile -Path $Path
    $escapedTarget = [regex]::Escape($ImportTargetName)
    $pattern = '(from\s+["''])([^"'']*' + $escapedTarget + ')(["''])'
    $replacement = ('$1' + $ReplacementPath + '$3')
    $updated = [regex]::Replace($content, $pattern, $replacement)

    if ($updated -eq $content) {
        if ($content -match ('from\s+["'']' + [regex]::Escape($ReplacementPath) + '["'']')) {
            Write-Host ('Already updated {0}: {1}' -f $Label, $Path)
            return
        }

        throw ('Unable to update {0}; import target was not found in {1}' -f $Label, $Path)
    }

    Write-TextFile -Path $Path -Content $updated
    Write-Host ('Updated {0}: {1}' -f $Label, $Path)
}

function Replace-ExactTextIfPresent {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $OldText,
        [Parameter(Mandatory = $true)][string] $NewText,
        [Parameter(Mandatory = $true)][string] $Label
    )

    $content = Read-TextFile -Path $Path
    if ($content.Contains($NewText)) {
        Write-Host ('Already updated {0}: {1}' -f $Label, $Path)
        return
    }

    if (-not $content.Contains($OldText)) {
        throw ('Unable to update {0}; expected text was not found in {1}' -f $Label, $Path)
    }

    $updated = $content.Replace($OldText, $NewText)
    Write-TextFile -Path $Path -Content $updated
    Write-Host ('Updated {0}: {1}' -f $Label, $Path)
}

$repoRoot = Get-RepositoryRoot
$adminSrc = Join-RepoPath -Root $repoRoot -Segments @('src','Admin','Migration.Admin.Web','src')
$featureRoot = Join-RepoPath -Root $adminSrc -Segments @('features','security','credentialVault')
$pageDirectory = Join-RepoPath -Root $featureRoot -Segments @('pages')
$apiDirectory = Join-RepoPath -Root $featureRoot -Segments @('api')
$typeDirectory = Join-RepoPath -Root $featureRoot -Segments @('types')

Ensure-Directory -Path $pageDirectory
Ensure-Directory -Path $apiDirectory
Ensure-Directory -Path $typeDirectory

$pageSource = Join-RepoPath -Root $adminSrc -Segments @('pages','CredentialVault.tsx')
$apiSource = Join-RepoPath -Root $adminSrc -Segments @('api','credentialVaultApi.ts')
$typeSource = Join-RepoPath -Root $adminSrc -Segments @('types','credentialVault.ts')
$pageDestination = Join-RepoPath -Root $pageDirectory -Segments @('CredentialVault.tsx')
$apiDestination = Join-RepoPath -Root $apiDirectory -Segments @('credentialVaultApi.ts')
$typeDestination = Join-RepoPath -Root $typeDirectory -Segments @('credentialVault.ts')
$appPath = Join-RepoPath -Root $adminSrc -Segments @('App.tsx')

Move-FileIfNeeded -Source $pageSource -Destination $pageDestination -Label 'Credential Vault page'
Move-FileIfNeeded -Source $apiSource -Destination $apiDestination -Label 'Credential Vault API'
Move-FileIfNeeded -Source $typeSource -Destination $typeDestination -Label 'Credential Vault types'

Replace-ImportPath -Path $pageDestination -ImportTargetName 'credentialVaultApi' -ReplacementPath '../api/credentialVaultApi' -Label 'Credential Vault page API import'
Replace-ImportPath -Path $pageDestination -ImportTargetName 'components/Card' -ReplacementPath '../../../../components/Card' -Label 'Credential Vault page Card import'
Replace-ImportPath -Path $pageDestination -ImportTargetName 'components/LoadingError' -ReplacementPath '../../../../components/LoadingError' -Label 'Credential Vault page LoadingError import'
Replace-ImportPath -Path $pageDestination -ImportTargetName 'credentialVault' -ReplacementPath '../types/credentialVault' -Label 'Credential Vault page type import'
Replace-ImportPath -Path $apiDestination -ImportTargetName 'core/adminApiClient' -ReplacementPath '../../../../api/core/adminApiClient' -Label 'Credential Vault API core client import'
Replace-ImportPath -Path $apiDestination -ImportTargetName 'credentialVault' -ReplacementPath '../types/credentialVault' -Label 'Credential Vault API type import'

Replace-ExactTextIfPresent -Path $appPath -OldText 'from "./pages/CredentialVault"' -NewText 'from "./features/security/credentialVault/pages/CredentialVault"' -Label 'App.tsx Credential Vault import'

Write-Host 'P10.2AN Repair2 apply completed.'
