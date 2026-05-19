param(
    [string]$Instance = "(localdb)\MSSQLLocalDB",
    [string]$Database = "MigrationOperationalStore"
)

$ErrorActionPreference = "Stop"

$scriptPath = "src\Migration.Admin.Api\OperationalStore\Sql\Scripts\002_CreateDispatcherExecutions.sql"

if (-not (Test-Path $scriptPath)) {
    throw "Could not find $scriptPath"
}

Write-Host "Applying dispatcher execution history SQL schema..."
Write-Host "Instance: $Instance"
Write-Host "Database: $Database"
Write-Host "Script: $scriptPath"

sqlcmd -S $Instance -d $Database -i $scriptPath

Write-Host "Dispatcher execution history SQL schema applied."
