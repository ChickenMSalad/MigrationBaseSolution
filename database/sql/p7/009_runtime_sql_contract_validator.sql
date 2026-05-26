/*
P7.8B runtime SQL contract validator.

Purpose:
  Fail fast when a target SQL database does not match the canonical P7 runtime contract
  used by the Service Bus dispatcher/executor and SQL operational queue.

This script is intentionally read-only. It creates no tables, indexes, or procedures.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Failures TABLE
(
    FailureId int IDENTITY(1,1) NOT NULL,
    FailureMessage nvarchar(4000) NOT NULL
);

DECLARE @Warnings TABLE
(
    WarningId int IDENTITY(1,1) NOT NULL,
    WarningMessage nvarchar(4000) NOT NULL
);

DECLARE @WorkItemsObjectId int = OBJECT_ID(N'migration.WorkItems', N'U');
DECLARE @ManifestRowsObjectId int = OBJECT_ID(N'migration.ManifestRows', N'U');
DECLARE @RunsObjectId int = OBJECT_ID(N'migration.Runs', N'U');

IF SCHEMA_ID(N'migration') IS NULL
BEGIN
    INSERT INTO @Failures(FailureMessage) VALUES (N'Missing schema: migration.');
END;

IF @WorkItemsObjectId IS NULL
BEGIN
    INSERT INTO @Failures(FailureMessage) VALUES (N'Missing active runtime table: migration.WorkItems.');
END;

IF @ManifestRowsObjectId IS NULL
BEGIN
    INSERT INTO @Failures(FailureMessage) VALUES (N'Missing active runtime table: migration.ManifestRows.');
END;

IF @RunsObjectId IS NULL
BEGIN
    INSERT INTO @Failures(FailureMessage) VALUES (N'Missing active runtime table: migration.Runs.');
END;

DECLARE @RequiredColumns TABLE
(
    SchemaName sysname NOT NULL,
    TableName sysname NOT NULL,
    ColumnName sysname NOT NULL,
    DataType sysname NOT NULL,
    IsNullable bit NULL,
    IsIdentity bit NULL
);

INSERT INTO @RequiredColumns(SchemaName, TableName, ColumnName, DataType, IsNullable, IsIdentity)
VALUES
    (N'migration', N'WorkItems', N'WorkItemId', N'bigint', 0, 1),
    (N'migration', N'WorkItems', N'RunId', N'uniqueidentifier', 0, 0),
    (N'migration', N'WorkItems', N'ManifestRowId', N'bigint', NULL, 0),
    (N'migration', N'WorkItems', N'WorkType', N'nvarchar', NULL, 0),
    (N'migration', N'WorkItems', N'Status', N'nvarchar', 0, 0),
    (N'migration', N'WorkItems', N'AttemptCount', N'int', 0, 0),
    (N'migration', N'WorkItems', N'MaxAttempts', N'int', 0, 0),
    (N'migration', N'WorkItems', N'PayloadJson', N'nvarchar', NULL, 0),
    (N'migration', N'WorkItems', N'ResultJson', N'nvarchar', NULL, 0),
    (N'migration', N'WorkItems', N'LastErrorMessage', N'nvarchar', NULL, 0),
    (N'migration', N'WorkItems', N'CreatedAtUtc', N'datetime2', 0, 0),
    (N'migration', N'WorkItems', N'UpdatedAtUtc', N'datetime2', 0, 0),
    (N'migration', N'WorkItems', N'DispatchedAtUtc', N'datetime2', NULL, 0),
    (N'migration', N'WorkItems', N'LeaseOwner', N'nvarchar', NULL, 0),
    (N'migration', N'WorkItems', N'LeaseExpiresUtc', N'datetime2', NULL, 0),
    (N'migration', N'WorkItems', N'WorkItemType', N'nvarchar', NULL, 0),
    (N'migration', N'ManifestRows', N'ManifestRowId', N'bigint', 0, 1),
    (N'migration', N'ManifestRows', N'RunId', N'uniqueidentifier', 0, 0),
    (N'migration', N'ManifestRows', N'PayloadJson', N'nvarchar', NULL, 0),
    (N'migration', N'ManifestRows', N'CreatedAtUtc', N'datetime2', 0, 0),
    (N'migration', N'ManifestRows', N'UpdatedAtUtc', N'datetime2', 0, 0),
    (N'migration', N'Runs', N'RunId', N'uniqueidentifier', 0, 0),
    (N'migration', N'Runs', N'Status', N'nvarchar', 0, 0),
    (N'migration', N'Runs', N'CreatedAtUtc', N'datetimeoffset', 0, 0);

INSERT INTO @Failures(FailureMessage)
SELECT CONCAT(N'Missing required column: ', rc.SchemaName, N'.', rc.TableName, N'.', rc.ColumnName, N'.')
FROM @RequiredColumns rc
WHERE COL_LENGTH(QUOTENAME(rc.SchemaName) + N'.' + QUOTENAME(rc.TableName), rc.ColumnName) IS NULL;

INSERT INTO @Failures(FailureMessage)
SELECT CONCAT(N'Wrong data type for ', rc.SchemaName, N'.', rc.TableName, N'.', rc.ColumnName,
              N'. Expected ', rc.DataType, N', found ', ty.name, N'.')
FROM @RequiredColumns rc
JOIN sys.schemas s ON s.name = rc.SchemaName
JOIN sys.tables t ON t.schema_id = s.schema_id AND t.name = rc.TableName
JOIN sys.columns c ON c.object_id = t.object_id AND c.name = rc.ColumnName
JOIN sys.types ty ON ty.user_type_id = c.user_type_id
WHERE ty.name <> rc.DataType;

INSERT INTO @Failures(FailureMessage)
SELECT CONCAT(N'Wrong nullability for ', rc.SchemaName, N'.', rc.TableName, N'.', rc.ColumnName,
              N'. Expected nullable=', CONVERT(nvarchar(1), rc.IsNullable),
              N', found nullable=', CONVERT(nvarchar(1), c.is_nullable), N'.')
FROM @RequiredColumns rc
JOIN sys.schemas s ON s.name = rc.SchemaName
JOIN sys.tables t ON t.schema_id = s.schema_id AND t.name = rc.TableName
JOIN sys.columns c ON c.object_id = t.object_id AND c.name = rc.ColumnName
WHERE rc.IsNullable IS NOT NULL
  AND c.is_nullable <> rc.IsNullable;

INSERT INTO @Failures(FailureMessage)
SELECT CONCAT(N'Wrong identity setting for ', rc.SchemaName, N'.', rc.TableName, N'.', rc.ColumnName,
              N'. Expected identity=', CONVERT(nvarchar(1), rc.IsIdentity),
              N', found identity=', CONVERT(nvarchar(1), c.is_identity), N'.')
FROM @RequiredColumns rc
JOIN sys.schemas s ON s.name = rc.SchemaName
JOIN sys.tables t ON t.schema_id = s.schema_id AND t.name = rc.TableName
JOIN sys.columns c ON c.object_id = t.object_id AND c.name = rc.ColumnName
WHERE rc.IsIdentity IS NOT NULL
  AND c.is_identity <> rc.IsIdentity;

DECLARE @RequiredIndexes TABLE
(
    SchemaName sysname NOT NULL,
    TableName sysname NOT NULL,
    IndexName sysname NOT NULL
);

INSERT INTO @RequiredIndexes(SchemaName, TableName, IndexName)
VALUES
    (N'migration', N'WorkItems', N'IX_WorkItems_ClaimQueue'),
    (N'migration', N'WorkItems', N'IX_WorkItems_LeaseRecovery'),
    (N'migration', N'ManifestRows', N'IX_ManifestRows_RunId_Status_RowId');

INSERT INTO @Failures(FailureMessage)
SELECT CONCAT(N'Missing required index: ', ri.SchemaName, N'.', ri.TableName, N'.', ri.IndexName, N'.')
FROM @RequiredIndexes ri
JOIN sys.schemas s ON s.name = ri.SchemaName
JOIN sys.tables t ON t.schema_id = s.schema_id AND t.name = ri.TableName
LEFT JOIN sys.indexes i ON i.object_id = t.object_id AND i.name = ri.IndexName
WHERE i.index_id IS NULL;

DECLARE @RequiredProcedures TABLE
(
    SchemaName sysname NOT NULL,
    ProcedureName sysname NOT NULL
);

INSERT INTO @RequiredProcedures(SchemaName, ProcedureName)
VALUES
    (N'migration', N'usp_ClaimWorkItems'),
    (N'migration', N'usp_CompleteWorkItem'),
    (N'migration', N'usp_FailWorkItem'),
    (N'migration', N'usp_RecordWorkItemExecutionAttemptStarted'),
    (N'migration', N'usp_RecordWorkItemExecutionAttemptCompleted');

INSERT INTO @Failures(FailureMessage)
SELECT CONCAT(N'Missing required procedure: ', rp.SchemaName, N'.', rp.ProcedureName, N'.')
FROM @RequiredProcedures rp
LEFT JOIN sys.schemas s ON s.name = rp.SchemaName
LEFT JOIN sys.procedures p ON p.schema_id = s.schema_id AND p.name = rp.ProcedureName
WHERE p.object_id IS NULL;

IF OBJECT_ID(N'migration.MigrationWorkItems', N'U') IS NOT NULL
BEGIN
    INSERT INTO @Warnings(WarningMessage)
    VALUES (N'Legacy GUID table exists: migration.MigrationWorkItems. Runtime workers must not use this table.');
END;

IF OBJECT_ID(N'migration.MigrationManifestRecords', N'U') IS NOT NULL
BEGIN
    INSERT INTO @Warnings(WarningMessage)
    VALUES (N'Legacy GUID table exists: migration.MigrationManifestRecords. Runtime workers must not use this table.');
END;

IF EXISTS (SELECT 1 FROM @Warnings)
BEGIN
    SELECT N'WARNING' AS Severity, WarningMessage AS Message
    FROM @Warnings
    ORDER BY WarningId;
END;

IF EXISTS (SELECT 1 FROM @Failures)
BEGIN
    SELECT N'FAILURE' AS Severity, FailureMessage AS Message
    FROM @Failures
    ORDER BY FailureId;

    THROW 51780, 'P7.8B runtime SQL contract validation failed.', 1;
END;

SELECT N'PASS' AS Severity,
       N'P7.8B runtime SQL contract validation passed.' AS Message;
