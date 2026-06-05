param(
    [string]$DatabaseName = "MigrationOperationalStore",
    [string]$SqlInstance = "(localdb)\MSSQLLocalDB",
    [string]$SchemaScriptPath = "src\Migration.Infrastructure\State\OperationalStore\Sql\Scripts\001_CreateOperationalStore.sql"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $SchemaScriptPath)) {
    throw "Could not find schema script: $SchemaScriptPath"
}

Write-Host "Applying operational SQL schema..."
Write-Host "Instance: $SqlInstance"
Write-Host "Database: $DatabaseName"
Write-Host "Script: $SchemaScriptPath"

sqlcmd `
    -S $SqlInstance `
    -d $DatabaseName `
    -i $SchemaScriptPath

Write-Host "Operational SQL schema applied."
