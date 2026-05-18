param(
    [string]$BaseUrl = "https://localhost:55436",
    [string]$DatabaseName = "MigrationOperationalStore",
    [string]$SqlInstance = "(localdb)\MSSQLLocalDB"
)

$ErrorActionPreference = "Stop"

Write-Host "Step 1: Create database"
./scripts/operational-sql-localdb-create-database.ps1 `
    -DatabaseName $DatabaseName `
    -SqlInstance $SqlInstance

Write-Host "Step 2: Apply schema"
./scripts/operational-sql-localdb-apply-schema.ps1 `
    -DatabaseName $DatabaseName `
    -SqlInstance $SqlInstance

Write-Host "Step 3: Run Admin API smoke test"
./scripts/operational-sql-schema-smoke-test.ps1 `
    -BaseUrl $BaseUrl
