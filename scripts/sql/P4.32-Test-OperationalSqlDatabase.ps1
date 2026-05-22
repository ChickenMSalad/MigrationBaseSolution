[CmdletBinding()]
param(
    [string]$ServerInstance = "(localdb)\MSSQLLocalDB",
    [string]$DatabaseName = "MigrationBaseSolution_Operational"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host ("[P4.32-SQL-TEST] {0}" -f $Message)
}

function Invoke-SqlScalar {
    param(
        [string]$ConnectionString,
        [string]$CommandText
    )

    $connection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandTimeout = 120
        $command.CommandText = $CommandText
        return $command.ExecuteScalar()
    }
    finally {
        $connection.Dispose()
    }
}

$connectionString = "Server=$ServerInstance;Database=$DatabaseName;Trusted_Connection=True;TrustServerCertificate=True;"

$expectedTables = @(
    "MigrationProjects",
    "MigrationRuns",
    "MigrationManifestRows",
    "MigrationWorkItems",
    "MigrationFailures",
    "MigrationRunCheckpoints",
    "MigrationAssetMappings",
    "MigrationConnectorRegistrations"
)

foreach ($table in $expectedTables) {
    $sql = "SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = '$table';"
    $count = Invoke-SqlScalar -ConnectionString $connectionString -CommandText $sql

    if ([int]$count -ne 1) {
        throw ("Expected table not found: dbo.{0}" -f $table)
    }

    Write-Step ("Verified dbo.{0}" -f $table)
}

Write-Step "Operational SQL database validation passed."
