SET NOCOUNT ON;

DECLARE @Failures TABLE
(
    FailureId int IDENTITY(1,1) NOT NULL,
    FailureMessage nvarchar(4000) NOT NULL
);

DECLARE @ExpectedColumns TABLE
(
    SchemaName sysname NOT NULL,
    TableName sysname NOT NULL,
    ColumnName sysname NOT NULL,
    DataType sysname NOT NULL,
    IsNullable bit NULL,
    IsIdentity bit NULL
);

INSERT INTO @ExpectedColumns (SchemaName, TableName, ColumnName, DataType, IsNullable, IsIdentity)
VALUES
    (N'migration', N'WorkItems', N'WorkItemId', N'bigint', 0, 1),
    (N'migration', N'WorkItems', N'RunId', N'uniqueidentifier', 0, 0),
    (N'migration', N'WorkItems', N'ManifestRowId', N'bigint', 1, 0),
    (N'migration', N'WorkItems', N'WorkType', N'nvarchar', 0, 0),
    (N'migration', N'WorkItems', N'Status', N'nvarchar', 0, 0),
    (N'migration', N'WorkItems', N'AttemptCount', N'int', 0, 0),
    (N'migration', N'WorkItems', N'MaxAttempts', N'int', 0, 0),
    (N'migration', N'WorkItems', N'PayloadJson', N'nvarchar', 1, 0),
    (N'migration', N'WorkItems', N'CreatedAtUtc', N'datetime2', 0, 0),
    (N'migration', N'WorkItems', N'UpdatedAtUtc', N'datetime2', 0, 0),
    (N'migration', N'WorkItems', N'DispatchedAtUtc', N'datetime2', 1, 0),
    (N'migration', N'ManifestRows', N'ManifestRowId', N'bigint', 0, 1),
    (N'migration', N'ManifestRows', N'RunId', N'uniqueidentifier', 0, 0),
    (N'migration', N'ManifestRows', N'PayloadJson', N'nvarchar', 1, 0),
    (N'migration', N'WorkItemExecutionAttempts', N'ExecutionAttemptId', N'bigint', 0, 1),
    (N'migration', N'WorkItemExecutionAttempts', N'WorkItemId', N'bigint', 0, 0),
    (N'migration', N'WorkItemExecutionAttempts', N'ManifestRowId', N'bigint', 1, 0),
    (N'migration', N'WorkItemFailures', N'WorkItemFailureId', N'bigint', 0, 1),
    (N'migration', N'WorkItemFailures', N'WorkItemId', N'bigint', 0, 0);

INSERT INTO @Failures (FailureMessage)
SELECT CONCAT(N'Missing table ', ec.SchemaName, N'.', ec.TableName)
FROM @ExpectedColumns ec
WHERE OBJECT_ID(QUOTENAME(ec.SchemaName) + N'.' + QUOTENAME(ec.TableName), N'U') IS NULL
GROUP BY ec.SchemaName, ec.TableName;

INSERT INTO @Failures (FailureMessage)
SELECT CONCAT(
    N'Missing column ', ec.SchemaName, N'.', ec.TableName, N'.', ec.ColumnName,
    N' expected ', ec.DataType)
FROM @ExpectedColumns ec
LEFT JOIN sys.schemas s ON s.name = ec.SchemaName
LEFT JOIN sys.tables t ON t.schema_id = s.schema_id AND t.name = ec.TableName
LEFT JOIN sys.columns c ON c.object_id = t.object_id AND c.name = ec.ColumnName
WHERE c.column_id IS NULL;

INSERT INTO @Failures (FailureMessage)
SELECT CONCAT(
    N'Column type mismatch ', ec.SchemaName, N'.', ec.TableName, N'.', ec.ColumnName,
    N': expected ', ec.DataType,
    N', actual ', COALESCE(ty.name, N'<missing>'))
FROM @ExpectedColumns ec
JOIN sys.schemas s ON s.name = ec.SchemaName
JOIN sys.tables t ON t.schema_id = s.schema_id AND t.name = ec.TableName
JOIN sys.columns c ON c.object_id = t.object_id AND c.name = ec.ColumnName
JOIN sys.types ty ON ty.user_type_id = c.user_type_id
WHERE ty.name <> ec.DataType;

INSERT INTO @Failures (FailureMessage)
SELECT CONCAT(
    N'Column nullability mismatch ', ec.SchemaName, N'.', ec.TableName, N'.', ec.ColumnName,
    N': expected is_nullable=', CONVERT(nvarchar(1), ec.IsNullable),
    N', actual is_nullable=', CONVERT(nvarchar(1), c.is_nullable))
FROM @ExpectedColumns ec
JOIN sys.schemas s ON s.name = ec.SchemaName
JOIN sys.tables t ON t.schema_id = s.schema_id AND t.name = ec.TableName
JOIN sys.columns c ON c.object_id = t.object_id AND c.name = ec.ColumnName
WHERE ec.IsNullable IS NOT NULL
  AND c.is_nullable <> ec.IsNullable;

INSERT INTO @Failures (FailureMessage)
SELECT CONCAT(
    N'Column identity mismatch ', ec.SchemaName, N'.', ec.TableName, N'.', ec.ColumnName,
    N': expected is_identity=', CONVERT(nvarchar(1), ec.IsIdentity),
    N', actual is_identity=', CONVERT(nvarchar(1), c.is_identity))
FROM @ExpectedColumns ec
JOIN sys.schemas s ON s.name = ec.SchemaName
JOIN sys.tables t ON t.schema_id = s.schema_id AND t.name = ec.TableName
JOIN sys.columns c ON c.object_id = t.object_id AND c.name = ec.ColumnName
WHERE ec.IsIdentity IS NOT NULL
  AND c.is_identity <> ec.IsIdentity;

DECLARE @ExpectedParameters TABLE
(
    SchemaName sysname NOT NULL,
    ProcedureName sysname NOT NULL,
    ParameterName sysname NOT NULL,
    ParameterType sysname NOT NULL,
    IsOutput bit NULL
);

INSERT INTO @ExpectedParameters (SchemaName, ProcedureName, ParameterName, ParameterType, IsOutput)
VALUES
    (N'migration', N'usp_ClaimWorkItems', N'@WorkerId', N'nvarchar', 0),
    (N'migration', N'usp_ClaimWorkItems', N'@BatchSize', N'int', 0),
    (N'migration', N'usp_ClaimWorkItems', N'@LeaseSeconds', N'int', 0),
    (N'migration', N'usp_ClaimWorkItems', N'@RunId', N'uniqueidentifier', 0),
    (N'migration', N'usp_CompleteWorkItem', N'@WorkItemId', N'bigint', 0),
    (N'migration', N'usp_FailWorkItem', N'@WorkItemId', N'bigint', 0),
    (N'migration', N'usp_RecordWorkItemExecutionAttemptStarted', N'@WorkItemId', N'bigint', 0),
    (N'migration', N'usp_RecordWorkItemExecutionAttemptStarted', N'@ManifestRowId', N'bigint', 0),
    (N'migration', N'usp_RecordWorkItemExecutionAttemptStarted', N'@ExecutionAttemptId', N'bigint', 1);

INSERT INTO @Failures (FailureMessage)
SELECT CONCAT(
    N'Missing procedure parameter ', ep.SchemaName, N'.', ep.ProcedureName, N' ', ep.ParameterName)
FROM @ExpectedParameters ep
LEFT JOIN sys.schemas s ON s.name = ep.SchemaName
LEFT JOIN sys.procedures p ON p.schema_id = s.schema_id AND p.name = ep.ProcedureName
LEFT JOIN sys.parameters prm ON prm.object_id = p.object_id AND prm.name = ep.ParameterName
WHERE prm.parameter_id IS NULL;

INSERT INTO @Failures (FailureMessage)
SELECT CONCAT(
    N'Procedure parameter type mismatch ', ep.SchemaName, N'.', ep.ProcedureName, N' ', ep.ParameterName,
    N': expected ', ep.ParameterType,
    N', actual ', COALESCE(TYPE_NAME(prm.user_type_id), N'<missing>'))
FROM @ExpectedParameters ep
JOIN sys.schemas s ON s.name = ep.SchemaName
JOIN sys.procedures p ON p.schema_id = s.schema_id AND p.name = ep.ProcedureName
JOIN sys.parameters prm ON prm.object_id = p.object_id AND prm.name = ep.ParameterName
WHERE TYPE_NAME(prm.user_type_id) <> ep.ParameterType;

INSERT INTO @Failures (FailureMessage)
SELECT CONCAT(
    N'Procedure parameter output mismatch ', ep.SchemaName, N'.', ep.ProcedureName, N' ', ep.ParameterName,
    N': expected is_output=', CONVERT(nvarchar(1), ep.IsOutput),
    N', actual is_output=', CONVERT(nvarchar(1), prm.is_output))
FROM @ExpectedParameters ep
JOIN sys.schemas s ON s.name = ep.SchemaName
JOIN sys.procedures p ON p.schema_id = s.schema_id AND p.name = ep.ProcedureName
JOIN sys.parameters prm ON prm.object_id = p.object_id AND prm.name = ep.ParameterName
WHERE ep.IsOutput IS NOT NULL
  AND prm.is_output <> ep.IsOutput;

INSERT INTO @Failures (FailureMessage)
SELECT CONCAT(N'Active migration module still references legacy object ', o.name, N': ', bad.LegacyObject)
FROM sys.sql_modules m
JOIN sys.objects o ON o.object_id = m.object_id
JOIN sys.schemas s ON s.schema_id = o.schema_id
CROSS APPLY
(
    SELECT LegacyObject
    FROM (VALUES
        (N'dbo.MigrationWorkItems'),
        (N'[dbo].[MigrationWorkItems]'),
        (N'migration.MigrationWorkItems'),
        (N'[migration].[MigrationWorkItems]'),
        (N'dbo.MigrationManifestRows'),
        (N'[dbo].[MigrationManifestRows]'),
        (N'migration.MigrationManifestRecords'),
        (N'[migration].[MigrationManifestRecords]')
    ) v(LegacyObject)
    WHERE m.definition LIKE N'%' + v.LegacyObject + N'%'
) bad
WHERE s.name = N'migration'
  AND o.type IN (N'P', N'V', N'FN', N'IF', N'TF')
  AND o.name NOT LIKE N'%Legacy%';

IF EXISTS (SELECT 1 FROM @Failures)
BEGIN
    SELECT FailureMessage FROM @Failures ORDER BY FailureId;
    THROW 51078, 'P7.8A runtime SQL contract validation failed.', 1;
END;

SELECT N'P7.8A runtime SQL contract validation passed.' AS ValidationResult;
