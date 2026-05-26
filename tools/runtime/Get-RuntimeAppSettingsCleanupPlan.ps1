[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $Path,

    [Parameter(Mandatory = $true)]
    [ValidateSet('Dispatcher', 'Executor')]
    [string] $Role
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Read-JsonFile {
    param([Parameter(Mandatory = $true)][string] $InputPath)

    if (-not (Test-Path -LiteralPath $InputPath)) {
        throw "Appsettings JSON file was not found: $InputPath"
    }

    $raw = Get-Content -LiteralPath $InputPath -Raw
    if ([string]::IsNullOrWhiteSpace($raw)) {
        throw "Appsettings JSON file is empty: $InputPath"
    }

    return $raw | ConvertFrom-Json
}

function Get-SettingNames {
    param([Parameter(Mandatory = $true)] $Items)

    $names = New-Object System.Collections.Generic.List[string]
    foreach ($item in @($Items)) {
        if ($null -ne $item -and $item.PSObject.Properties['name']) {
            $value = [string]$item.name
            if (-not [string]::IsNullOrWhiteSpace($value)) {
                [void]$names.Add($value)
            }
        }
    }
    return $names.ToArray()
}

$sharedCanonical = @(
    'ConnectionStrings__MigrationOperationalStore',
    'SqlOperationalRuntimeReadiness__ConnectionString',
    'OpenTelemetry__EnableTracing',
    'OpenTelemetry__EnableAzureMonitorExporter'
)

$dispatcherCanonical = @(
    'SqlServiceBusDispatcher__Enabled',
    'SqlServiceBusDispatcher__SqlConnectionString',
    'SqlServiceBusDispatcher__ServiceBusConnectionString',
    'SqlServiceBusDispatcher__QueueName'
)

$executorCanonical = @(
    'SqlServiceBusExecutor__ServiceBusConnectionString',
    'SqlServiceBusExecutor__QueueName',
    'SqlOperationalMigrationJobExecutor__Enabled',
    'SqlOperationalWorkItemQueue__SchemaName',
    'SqlOperationalWorkItemQueue__WorkItemsTableName',
    'GenericMigrationRuntime__RegisterAllWhenEmpty',
    'GenericMigrationRuntime__EnabledManifests__0',
    'GenericMigrationRuntime__EnabledSources__0',
    'GenericMigrationRuntime__EnabledTargets__0'
)

$stalePrefixes = @(
    'MIGRATION_ServiceBusDispatcher__',
    'MIGRATION_ServiceBusExecutor__',
    'MIGRATION_SqlWorkItemDispatcher__',
    'MIGRATION_SqlServiceBusExecutor__',
    'MIGRATION_SqlOperationalWorkItemQueue__',
    'OperationalStore__Sql__'
)

$staleExact = @(
    'MIGRATION_ConnectionStrings__MigrationOperationStore'
)

$expected = New-Object System.Collections.Generic.HashSet[string]
foreach ($name in $sharedCanonical) { [void]$expected.Add($name) }
if ($Role -eq 'Dispatcher') {
    foreach ($name in $dispatcherCanonical) { [void]$expected.Add($name) }
}
else {
    foreach ($name in $executorCanonical) { [void]$expected.Add($name) }
}

$items = Read-JsonFile -InputPath $Path
$names = Get-SettingNames -Items $items

$missing = @()
foreach ($name in $expected) {
    if ($names -notcontains $name) {
        $missing += $name
    }
}

$stale = @()
foreach ($name in $names) {
    if ($staleExact -contains $name) {
        $stale += $name
        continue
    }

    foreach ($prefix in $stalePrefixes) {
        if ($name.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            $stale += $name
            break
        }
    }
}

$possiblyDuplicate = @()
foreach ($name in $names) {
    if ($name.StartsWith('MIGRATION_', [System.StringComparison]::OrdinalIgnoreCase)) {
        $possiblyDuplicate += $name
    }
}

[pscustomobject]@{
    Role = $Role
    Path = (Resolve-Path -LiteralPath $Path).Path
    CanonicalExpectedCount = $expected.Count
    CurrentSettingCount = $names.Count
    MissingCanonicalSettings = @($missing | Sort-Object)
    StaleSettingsToReview = @($stale | Sort-Object -Unique)
    MigrationPrefixedSettingsToReview = @($possiblyDuplicate | Sort-Object -Unique)
}
