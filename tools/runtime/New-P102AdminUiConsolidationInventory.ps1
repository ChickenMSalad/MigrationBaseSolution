[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string] $RepoRoot,

    [Parameter(Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string] $OutputPath = 'artifacts/admin-ui-consolidation/p10-admin-ui-consolidation-inventory.md'
)

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

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = Split-Path -Parent (Split-Path -Parent $scriptRoot)
}

$repoFullPath = (Resolve-Path -LiteralPath $RepoRoot).Path
$canonicalPath = [System.IO.Path]::Combine($repoFullPath, 'src', 'Admin', 'Migration.Admin.Web')
$featureSourcePath = [System.IO.Path]::Combine($repoFullPath, 'apps', 'migration-admin-ui')

foreach ($pathToCheck in @($canonicalPath, $featureSourcePath)) {
    if (-not (Test-Path -LiteralPath $pathToCheck)) {
        throw ('Required Admin UI path is missing: {0}' -f $pathToCheck)
    }
}

$outputFullPath = $OutputPath
if (-not [System.IO.Path]::IsPathRooted($outputFullPath)) {
    $outputFullPath = [System.IO.Path]::Combine($repoFullPath, $OutputPath)
}
$outputParent = Split-Path -Parent $outputFullPath
if (-not [string]::IsNullOrWhiteSpace($outputParent) -and -not (Test-Path -LiteralPath $outputParent)) {
    New-Item -ItemType Directory -Path $outputParent -Force | Out-Null
}

function Test-IsIgnoredPath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $FullName
    )

    $segments = @($FullName -split '[\\/]')
    foreach ($segment in $segments) {
        if ($segment -in @('node_modules', 'dist', 'build', '.git', '.vite', '.react-router')) {
            return $true
        }
    }
    return $false
}

function Get-RelativeFileList {
    param(
        [Parameter(Mandatory = $true)]
        [string] $RootPath
    )

    $files = Get-ChildItem -LiteralPath $RootPath -Recurse -File | Where-Object { -not (Test-IsIgnoredPath -FullName $_.FullName) }
    $result = @()
    foreach ($file in $files) {
        $relative = $file.FullName.Substring($RootPath.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
        $result += $relative.Replace([System.IO.Path]::DirectorySeparatorChar, '/')
    }
    return @($result | Sort-Object)
}

$canonicalFiles = @(Get-RelativeFileList -RootPath $canonicalPath)
$featureSourceFiles = @(Get-RelativeFileList -RootPath $featureSourcePath)

$featureHints = @(
    'OperationalRuntimeDashboard',
    'RunLaunchPanel',
    'ManifestImportPanel',
    'FailureRetryWorkspace',
    'CredentialVaultWorkspace',
    'ConnectorConfigurationWorkspace',
    'ExecutionSessionWorkspace',
    'OperationalEventTimelineWorkspace',
    'AuditTrailWorkspace',
    'ExecutionWorkerTelemetryWorkspace',
    'CommandCenterSummaryWorkspace',
    'CapacityForecastWorkspace',
    'CostAnalyticsWorkspace'
)

$matchedFeatureFiles = @()
foreach ($hint in $featureHints) {
    foreach ($file in $featureSourceFiles) {
        if ($file.IndexOf($hint, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
            $matchedFeatureFiles += $file
        }
    }
}
$matchedFeatureFiles = @($matchedFeatureFiles | Sort-Object -Unique)

$lines = @()
$lines += '# P10.2E Admin UI Consolidation Inventory'
$lines += ''
$lines += ('- Generated UTC: {0:o}' -f [DateTimeOffset]::UtcNow)
$lines += ('- Repository root: {0}' -f $repoFullPath)
$lines += '- Canonical Admin UI: `src/Admin/Migration.Admin.Web`'
$lines += '- Feature source/prototype: `apps/migration-admin-ui`'
$lines += ''
$lines += '## Counts'
$lines += ''
$lines += ('- Canonical Admin Web files: {0}' -f @($canonicalFiles).Count)
$lines += ('- Feature source files: {0}' -f @($featureSourceFiles).Count)
$lines += ('- High-value feature-source matches: {0}' -f @($matchedFeatureFiles).Count)
$lines += ''
$lines += '## High-value feature source candidates'
$lines += ''
if (@($matchedFeatureFiles).Count -eq 0) {
    $lines += '- None found by name hint.'
} else {
    foreach ($file in $matchedFeatureFiles) {
        $lines += ('- `{0}`' -f $file)
    }
}
$lines += ''
$lines += '## Rule'
$lines += ''
$lines += 'Future Admin UI feature work goes into `src/Admin/Migration.Admin.Web`. Use `apps/migration-admin-ui` as reference until migrated.'

$lines | Set-Content -LiteralPath $outputFullPath -Encoding UTF8
Write-Host ('Admin UI consolidation inventory written to {0}' -f $outputFullPath)
