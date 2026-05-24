param(
    [Parameter(Mandatory = $true)]
    [string]$ConnectionString
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Data

function New-SqlConnection {
    param([string]$Value)

    $connection = New-Object System.Data.SqlClient.SqlConnection
    $connection.ConnectionString = $Value
    return $connection
}

function Invoke-SqlScalar {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [string]$Sql
    )

    $command = $Connection.CreateCommand()
    $command.CommandText = $Sql
    $command.CommandTimeout = 30
    return $command.ExecuteScalar()
}

$connection = New-SqlConnection -Value $ConnectionString
try {
    $connection.Open()

    $requiredObjects = @(
        @{ Name = 'migration.WorkItemExecutionAttempts'; Type = 'U' },
        @{ Name = 'migration.vw_WorkItemExecutionAttemptSummary'; Type = 'V' },
        @{ Name = 'migration.usp_RecordWorkItemExecutionAttemptStarted'; Type = 'P' },
        @{ Name = 'migration.usp_RecordWorkItemExecutionAttemptCompleted'; Type = 'P' }
    )

    foreach ($requiredObject in $requiredObjects) {
        $name = [string]$requiredObject.Name
        $type = [string]$requiredObject.Type
        $sql = "SELECT COUNT(1) FROM sys.objects WHERE object_id = OBJECT_ID(N'$name') AND type = N'$type';"
        $count = [int](Invoke-SqlScalar -Connection $connection -Sql $sql)
        if ($count -ne 1) {
            throw "Required SQL object missing: $name"
        }
    }

    $requiredColumns = @(
        'ExecutionAttemptId',
        'WorkItemId',
        'RunId',
        'ManifestRowId',
        'WorkItemType',
        'AttemptNumber',
        'WorkerId',
        'Status',
        'StartedUtc',
        'CompletedUtc',
        'DurationMilliseconds',
        'ErrorCode',
        'ErrorMessage',
        'IsRetryable',
        'PayloadJson',
        'ResultJson',
        'CreatedUtc',
        'UpdatedUtc'
    )

    foreach ($column in $requiredColumns) {
        $sql = "SELECT COUNT(1) FROM sys.columns WHERE object_id = OBJECT_ID(N'migration.WorkItemExecutionAttempts') AND name = N'$column';"
        $count = [int](Invoke-SqlScalar -Connection $connection -Sql $sql)
        if ($count -ne 1) {
            throw "Required column missing from migration.WorkItemExecutionAttempts: $column"
        }
    }

    Write-Host 'P7 SQL operational execution history schema validation passed.'
}
finally {
    if ($connection.State -ne [System.Data.ConnectionState]::Closed) {
        $connection.Close()
    }
    $connection.Dispose()
}
