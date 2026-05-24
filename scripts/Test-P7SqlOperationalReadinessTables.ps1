Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

param(
    [Parameter(Mandatory = $true)]
    [string]$ConnectionString
)

Add-Type -AssemblyName System.Data

$requiredTables = @(
    'MigrationProjects',
    'MigrationRuns',
    'MigrationManifestRows',
    'MigrationWorkItems',
    'MigrationFailures',
    'MigrationRunCheckpoints',
    'MigrationAssetMappings'
)

$connection = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
try {
    $connection.Open()
    foreach ($table in $requiredTables) {
        $command = $connection.CreateCommand()
        $command.CommandText = "select case when object_id(@ObjectName, 'U') is null then 0 else 1 end;"
        $parameter = $command.Parameters.Add('@ObjectName', [System.Data.SqlDbType]::NVarChar, 512)
        $parameter.Value = "[migration].[$table]"
        $exists = [int]$command.ExecuteScalar()
        if ($exists -ne 1) {
            throw "Required table missing: migration.$table"
        }
        Write-Host "Found migration.$table"
    }
}
finally {
    $connection.Dispose()
}

Write-Host 'P7 SQL operational readiness tables are present.'
