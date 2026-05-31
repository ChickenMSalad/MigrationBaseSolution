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
        [ValidateNotNullOrEmpty()]
        [string] $Root,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $RelativePath
    )

    $normalized = $RelativePath.Replace('/', [System.IO.Path]::DirectorySeparatorChar)
    return [System.IO.Path]::Combine($Root, $normalized)
}

$toolRoot = Resolve-ValidatorRoot
$repoRoot = Split-Path -Parent $toolRoot

$requiredFiles = @(
    'docs/p10/P10.2F-Admin-UI-Feature-Migration-Matrix.md',
    'docs/operations/admin-ui-feature-migration-matrix.md',
    'config-samples/p10-admin-ui-feature-migration-matrix.sample.json',
    'tools/runtime/New-P102AdminUiFeatureMigrationMatrix.ps1'
)

foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-RepoPath -Root $repoRoot -RelativePath $relativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw ('Required P10.2F file is missing: {0}' -f $relativePath)
    }
}

$scriptsToParse = @(
    'tools/runtime/New-P102AdminUiFeatureMigrationMatrix.ps1'
)

foreach ($relativeScript in $scriptsToParse) {
    $scriptPath = Join-RepoPath -Root $repoRoot -RelativePath $relativeScript
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

    $badInvocationPattern = '$' + 'MyInvocation' + '.ScriptName'
    if ($scriptText.IndexOf($badInvocationPattern, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw ('Avoid fragile invocation-root usage in {0}' -f $relativeScript)
    }
}

$configPath = Join-RepoPath -Root $repoRoot -RelativePath 'config-samples/p10-admin-ui-feature-migration-matrix.sample.json'
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
foreach ($propertyName in @('canonicalAdminUiPath', 'featureSourcePath', 'outputDirectory', 'featureFamilies')) {
    if ($null -eq $config.PSObject.Properties[$propertyName]) {
        throw ('P10.2F config is missing property: {0}' -f $propertyName)
    }
}

if ([string]$config.canonicalAdminUiPath -ne 'src/Admin/Migration.Admin.Web') {
    throw 'P10.2F canonicalAdminUiPath must remain src/Admin/Migration.Admin.Web.'
}

if ([string]$config.featureSourcePath -ne 'apps/migration-admin-ui') {
    throw 'P10.2F featureSourcePath must remain apps/migration-admin-ui.'
}

if (@($config.featureFamilies).Count -lt 5) {
    throw 'P10.2F must define at least five Admin UI feature families.'
}

$docsToCheck = @(
    'docs/p10/P10.2F-Admin-UI-Feature-Migration-Matrix.md',
    'docs/operations/admin-ui-feature-migration-matrix.md'
)

foreach ($relativeDoc in $docsToCheck) {
    $docPath = Join-RepoPath -Root $repoRoot -RelativePath $relativeDoc
    $text = Get-Content -LiteralPath $docPath -Raw
    foreach ($term in @('src/Admin/Migration.Admin.Web', 'apps/migration-admin-ui')) {
        if ($text.IndexOf($term, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
            throw ('Documentation file is missing consolidation term {0}: {1}' -f $term, $relativeDoc)
        }
    }
}

Write-Host 'P10.2F Admin UI feature migration matrix validation passed.'
