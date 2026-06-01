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
        [string] $RelativePath
    )

    $current = $repoRoot
    foreach ($part in ($RelativePath -split '/')) {
        if (-not [string]::IsNullOrWhiteSpace($part)) {
            $current = [System.IO.Path]::Combine($current, $part)
        }
    }
    return $current
}

$requiredFiles = @(
    'docs/p10/P10.2Z-Admin-Web-Feature-Folder-Structure.md',
    'docs/operations/admin-web-feature-folder-structure.md',
    'config-samples/p10-admin-web-feature-folder-structure.sample.json',
    'src/Admin/Migration.Admin.Web/src/features/README.md',
    'src/Admin/Migration.Admin.Web/src/features/operations/README.md',
    'src/Admin/Migration.Admin.Web/src/features/connectors/README.md',
    'src/Admin/Migration.Admin.Web/src/features/security/README.md',
    'src/Admin/Migration.Admin.Web/src/features/governance/README.md',
    'src/Admin/Migration.Admin.Web/src/features/platform/README.md',
    'tools/runtime/New-P102AdminWebFeatureStructureReport.ps1'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-RepoPath -RelativePath $relativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw ('Required P10.2Z file is missing: {0}' -f $relativePath)
    }
}

$scriptsToParse = @(
    'tools/runtime/New-P102AdminWebFeatureStructureReport.ps1'
)

foreach ($relativeScript in $scriptsToParse) {
    $scriptPath = Join-RepoPath -RelativePath $relativeScript
    if (-not (Test-Path -LiteralPath $scriptPath -PathType Leaf)) {
        throw ('Script file is missing: {0}' -f $relativeScript)
    }

    $scriptText = Get-Content -LiteralPath $scriptPath -Raw
    $parseErrors = $null
    [System.Management.Automation.PSParser]::Tokenize($scriptText, [ref]$parseErrors) | Out-Null
    if ($null -ne $parseErrors -and @($parseErrors).Count -gt 0) {
        $message = (@($parseErrors) | ForEach-Object { $_.Message }) -join '; '
        throw ('PowerShell parser errors in {0}: {1}' -f $relativeScript, $message)
    }
}

$configPath = Join-RepoPath -RelativePath 'config-samples/p10-admin-web-feature-folder-structure.sample.json'
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
foreach ($propertyName in @('canonicalAdminUiPath', 'featureSourcePath', 'canonicalFeatureRoot', 'featureGroups')) {
    if ($null -eq $config.PSObject.Properties[$propertyName]) {
        throw ('P10.2Z config is missing property: {0}' -f $propertyName)
    }
}
if ($config.canonicalAdminUiPath -ne 'src/Admin/Migration.Admin.Web') {
    throw 'P10.2Z canonicalAdminUiPath must remain src/Admin/Migration.Admin.Web.'
}
if ($config.featureSourcePath -ne 'apps/migration-admin-ui') {
    throw 'P10.2Z featureSourcePath must remain apps/migration-admin-ui.'
}
if ($config.canonicalFeatureRoot -ne 'src/Admin/Migration.Admin.Web/src/features') {
    throw 'P10.2Z canonicalFeatureRoot must remain src/Admin/Migration.Admin.Web/src/features.'
}
if (@($config.featureGroups).Count -lt 5) {
    throw 'P10.2Z config must define the expected canonical feature groups.'
}

$checks = @(
    [pscustomobject]@{ Path = 'docs/p10/P10.2Z-Admin-Web-Feature-Folder-Structure.md'; Terms = @('src/Admin/Migration.Admin.Web', 'apps/migration-admin-ui', 'features/operations') },
    [pscustomobject]@{ Path = 'docs/operations/admin-web-feature-folder-structure.md'; Terms = @('feature-source', 'features/connectors', 'features/governance') },
    [pscustomobject]@{ Path = 'src/Admin/Migration.Admin.Web/src/features/README.md'; Terms = @('canonical feature-grouping root', 'apps/migration-admin-ui') },
    [pscustomobject]@{ Path = 'tools/runtime/New-P102AdminWebFeatureStructureReport.ps1'; Terms = @('Feature-source folders', 'Canonical feature folders') }
)

foreach ($check in $checks) {
    $pathProperty = $check.PSObject.Properties['Path']
    $termsProperty = $check.PSObject.Properties['Terms']
    if ($null -eq $pathProperty -or $null -eq $termsProperty) {
        throw 'Validator check entry is malformed.'
    }

    $relativePath = [string]$pathProperty.Value
    if ([string]::IsNullOrWhiteSpace($relativePath)) {
        throw 'Validator check entry has an empty Path.'
    }

    $fullPath = Join-RepoPath -RelativePath $relativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw ('Required file is missing: {0}' -f $relativePath)
    }

    $text = Get-Content -LiteralPath $fullPath -Raw
    foreach ($term in @($termsProperty.Value)) {
        $termText = [string]$term
        if ([string]::IsNullOrWhiteSpace($termText)) {
            throw ('Validator check entry has an empty expected term for file: {0}' -f $relativePath)
        }
        if ($text.IndexOf($termText, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
            throw ('File {0} is missing expected term: {1}' -f $relativePath, $termText)
        }
    }
}

Write-Host 'P10.2Z Admin Web feature folder structure validation passed.'
