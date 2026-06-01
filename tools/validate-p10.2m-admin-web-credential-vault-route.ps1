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
        [ValidateNotNullOrEmpty()]
        [string] $RelativePath
    )

    $fullPath = $repoRoot
    foreach ($part in ($RelativePath -split '/')) {
        if (-not [string]::IsNullOrWhiteSpace($part)) {
            $fullPath = [System.IO.Path]::Combine($fullPath, $part)
        }
    }
    return $fullPath
}

$requiredFiles = @(
    'docs/p10/P10.2M-Admin-Web-Credential-Vault-Route.md',
    'docs/operations/admin-web-credential-vault-route.md',
    'config-samples/p10-admin-web-credential-vault-route.sample.json',
    'src/Admin/Migration.Admin.Web/src/pages/CredentialVault.tsx',
    'src/Admin/Migration.Admin.Web/src/App.tsx',
    'src/Admin/Migration.Admin.Web/src/components/Layout.tsx',
    'tools/runtime/Apply-P102AdminWebCredentialVaultRoute.ps1'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-RepoPath $relativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw ('Required P10.2M file is missing: {0}' -f $relativePath)
    }
}

$scriptsToParse = @(
    'tools/runtime/Apply-P102AdminWebCredentialVaultRoute.ps1'
)

foreach ($relativeScript in $scriptsToParse) {
    $scriptPath = Join-RepoPath $relativeScript
    $parseErrors = $null
    $scriptText = Get-Content -LiteralPath $scriptPath -Raw
    [System.Management.Automation.PSParser]::Tokenize($scriptText, [ref]$parseErrors) | Out-Null
    if ($null -ne $parseErrors -and @($parseErrors).Count -gt 0) {
        $message = (@($parseErrors) | ForEach-Object { $_.Message }) -join '; '
        throw ('PowerShell parser errors in {0}: {1}' -f $relativeScript, $message)
    }
}

$appText = Get-Content -LiteralPath (Join-RepoPath 'src/Admin/Migration.Admin.Web/src/App.tsx') -Raw
foreach ($term in @('pages/CredentialVault', 'path="/credential-vault"', 'CredentialVault')) {
    if ($appText.IndexOf($term, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw ('Admin Web App.tsx is missing Credential Vault route term: {0}' -f $term)
    }
}

$layoutText = Get-Content -LiteralPath (Join-RepoPath 'src/Admin/Migration.Admin.Web/src/components/Layout.tsx') -Raw
foreach ($term in @('to: "/credential-vault"', 'Credential Vault')) {
    if ($layoutText.IndexOf($term, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw ('Admin Web Layout.tsx is missing Credential Vault navigation term: {0}' -f $term)
    }
}

$config = Get-Content -LiteralPath (Join-RepoPath 'config-samples/p10-admin-web-credential-vault-route.sample.json') -Raw | ConvertFrom-Json
foreach ($propertyName in @('canonicalAdminUiPath', 'pagePath', 'routePath', 'featureSourcePath')) {
    if ($null -eq $config.PSObject.Properties[$propertyName]) {
        throw ('P10.2M config is missing property: {0}' -f $propertyName)
    }
}
if ($config.canonicalAdminUiPath -ne 'src/Admin/Migration.Admin.Web') {
    throw 'P10.2M canonicalAdminUiPath must remain src/Admin/Migration.Admin.Web.'
}

Write-Host 'P10.2M Admin Web Credential Vault route validation passed.'
