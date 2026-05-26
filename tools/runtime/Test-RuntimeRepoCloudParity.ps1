[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$SnapshotPath,

    [Parameter(Mandatory = $false)]
    [string]$RepoRoot
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

function Resolve-FullPath {
    param([Parameter(Mandatory = $true)][string]$Path)
    if ([System.IO.Path]::IsPathRooted($Path)) { return $Path }
    return (Join-Path (Get-Location).Path $Path)
}

function Find-RepoRoot {
    param([string]$StartingPath)

    if (-not [string]::IsNullOrWhiteSpace($StartingPath)) {
        $candidate = Resolve-FullPath -Path $StartingPath
        if (-not (Test-Path -LiteralPath $candidate -PathType Container)) {
            throw "RepoRoot does not exist: $candidate"
        }
        return $candidate
    }

    $current = (Get-Location).Path
    while (-not [string]::IsNullOrWhiteSpace($current)) {
        if (Test-Path -LiteralPath (Join-Path $current "MigrationBaseSolution.sln") -PathType Leaf) {
            return $current
        }
        $parent = Split-Path -Path $current -Parent
        if ($parent -eq $current) { break }
        $current = $parent
    }

    throw "Could not locate repo root. Run from the repository or pass -RepoRoot."
}

function Get-MapValue {
    param($Map, [string]$Name)
    if ($null -eq $Map) { return $null }
    $property = $Map.PSObject.Properties[$Name]
    if ($null -eq $property) { return $null }
    return $property.Value
}

$snapshotFullPath = Resolve-FullPath -Path $SnapshotPath
if (-not (Test-Path -LiteralPath $snapshotFullPath -PathType Leaf)) {
    throw "Snapshot file not found: $snapshotFullPath"
}
$snapshot = (Get-Content -LiteralPath $snapshotFullPath -Raw) | ConvertFrom-Json
$repo = Find-RepoRoot -StartingPath $RepoRoot
$errors = New-Object System.Collections.ArrayList
$warnings = New-Object System.Collections.ArrayList

$executorSettings = $snapshot.executorAppSettings
$dispatcherSettings = $snapshot.dispatcherAppSettings

if ((Get-MapValue -Map $executorSettings -Name "SqlOperationalWorkItemQueue__SchemaName") -ne "migration") {
    [void]$errors.Add("Executor setting SqlOperationalWorkItemQueue__SchemaName must be migration.")
}

if ((Get-MapValue -Map $executorSettings -Name "SqlOperationalWorkItemQueue__WorkItemsTableName") -ne "WorkItems") {
    [void]$errors.Add("Executor setting SqlOperationalWorkItemQueue__WorkItemsTableName must be WorkItems.")
}

if ((Get-MapValue -Map $executorSettings -Name "SqlOperationalMigrationJobExecutor__Enabled") -ne "true") {
    [void]$warnings.Add("Executor setting SqlOperationalMigrationJobExecutor__Enabled is not true.")
}

if ((Get-MapValue -Map $dispatcherSettings -Name "SqlServiceBusDispatcher__Enabled") -ne "true") {
    [void]$warnings.Add("Dispatcher setting SqlServiceBusDispatcher__Enabled is not true.")
}

$schemaText = [string]$snapshot.sqlSchemaText
if (-not $schemaText.Contains("WorkItems")) {
    [void]$errors.Add("SQL snapshot does not contain migration.WorkItems evidence.")
}
if (-not $schemaText.Contains("ManifestRows")) {
    [void]$errors.Add("SQL snapshot does not contain migration.ManifestRows evidence.")
}
if (-not $schemaText.Contains("bigint")) {
    [void]$errors.Add("SQL snapshot does not contain bigint evidence for runtime identifiers.")
}

$queueOptionsPath = Join-Path $repo "src/Core/Migration.Infrastructure.Sql/Operational/WorkItems/SqlOperationalWorkItemQueueOptions.cs"
if (Test-Path -LiteralPath $queueOptionsPath -PathType Leaf) {
    $optionsText = Get-Content -LiteralPath $queueOptionsPath -Raw
    if ($optionsText -match 'SchemaName\s*\{[^\r\n]*\}\s*=\s*"dbo"') {
        [void]$errors.Add("SqlOperationalWorkItemQueueOptions default SchemaName still appears to be dbo.")
    }
    if ($optionsText -match 'WorkItemsTableName\s*\{[^\r\n]*\}\s*=\s*"MigrationWorkItems"') {
        [void]$errors.Add("SqlOperationalWorkItemQueueOptions default WorkItemsTableName still appears to be MigrationWorkItems.")
    }
}
else {
    [void]$warnings.Add("Could not find SqlOperationalWorkItemQueueOptions.cs at expected repo path.")
}

foreach ($warning in $warnings) {
    Write-Warning $warning
}

if ($errors.Count -gt 0) {
    foreach ($errorItem in $errors) {
        Write-Error $errorItem
    }
    throw "Runtime repo/cloud parity validation failed with $($errors.Count) error(s)."
}

Write-Host "Runtime repo/cloud parity validation passed. Warnings: $($warnings.Count)."
