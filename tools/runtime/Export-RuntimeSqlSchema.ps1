[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Server,

    [Parameter(Mandatory = $true)]
    [string]$Database,

    [Parameter(Mandatory = $true)]
    [string]$User,

    [Parameter(Mandatory = $true)]
    [string]$Password,

    [Parameter(Mandatory = $false)]
    [string]$OutputPath = ".\runtime-sql-schema-export.txt"
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Resolve-SqlcmdPath {
    $command = Get-Command sqlcmd -ErrorAction SilentlyContinue
    if ($null -eq $command) {
        throw "sqlcmd was not found on PATH. Install sqlcmd or run this from a shell where sqlcmd is available."
    }

    return $command.Source
}

$sqlcmd = Resolve-SqlcmdPath
$query = @"
SET NOCOUNT ON;

SELECT
    s.name AS SchemaName,
    t.name AS TableName,
    c.column_id AS ColumnId,
    c.name AS ColumnName,
    ty.name AS DataType,
    c.max_length AS MaxLength,
    c.precision AS [Precision],
    c.scale AS Scale,
    c.is_nullable AS IsNullable,
    c.is_identity AS IsIdentity
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
JOIN sys.columns c ON c.object_id = t.object_id
JOIN sys.types ty ON ty.user_type_id = c.user_type_id
WHERE s.name = N'migration'
ORDER BY s.name, t.name, c.column_id;

SELECT
    s.name AS SchemaName,
    i.name AS IndexName,
    t.name AS TableName,
    i.is_unique AS IsUnique,
    i.has_filter AS HasFilter,
    i.filter_definition AS FilterDefinition
FROM sys.indexes i
JOIN sys.tables t ON t.object_id = i.object_id
JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE s.name = N'migration'
  AND i.name IS NOT NULL
ORDER BY s.name, t.name, i.name;

SELECT
    s.name AS SchemaName,
    p.name AS ProcedureName,
    prm.parameter_id AS ParameterId,
    prm.name AS ParameterName,
    TYPE_NAME(prm.user_type_id) AS ParameterType,
    prm.max_length AS MaxLength,
    prm.is_output AS IsOutput
FROM sys.procedures p
JOIN sys.schemas s ON s.schema_id = p.schema_id
LEFT JOIN sys.parameters prm ON prm.object_id = p.object_id
WHERE s.name = N'migration'
ORDER BY s.name, p.name, prm.parameter_id;
"@

$resolvedOutput = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)
$outputDirectory = Split-Path -Parent $resolvedOutput
if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

& $sqlcmd -S $Server -d $Database -U $User -P $Password -Q $query -W -s "`t" -o $resolvedOutput
if ($LASTEXITCODE -ne 0) {
    throw "sqlcmd schema export failed with exit code $LASTEXITCODE."
}

Write-Host "Runtime SQL schema export written to $resolvedOutput"
