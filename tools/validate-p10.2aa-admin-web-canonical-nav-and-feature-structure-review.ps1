[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Resolve-ValidatorRoot {
    if (-not [string]::IsNullOrWhiteSpace($PSScriptRoot)) {
        return $PSScriptRoot
    }

    if (-not [string]::IsNullOrWhiteSpace($PSCommandPath)) {
        return (Split-Path -Parent $PSCommandPath)
    }

    throw 'Unable to resolve validator root.'
}

function Join-RepoPath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $RepoRoot,

        [Parameter(Mandatory = $true)]
        [string] $RelativePath
    )

    $normalized = $RelativePath.Replace('/', [System.IO.Path]::DirectorySeparatorChar)
    return [System.IO.Path]::Combine($RepoRoot, $normalized)
}

$validatorRoot = Resolve-ValidatorRoot
$repoRoot = Split-Path -Parent $validatorRoot

$requiredFiles = @(
    'docs/p10/P10.2AA-Admin-Web-Canonical-Nav-And-Feature-Structure-Review.md',
    'docs/operations/admin-web-canonical-nav-and-feature-structure-review.md',
    'config-samples/p10-admin-web-canonical-nav-and-feature-structure-review.sample.json',
    'tools/runtime/New-P102AdminWebCanonicalNavAndFeatureStructureReport.ps1'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-RepoPath -RepoRoot $repoRoot -RelativePath $relativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw ('Required P10.2AA file is missing: {0}' -f $relativePath)
    }
}

$scriptRelativePath = 'tools/runtime/New-P102AdminWebCanonicalNavAndFeatureStructureReport.ps1'
$scriptPath = Join-RepoPath -RepoRoot $repoRoot -RelativePath $scriptRelativePath
$scriptText = Get-Content -LiteralPath $scriptPath -Raw
$parseErrors = $null
[System.Management.Automation.PSParser]::Tokenize($scriptText, [ref]$parseErrors) | Out-Null

if ($null -ne $parseErrors -and @($parseErrors).Count -gt 0) {
    $message = (@($parseErrors) | ForEach-Object { $_.Message }) -join '; '
    throw ('PowerShell parser errors in {0}: {1}' -f $scriptRelativePath, $message)
}

$fragileInvocationPattern = '$' + 'MyInvocation' + '.ScriptName'
if ($scriptText.IndexOf($fragileInvocationPattern, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
    throw ('Avoid fragile invocation-root usage in {0}' -f $scriptRelativePath)
}

if ($scriptText.IndexOf('.PackageReference', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
    $scriptText.IndexOf('.None', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
    $scriptText.IndexOf('.ItemGroup', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
    throw ('Potential StrictMode-unsafe XML property access in {0}' -f $scriptRelativePath)
}

$checks = @(
    [pscustomobject]@{
        Path = 'docs/p10/P10.2AA-Admin-Web-Canonical-Nav-And-Feature-Structure-Review.md'
        Terms = @('src/Admin/Migration.Admin.Web', 'apps/migration-admin-ui', 'feature-folder')
    },
    [pscustomobject]@{
        Path = 'docs/operations/admin-web-canonical-nav-and-feature-structure-review.md'
        Terms = @('canonical UI', 'feature-source', 'src/Admin/Migration.Admin.Web')
    },
    [pscustomobject]@{
        Path = 'config-samples/p10-admin-web-canonical-nav-and-feature-structure-review.sample.json'
        Terms = @('canonicalAdminUiPath', 'featureSourcePath', 'featureFamilies')
    },
    [pscustomobject]@{
        Path = 'tools/runtime/New-P102AdminWebCanonicalNavAndFeatureStructureReport.ps1'
        Terms = @('duplicate navigation routes', 'featureFamilies', 'src/pages')
    }
)

foreach ($check in $checks) {
    $pathProperty = $check.PSObject.Properties['Path']
    $termsProperty = $check.PSObject.Properties['Terms']

    if ($null -eq $pathProperty -or $null -eq $termsProperty) {
        throw 'Validator check entry is malformed.'
    }

    $relativePath = [string]$pathProperty.Value
    $fullPath = Join-RepoPath -RepoRoot $repoRoot -RelativePath $relativePath

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

$configPath = Join-RepoPath -RepoRoot $repoRoot -RelativePath 'config-samples/p10-admin-web-canonical-nav-and-feature-structure-review.sample.json'
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json

foreach ($propertyName in @('canonicalAdminUiPath', 'featureSourcePath', 'featureFamilies')) {
    if ($null -eq $config.PSObject.Properties[$propertyName]) {
        throw ('Configuration is missing property: {0}' -f $propertyName)
    }
}

if ([string]$config.canonicalAdminUiPath -ne 'src/Admin/Migration.Admin.Web') {
    throw 'canonicalAdminUiPath must remain src/Admin/Migration.Admin.Web.'
}

if ([string]$config.featureSourcePath -ne 'apps/migration-admin-ui') {
    throw 'featureSourcePath must remain apps/migration-admin-ui.'
}

Write-Host 'P10.2AA Admin Web canonical navigation and feature structure review validation passed.'
