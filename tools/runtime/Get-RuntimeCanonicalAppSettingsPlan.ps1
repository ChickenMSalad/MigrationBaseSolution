[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $SettingsPath,

    [Parameter(Mandatory = $true)]
    [ValidateSet('Dispatcher', 'Executor')]
    [string] $Role,

    [Parameter(Mandatory = $false)]
    [string] $OutputPath
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Resolve-FullPath {
    param([Parameter(Mandatory = $true)][string] $Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return (Join-Path (Get-Location).Path $Path)
}

function Read-AppSettings {
    param([Parameter(Mandatory = $true)][string] $Path)

    $fullPath = Resolve-FullPath -Path $Path
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw ('Settings file not found: {0}' -f $fullPath)
    }

    $raw = Get-Content -LiteralPath $fullPath -Raw
    if ([string]::IsNullOrWhiteSpace($raw)) {
        throw ('Settings file is empty: {0}' -f $fullPath)
    }

    return ConvertFrom-Json -InputObject $raw
}

function New-SettingLookup {
    param([Parameter(Mandatory = $true)] $Settings)

    $lookup = @{}
    foreach ($setting in @($Settings)) {
        if ($null -ne $setting -and $setting.PSObject.Properties.Name -contains 'name') {
            $name = [string]$setting.name
            if (-not [string]::IsNullOrWhiteSpace($name)) {
                $lookup[$name] = $setting
            }
        }
    }

    return $lookup
}

$settings = Read-AppSettings -Path $SettingsPath
$lookup = New-SettingLookup -Settings $settings

$canonicalCommon = @(
    'ConnectionStrings__MigrationOperationalStore',
    'OpenTelemetry__EnableTracing',
    'OpenTelemetry__EnableAzureMonitorExporter'
)

if ($Role -eq 'Dispatcher') {
    $canonicalRole = @(
        'SqlServiceBusDispatcher__Enabled',
        'SqlServiceBusDispatcher__SqlConnectionString',
        'SqlServiceBusDispatcher__ServiceBusConnectionString',
        'SqlServiceBusDispatcher__QueueName'
    )
}
else {
    $canonicalRole = @(
        'SqlServiceBusExecutor__ServiceBusConnectionString',
        'SqlServiceBusExecutor__QueueName',
        'SqlOperationalWorkItemQueue__SchemaName',
        'SqlOperationalWorkItemQueue__WorkItemsTableName',
        'SqlOperationalMigrationJobExecutor__Enabled',
        'GenericMigrationRuntime__RegisterAllWhenEmpty'
    )
}

$canonicalKeys = @($canonicalCommon + $canonicalRole)

$legacyPatterns = @(
    '^MIGRATION_ConnectionStrings__MigrationOperationStore$',
    '^MIGRATION_SqlWorkItemDispatcher__',
    '^MIGRATION_ServiceBusDispatcher__',
    '^MIGRATION_SqlServiceBusExecutor__',
    '^MIGRATION_SqlOperationalWorkItemQueue__',
    '^MIGRATION_SqlOperationalMigrationJobExecutor__',
    '^OperationalStore__Sql__'
)

$transitionalPatterns = @(
    '^MIGRATION_ConnectionStrings__MigrationOperationalStore$',
    '^MIGRATION_OpenTelemetry__',
    '^MIGRATION_ServiceBusDispatcher__Enabled$',
    '^MIGRATION_ServiceBusExecutor__Enabled$',
    '^MIGRATION_SqlOperationalWorker__Enabled$',
    '^MIGRATION_SqlOperationalRuntimeReadiness__ConnectionString$',
    '^SqlOperationalRuntimeReadiness__ConnectionString$'
)

$missingCanonical = @()
foreach ($key in $canonicalKeys) {
    if (-not $lookup.ContainsKey($key)) {
        $missingCanonical += $key
    }
}

$legacyKeys = @()
$transitionalKeys = @()
foreach ($name in @($lookup.Keys | Sort-Object)) {
    foreach ($pattern in $legacyPatterns) {
        if ($name -match $pattern) {
            $legacyKeys += $name
            break
        }
    }

    foreach ($pattern in $transitionalPatterns) {
        if ($name -match $pattern) {
            $transitionalKeys += $name
            break
        }
    }
}

$plan = [ordered]@{
    role = $Role
    sourcePath = (Resolve-FullPath -Path $SettingsPath)
    generatedUtc = [DateTimeOffset]::UtcNow.ToString('O')
    canonicalKeys = @($canonicalKeys | Sort-Object)
    missingCanonicalKeys = @($missingCanonical | Sort-Object)
    legacyKeysToReviewForDeletion = @($legacyKeys | Sort-Object -Unique)
    transitionalKeysToReviewForDeletion = @($transitionalKeys | Sort-Object -Unique)
    totalSettingCount = @($lookup.Keys).Count
}

$json = ConvertTo-Json -InputObject $plan -Depth 10

if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
    $outputFullPath = Resolve-FullPath -Path $OutputPath
    $parent = Split-Path -Parent $outputFullPath
    if (-not [string]::IsNullOrWhiteSpace($parent) -and -not (Test-Path -LiteralPath $parent)) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }
    Set-Content -LiteralPath $outputFullPath -Value $json -Encoding UTF8
    Write-Host ('Runtime AppSettings canonicalization plan written to {0}' -f $outputFullPath)
}
else {
    $json
}
