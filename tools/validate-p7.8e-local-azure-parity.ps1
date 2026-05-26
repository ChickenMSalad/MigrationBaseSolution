[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

function Get-RepoRoot {
    $scriptRoot = $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
        $scriptRoot = (Get-Location).Path
    }

    $candidate = Split-Path -Path $scriptRoot -Parent
    while (-not [string]::IsNullOrWhiteSpace($candidate)) {
        if (Test-Path -LiteralPath (Join-Path $candidate "MigrationBaseSolution.sln") -PathType Leaf) {
            return $candidate
        }
        $parent = Split-Path -Path $candidate -Parent
        if ($parent -eq $candidate) { break }
        $candidate = $parent
    }

    return (Get-Location).Path
}

function Assert-FileExists {
    param([Parameter(Mandatory = $true)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Required file missing: $Path"
    }
}

function Assert-NoForbiddenText {
    param([Parameter(Mandatory = $true)][string]$Path)

    $text = Get-Content -LiteralPath $Path -Raw
    $forbidden = @(
        'MigrationWorkItems";',
        'SchemaName { get; set; } = "dbo"',
        'WorkItemsTableName { get; set; } = "MigrationWorkItems"'
    )

    foreach ($needle in $forbidden) {
        if ($text.Contains($needle)) {
            throw "Forbidden active-runtime default text found in $Path : $needle"
        }
    }
}

$repoRoot = Get-RepoRoot
$requiredRelativePaths = @(
    "docs/p7/P7.8E-Local-Azure-Parity-Drift-Checks.md",
    "config-samples/runtime-parity-baseline.sample.json",
    "tools/runtime/Export-RuntimeEnvironmentSnapshot.ps1",
    "tools/runtime/Compare-RuntimeEnvironmentSnapshot.ps1",
    "tools/runtime/New-RuntimeParityReport.ps1",
    "tools/runtime/Test-RuntimeRepoCloudParity.ps1"
)

foreach ($relativePath in $requiredRelativePaths) {
    Assert-FileExists -Path (Join-Path $repoRoot $relativePath)
}

$runtimeScripts = @(
    "tools/runtime/Export-RuntimeEnvironmentSnapshot.ps1",
    "tools/runtime/Compare-RuntimeEnvironmentSnapshot.ps1",
    "tools/runtime/New-RuntimeParityReport.ps1",
    "tools/runtime/Test-RuntimeRepoCloudParity.ps1"
)

foreach ($relativePath in $runtimeScripts) {
    $fullPath = Join-Path $repoRoot $relativePath
    Assert-NoForbiddenText -Path $fullPath
}

$samplePath = Join-Path $repoRoot "config-samples/runtime-parity-baseline.sample.json"
$sampleJson = Get-Content -LiteralPath $samplePath -Raw | ConvertFrom-Json
if ($null -eq $sampleJson.PSObject.Properties["sqlContract"]) {
    throw "runtime-parity-baseline.sample.json is missing sqlContract."
}
if ($sampleJson.sqlContract.schema -ne "migration") {
    throw "runtime-parity-baseline.sample.json sqlContract.schema must be migration."
}
if ($sampleJson.sqlContract.workItemsTable -ne "WorkItems") {
    throw "runtime-parity-baseline.sample.json sqlContract.workItemsTable must be WorkItems."
}

Write-Host "P7.8E local/Azure parity drop-in validation passed."
