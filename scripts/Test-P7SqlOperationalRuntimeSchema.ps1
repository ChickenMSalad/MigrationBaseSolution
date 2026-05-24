param(
    [Parameter(Mandatory = $true)]
    [string]$ConnectionString
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function New-SqlConnection {
    param([string]$ConnectionStringValue)

    $connection = New-Object System.Data.SqlClient.SqlConnection
    $connection.ConnectionString = $ConnectionStringValue
    $connection.Open()
    return $connection
}

function Invoke-Scalar {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [string]$Sql
    )

    $command = $Connection.CreateCommand()
    $command.CommandText = $Sql
    $command.CommandTimeout = 30
    try {
        return $command.ExecuteScalar()
    }
    finally {
        $command.Dispose()
    }
}

function Assert-TableExists {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [string]$SchemaName,
        [string]$TableName
    )

    $escapedSchema = $SchemaName.Replace("'", "''")
    $escapedTable = $TableName.Replace("'", "''")
    $sql = "select count(*) from INFORMATION_SCHEMA.TABLES where TABLE_SCHEMA = '$escapedSchema' and TABLE_NAME = '$escapedTable' and TABLE_TYPE = 'BASE TABLE'"
    $count = [int](Invoke-Scalar -Connection $Connection -Sql $sql)
    if ($count -ne 1) {
        throw "Required table missing: $SchemaName.$TableName"
    }
}

function Assert-ColumnExists {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [string]$SchemaName,
        [string]$TableName,
        [string]$ColumnName
    )

    $escapedSchema = $SchemaName.Replace("'", "''")
    $escapedTable = $TableName.Replace("'", "''")
    $escapedColumn = $ColumnName.Replace("'", "''")
    $sql = "select count(*) from INFORMATION_SCHEMA.COLUMNS where TABLE_SCHEMA = '$escapedSchema' and TABLE_NAME = '$escapedTable' and COLUMN_NAME = '$escapedColumn'"
    $count = [int](Invoke-Scalar -Connection $Connection -Sql $sql)
    if ($count -ne 1) {
        throw "Required column missing: $SchemaName.$TableName.$ColumnName"
    }
}

$connection = New-SqlConnection -ConnectionStringValue $ConnectionString
try {
    Assert-TableExists -Connection $connection -SchemaName 'migration' -TableName 'Runs'
    Assert-TableExists -Connection $connection -SchemaName 'migration' -TableName 'WorkItems'
    Assert-TableExists -Connection $connection -SchemaName 'migration' -TableName 'ManifestRows'

    $runsColumns = @(
        'RunId',
        'ProjectId',
        'RunKey',
        'Status',
        'StatusReason',
        'CoordinatorOwner',
        'CoordinationLeaseExpiresUtc',
        'RequestedAtUtc',
        'StartedAtUtc',
        'CompletedAtUtc',
        'RequestedCancellationUtc',
        'CancellationReason',
        'CompletionEvaluatedUtc',
        'CreatedAtUtc',
        'UpdatedAtUtc'
    )

    foreach ($column in $runsColumns) {
        Assert-ColumnExists -Connection $connection -SchemaName 'migration' -TableName 'Runs' -ColumnName $column
    }

    $workItemColumns = @(
        'WorkItemId',
        'RunId',
        'ManifestRowId',
        'WorkItemType',
        'Status',
        'PartitionKey',
        'Priority',
        'AttemptCount',
        'MaxAttempts',
        'LeaseOwner',
        'LeaseExpiresUtc',
        'NotBeforeUtc',
        'PayloadJson',
        'ResultJson',
        'LastErrorCode',
        'LastErrorMessage',
        'CreatedUtc',
        'UpdatedUtc'
    )

    foreach ($column in $workItemColumns) {
        Assert-ColumnExists -Connection $connection -SchemaName 'migration' -TableName 'WorkItems' -ColumnName $column
    }

    $manifestColumns = @(
        'ManifestRowId',
        'RunId',
        'ManifestStatus',
        'Operation',
        'PayloadJson',
        'SourcePath',
        'SourceExternalId',
        'SourceRowNumber',
        'ContentHash',
        'ValidationJson',
        'CreatedAtUtc',
        'UpdatedAtUtc'
    )

    foreach ($column in $manifestColumns) {
        Assert-ColumnExists -Connection $connection -SchemaName 'migration' -TableName 'ManifestRows' -ColumnName $column
    }

    Write-Host 'P7.8B SQL operational runtime schema validation passed.'
}
finally {
    $connection.Dispose()
}
