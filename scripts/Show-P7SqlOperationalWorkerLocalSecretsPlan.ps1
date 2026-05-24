Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

Write-Host 'Recommended local Development configuration:'
Write-Host ''
Write-Host 'ConnectionStrings:MigrationOperationalStore = <your local SQL connection string>'
Write-Host 'SqlOperationalWorker:Enabled = true'
Write-Host 'SqlOperationalWorker:WorkerId = local-dev-sql-operational-worker-01'
Write-Host 'SqlOperationalWorker:BatchSize = 10'
Write-Host 'SqlOperationalWorker:LeaseSeconds = 300'
Write-Host 'SqlOperationalWorker:CompleteNoOpWorkItems = true'
Write-Host 'SqlOperationalMigrationJobExecutor:Enabled = false'
Write-Host ''
Write-Host 'After smoke validation, set CompleteNoOpWorkItems=false before enabling SqlOperationalMigrationJobExecutor=true.'
