[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Dispatcher', 'Executor')]
    [string] $Role,

    [Parameter(Mandatory = $false)]
    [string] $OutputPath
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$lines = New-Object System.Collections.Generic.List[string]
[void]$lines.Add('az webapp config appsettings set `')
[void]$lines.Add('  --resource-group $resourceGroup `')
if ($Role -eq 'Dispatcher') {
    [void]$lines.Add('  --name $dispatcherApp `')
}
else {
    [void]$lines.Add('  --name $executorApp `')
}
[void]$lines.Add('  --settings `')
[void]$lines.Add('    "ConnectionStrings__MigrationOperationalStore=$sqlConnectionString" `')
[void]$lines.Add('    "SqlOperationalRuntimeReadiness__ConnectionString=$sqlConnectionString" `')
[void]$lines.Add('    "OpenTelemetry__EnableTracing=true" `')
[void]$lines.Add('    "OpenTelemetry__EnableAzureMonitorExporter=false" `')

if ($Role -eq 'Dispatcher') {
    [void]$lines.Add('    "SqlServiceBusDispatcher__Enabled=true" `')
    [void]$lines.Add('    "SqlServiceBusDispatcher__SqlConnectionString=$sqlConnectionString" `')
    [void]$lines.Add('    "SqlServiceBusDispatcher__ServiceBusConnectionString=$serviceBusConnection" `')
    [void]$lines.Add('    "SqlServiceBusDispatcher__QueueName=$serviceBusQueue"')
}
else {
    [void]$lines.Add('    "SqlServiceBusExecutor__ServiceBusConnectionString=$serviceBusConnection" `')
    [void]$lines.Add('    "SqlServiceBusExecutor__QueueName=$serviceBusQueue" `')
    [void]$lines.Add('    "SqlOperationalMigrationJobExecutor__Enabled=true" `')
    [void]$lines.Add('    "SqlOperationalWorkItemQueue__SchemaName=migration" `')
    [void]$lines.Add('    "SqlOperationalWorkItemQueue__WorkItemsTableName=WorkItems" `')
    [void]$lines.Add('    "GenericMigrationRuntime__RegisterAllWhenEmpty=true"')
}

$text = ($lines -join [Environment]::NewLine)
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $text
}
else {
    $parent = Split-Path -Parent $OutputPath
    if (-not [string]::IsNullOrWhiteSpace($parent) -and -not (Test-Path -LiteralPath $parent)) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }
    Set-Content -LiteralPath $OutputPath -Value $text -Encoding UTF8
    Write-Host "Wrote command to $OutputPath"
}
