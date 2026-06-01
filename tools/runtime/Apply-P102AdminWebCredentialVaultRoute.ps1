[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    if (-not [string]::IsNullOrWhiteSpace($PSCommandPath)) {
        $scriptRoot = Split-Path -Parent $PSCommandPath
    }
}
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    throw 'Unable to resolve script root.'
}

$repoRoot = Split-Path -Parent (Split-Path -Parent $scriptRoot)

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

$appPath = Join-RepoPath 'src/Admin/Migration.Admin.Web/src/App.tsx'
$layoutPath = Join-RepoPath 'src/Admin/Migration.Admin.Web/src/components/Layout.tsx'
$pagePath = Join-RepoPath 'src/Admin/Migration.Admin.Web/src/pages/CredentialVault.tsx'

foreach ($path in @($appPath, $layoutPath, $pagePath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw ('Required Admin Web file is missing: {0}' -f $path)
    }
}

$appText = Get-Content -LiteralPath $appPath -Raw

if ($appText.IndexOf('pages/CredentialVault', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    $anchor = 'import { Credentials } from "./pages/Credentials";'
    if ($appText.IndexOf($anchor, [System.StringComparison]::Ordinal) -lt 0) {
        throw 'Unable to find Credentials import anchor in Admin Web App.tsx.'
    }
    $replacement = $anchor + ' import { CredentialVault } from "./pages/CredentialVault";'
    $appText = $appText.Replace($anchor, $replacement)
}

if ($appText.IndexOf('path="/credential-vault"', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    $routeAnchor = '<Route path="/credentials" element={<Credentials />} />'
    if ($appText.IndexOf($routeAnchor, [System.StringComparison]::Ordinal) -lt 0) {
        throw 'Unable to find Credentials route anchor in Admin Web App.tsx.'
    }
    $routeReplacement = $routeAnchor + ' <Route path="/credential-vault" element={<CredentialVault />} />'
    $appText = $appText.Replace($routeAnchor, $routeReplacement)
}

Set-Content -LiteralPath $appPath -Value $appText -Encoding UTF8

$layoutText = Get-Content -LiteralPath $layoutPath -Raw

if ($layoutText.IndexOf('to: "/credential-vault"', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    $navAnchor = '{ to: "/credentials", label: "Credentials", icon: KeyRound }'
    if ($layoutText.IndexOf($navAnchor, [System.StringComparison]::Ordinal) -lt 0) {
        throw 'Unable to find Credentials navigation anchor in Admin Web Layout.tsx.'
    }
    $navReplacement = $navAnchor + ', { to: "/credential-vault", label: "Credential Vault", icon: KeyRound }'
    $layoutText = $layoutText.Replace($navAnchor, $navReplacement)
}

Set-Content -LiteralPath $layoutPath -Value $layoutText -Encoding UTF8

Write-Host 'P10.2M Admin Web Credential Vault route wiring applied.'
