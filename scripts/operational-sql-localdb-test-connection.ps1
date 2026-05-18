param(
    [string]$DatabaseName = "MigrationOperationalStore",
    [string]$SqlInstance = "(localdb)\MSSQLLocalDB"
)

$ErrorActionPreference = "Stop"

Write-Host "Testing SQL connection..."
Write-Host "Instance: $SqlInstance"
Write-Host "Database: $DatabaseName"

sqlcmd `
    -S $SqlInstance `
    -d $DatabaseName `
    -Q "SELECT DB_NAME() AS DatabaseName, SYSDATETIMEOFFSET() AS ServerTime;"

Write-Host "SQL connection test completed."
