param(
    [string]$DatabaseName = "MigrationOperationalStore",
    [string]$SqlInstance = "(localdb)\MSSQLLocalDB"
)

$ErrorActionPreference = "Stop"

$sql = @"
IF DB_ID(N'$DatabaseName') IS NULL
BEGIN
    CREATE DATABASE [$DatabaseName];
END
"@

Write-Host "Creating LocalDB database if missing..."
Write-Host "Instance: $SqlInstance"
Write-Host "Database: $DatabaseName"

sqlcmd `
    -S $SqlInstance `
    -d master `
    -Q $sql

Write-Host "Database exists or was created: $DatabaseName"
