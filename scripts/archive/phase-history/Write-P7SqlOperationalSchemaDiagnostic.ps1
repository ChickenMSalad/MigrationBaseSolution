Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

param(
    [Parameter(Mandatory = $true)]
    [string]$ConnectionString
)

function Get-RepositoryRoot {
    if ($PSScriptRoot) {
        return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
    }

    return (Get-Location).Path
}

function Invoke-ScalarQuery {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [string]$Sql
    )

    $command = $Connection.CreateCommand()
    $command.CommandText = $Sql
    $command.CommandTimeout = 30
    return $command.ExecuteScalar()
}

function Invoke-DataTableQuery {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [string]$Sql
    )

    $command = $Connection.CreateCommand()
    $command.CommandText = $Sql
    $command.CommandTimeout = 30
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter $command
    $table = New-Object System.Data.DataTable
    [void]$adapter.Fill($table)
    return $table
}

$root = Get-RepositoryRoot
$outputPath = Join-Path $root 'docs\p7\P7.7E-SQL-Operational-Schema-Diagnostic.generated.md'
$outputDir = Split-Path -Parent $outputPath
if (-not (Test-Path -LiteralPath $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('# P7.7E SQL Operational Schema Diagnostic')
$lines.Add('')
$lines.Add(('Generated: {0:O}' -f [DateTimeOffset]::UtcNow))
$lines.Add('')

$connection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
try {
    $connection.Open()
    $databaseName = Invoke-ScalarQuery -Connection $connection -Sql 'select db_name();'
    $serverName = Invoke-ScalarQuery -Connection $connection -Sql 'select @@servername;'

    $lines.Add('## Connection')
    $lines.Add('')
    $lines.Add(('- Server: `{0}`' -f $serverName))
    $lines.Add(('- Database: `{0}`' -f $databaseName))
    $lines.Add('')

    $tableSql = @"
select
    schema_name(t.schema_id) as SchemaName,
    t.name as TableName,
    sum(p.rows) as ApproximateRows
from sys.tables t
inner join sys.partitions p on p.object_id = t.object_id and p.index_id in (0, 1)
where schema_name(t.schema_id) in ('migration', 'dbo')
  and (
       t.name like '%Run%'
    or t.name like '%Manifest%'
    or t.name like '%WorkItem%'
    or t.name like '%Failure%'
    or t.name like '%Checkpoint%'
    or t.name like '%Mapping%'
  )
group by schema_name(t.schema_id), t.name
order by schema_name(t.schema_id), t.name;
"@

    $tables = Invoke-DataTableQuery -Connection $connection -Sql $tableSql
    $lines.Add('## Operational-looking tables')
    $lines.Add('')

    if ($tables.Rows.Count -eq 0) {
        $lines.Add('_No operational-looking tables were found in `migration` or `dbo` schemas._')
    }
    else {
        $lines.Add('| Schema | Table | Approximate Rows |')
        $lines.Add('|---|---|---:|')
        foreach ($row in $tables.Rows) {
            $lines.Add(('| `{0}` | `{1}` | {2} |' -f $row.SchemaName, $row.TableName, $row.ApproximateRows))
        }
    }

    $lines.Add('')
    $lines.Add('## Notes')
    $lines.Add('')
    $lines.Add('- This report is read-only.')
    $lines.Add('- It does not seed runs or work items.')
    $lines.Add('- Use it to confirm the SQL operational schema before running the worker host in no-op completion mode.')
}
finally {
    if ($null -ne $connection) {
        $connection.Dispose()
    }
}

Set-Content -Path $outputPath -Value $lines -Encoding UTF8
Write-Host "Wrote $outputPath"
