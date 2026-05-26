#Requires -Version 5.1
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $DispatcherSettingsPath,

    [Parameter(Mandatory = $true)]
    [string] $ExecutorSettingsPath
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$script:Failures = New-Object System.Collections.Generic.List[string]
$script:Warnings = New-Object System.Collections.Generic.List[string]

function Read-SettingsFile {
    param([Parameter(Mandatory = $true)][string] $Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Settings file not found: $Path"
    }

    $content = Get-Content -LiteralPath $Path -Raw
    if ([string]::IsNullOrWhiteSpace($content)) {
        throw "Settings file is empty: $Path"
    }

    return $content | ConvertFrom-Json
}

function Convert-ToMap {
    param([Parameter(Mandatory = $true)] $Settings)

    $map = @{}
    foreach ($item in @($Settings)) {
        if ($null -eq $item) { continue }
        $props = $item.PSObject.Properties
        if ($null -eq $props['name']) { continue }
        $name = [string]$item.name
        if (-not [string]::IsNullOrWhiteSpace($name)) {
            $value = $null
            if ($null -ne $props['value']) { $value = $item.value }
            $map[$name] = $value
        }
    }
    return $map
}

function Require-Key {
    param(
        [Parameter(Mandatory = $true)] [hashtable] $Map,
        [Parameter(Mandatory = $true)] [string] $Name,
        [Parameter(Mandatory = $true)] [string] $Owner
    )

    if (-not $Map.ContainsKey($Name)) {
        $script:Failures.Add("$Owner missing required setting '$Name'.")
        return
    }

    $value = [string]$Map[$Name]
    if ([string]::IsNullOrWhiteSpace($value)) {
        $script:Failures.Add("$Owner setting '$Name' is blank.")
    }
}

function Require-Value {
    param(
        [Parameter(Mandatory = $true)] [hashtable] $Map,
        [Parameter(Mandatory = $true)] [string] $Name,
        [Parameter(Mandatory = $true)] [string] $Expected,
        [Parameter(Mandatory = $true)] [string] $Owner
    )

    Require-Key -Map $Map -Name $Name -Owner $Owner
    if ($Map.ContainsKey($Name)) {
        $actual = [string]$Map[$Name]
        if ($actual -ne $Expected) {
            $script:Failures.Add("$Owner setting '$Name' expected '$Expected' but was '$actual'.")
        }
    }
}

function Warn-StaleKey {
    param(
        [Parameter(Mandatory = $true)] [hashtable] $Map,
        [Parameter(Mandatory = $true)] [string] $Name,
        [Parameter(Mandatory = $true)] [string] $Owner
    )

    if ($Map.ContainsKey($Name)) {
        $script:Warnings.Add("$Owner contains stale/duplicate setting '$Name'. Remove after canonical settings are confirmed.")
    }
}

$dispatcher = Convert-ToMap -Settings (Read-SettingsFile -Path $DispatcherSettingsPath)
$executor = Convert-ToMap -Settings (Read-SettingsFile -Path $ExecutorSettingsPath)

Require-Key -Map $dispatcher -Name 'ConnectionStrings__MigrationOperationalStore' -Owner 'Dispatcher'
Require-Value -Map $dispatcher -Name 'SqlServiceBusDispatcher__Enabled' -Expected 'true' -Owner 'Dispatcher'
Require-Key -Map $dispatcher -Name 'SqlServiceBusDispatcher__SqlConnectionString' -Owner 'Dispatcher'
Require-Key -Map $dispatcher -Name 'SqlServiceBusDispatcher__ServiceBusConnectionString' -Owner 'Dispatcher'
Require-Key -Map $dispatcher -Name 'SqlServiceBusDispatcher__QueueName' -Owner 'Dispatcher'

Require-Key -Map $executor -Name 'ConnectionStrings__MigrationOperationalStore' -Owner 'Executor'
Require-Key -Map $executor -Name 'SqlServiceBusExecutor__ServiceBusConnectionString' -Owner 'Executor'
Require-Key -Map $executor -Name 'SqlServiceBusExecutor__QueueName' -Owner 'Executor'
Require-Value -Map $executor -Name 'SqlOperationalWorkItemQueue__SchemaName' -Expected 'migration' -Owner 'Executor'
Require-Value -Map $executor -Name 'SqlOperationalWorkItemQueue__WorkItemsTableName' -Expected 'WorkItems' -Owner 'Executor'
Require-Value -Map $executor -Name 'SqlOperationalMigrationJobExecutor__Enabled' -Expected 'true' -Owner 'Executor'

$staleDispatcher = @(
    'MIGRATION_SqlWorkItemDispatcher__ServiceBusConnectionString',
    'MIGRATION_SqlWorkItemDispatcher__QueueName',
    'MIGRATION_ServiceBusDispatcher__Enabled',
    'MIGRATION_ServiceBusDispatcher__ServiceBusConnectionString',
    'MIGRATION_ServiceBusDispatcher__QueueName'
)
foreach ($name in $staleDispatcher) { Warn-StaleKey -Map $dispatcher -Name $name -Owner 'Dispatcher' }

$staleExecutor = @(
    'MIGRATION_ConnectionStrings__MigrationOperationStore',
    'MIGRATION_SqlServiceBusExecutor__ServiceBusConnectionString',
    'MIGRATION_SqlServiceBusExecutor__QueueName',
    'MIGRATION_SqlOperationalWorkItemQueue__SchemaName',
    'MIGRATION_SqlOperationalWorkItemQueue__WorkItemsTableName',
    'OperationalStore__Sql__SchemaName',
    'OperationalStore__Sql__WorkItemsTableName'
)
foreach ($name in $staleExecutor) { Warn-StaleKey -Map $executor -Name $name -Owner 'Executor' }

if ($script:Warnings.Count -gt 0) {
    Write-Warning 'Runtime app settings warnings:'
    foreach ($warning in $script:Warnings) { Write-Warning $warning }
}

if ($script:Failures.Count -gt 0) {
    Write-Error 'Runtime app settings validation failed.'
    foreach ($failure in $script:Failures) { Write-Error $failure }
    exit 1
}

Write-Host 'Runtime app settings validation passed.'
