[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Resolve-ScriptRoot {
    $root = $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($root)) {
        if (-not [string]::IsNullOrWhiteSpace($PSCommandPath)) {
            $root = Split-Path -Parent $PSCommandPath
        }
    }
    if ([string]::IsNullOrWhiteSpace($root)) {
        throw 'Unable to resolve script root.'
    }
    return $root
}

function Join-RepoPath {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $RepoRoot,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $RelativePath
    )

    $path = $RepoRoot
    foreach ($part in ($RelativePath -split '[\\/]')) {
        if (-not [string]::IsNullOrWhiteSpace($part)) {
            $path = [System.IO.Path]::Combine($path, $part)
        }
    }
    return $path
}

$scriptRoot = Resolve-ScriptRoot
$repoRoot = Split-Path -Parent (Split-Path -Parent $scriptRoot)

$appPath = Join-RepoPath -RepoRoot $repoRoot -RelativePath 'src/Admin/Migration.Admin.Web/src/App.tsx'
$layoutPath = Join-RepoPath -RepoRoot $repoRoot -RelativePath 'src/Admin/Migration.Admin.Web/src/components/Layout.tsx'

foreach ($requiredPath in @($appPath, $layoutPath)) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
        throw ('Required Admin Web file is missing: {0}' -f $requiredPath)
    }
}

$appText = Get-Content -LiteralPath $appPath -Raw

if ($appText.IndexOf('import { ConnectorConfiguration } from "./pages/ConnectorConfiguration";', [System.StringComparison]::Ordinal) -lt 0) {
    $importAnchor = 'import { Connectors } from "./pages/Connectors";'
    if ($appText.IndexOf($importAnchor, [System.StringComparison]::Ordinal) -lt 0) {
        throw 'Unable to find Connectors import anchor in Admin Web App.tsx.'
    }
    $appText = $appText.Replace(
        $importAnchor,
        $importAnchor + ' import { ConnectorConfiguration } from "./pages/ConnectorConfiguration";'
    )
}

if ($appText.IndexOf('path="/connector-configuration"', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    $routeText = '<Route path="/connector-configuration" element={<ConnectorConfiguration />} />'
    $routeAnchors = @(
        '<Route path="/connectors" element={<Connectors />} />',
        '<Route path="/credentials" element={<Credentials />} />'
    )

    $routeInserted = $false
    foreach ($anchor in $routeAnchors) {
        if ($appText.IndexOf($anchor, [System.StringComparison]::Ordinal) -ge 0) {
            $appText = $appText.Replace($anchor, $routeText + ' ' + $anchor)
            $routeInserted = $true
            break
        }
    }

    if (-not $routeInserted) {
        throw 'Unable to find a supported route anchor in Admin Web App.tsx.'
    }
}

Set-Content -LiteralPath $appPath -Value $appText -Encoding UTF8

$layoutText = Get-Content -LiteralPath $layoutPath -Raw

if ($layoutText.IndexOf('/connector-configuration', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    $navAnchor = '{ to: "/connectors", label: "Connectors", icon: PlugZap },'
    if ($layoutText.IndexOf($navAnchor, [System.StringComparison]::Ordinal) -lt 0) {
        throw 'Unable to find Connectors nav anchor in Admin Web Layout.tsx.'
    }

    $navText = $navAnchor + ' { to: "/connector-configuration", label: "Connector Config", icon: PlugZap },'
    $layoutText = $layoutText.Replace($navAnchor, $navText)
}

Set-Content -LiteralPath $layoutPath -Value $layoutText -Encoding UTF8

Write-Host 'P10.2O connector configuration route wiring applied.'
