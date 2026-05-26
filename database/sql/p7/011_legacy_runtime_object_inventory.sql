/*
P7.8H Legacy runtime object inventory

Read-only inventory. It classifies known current runtime objects and known legacy GUID-era runtime objects.
It must not mutate schema or data.
*/

SET NOCOUNT ON;

;WITH RuntimeObjects AS
(
    SELECT
        v.SchemaName,
        v.ObjectName,
        v.ObjectCategory,
        v.IsLegacyRuntimeObject,
        v.CanonicalRuntimeObject
    FROM (VALUES
        (N'migration', N'WorkItems', N'CurrentWorkItemQueue', CONVERT(bit, 0), N'migration.WorkItems'),
        (N'migration', N'ManifestRows', N'CurrentManifestRows', CONVERT(bit, 0), N'migration.ManifestRows'),
        (N'migration', N'WorkItemFailures', N'CurrentWorkItemFailures', CONVERT(bit, 0), N'migration.WorkItemFailures'),
        (N'migration', N'WorkItemExecutionAttempts', N'CurrentWorkItemExecutionAttempts', CONVERT(bit, 0), N'migration.WorkItemExecutionAttempts'),
        (N'migration', N'MigrationWorkItems', N'LegacyGuidWorkItems', CONVERT(bit, 1), N'migration.WorkItems'),
        (N'dbo', N'MigrationWorkItems', N'LegacyGuidWorkItems', CONVERT(bit, 1), N'migration.WorkItems'),
        (N'migration', N'MigrationManifestRows', N'LegacyGuidManifestRows', CONVERT(bit, 1), N'migration.ManifestRows'),
        (N'dbo', N'MigrationManifestRows', N'LegacyGuidManifestRows', CONVERT(bit, 1), N'migration.ManifestRows'),
        (N'migration', N'MigrationManifestRecords', N'LegacyGuidManifestRecords', CONVERT(bit, 1), N'migration.ManifestRows'),
        (N'dbo', N'MigrationManifestRecords', N'LegacyGuidManifestRecords', CONVERT(bit, 1), N'migration.ManifestRows'),
        (N'migration', N'MigrationFailures', N'LegacyGuidFailureReferences', CONVERT(bit, 1), N'migration.WorkItemFailures')
    ) AS v(SchemaName, ObjectName, ObjectCategory, IsLegacyRuntimeObject, CanonicalRuntimeObject)
)
SELECT
    ro.SchemaName,
    ro.ObjectName,
    ro.ObjectCategory,
    ro.IsLegacyRuntimeObject,
    ro.CanonicalRuntimeObject,
    CASE WHEN t.object_id IS NULL THEN CONVERT(bit, 0) ELSE CONVERT(bit, 1) END AS ObjectExists,
    c.column_id AS ColumnId,
    c.name AS ColumnName,
    ty.name AS DataType,
    c.max_length AS MaxLength,
    c.precision AS [Precision],
    c.scale AS Scale,
    c.is_nullable AS IsNullable,
    c.is_identity AS IsIdentity
FROM RuntimeObjects ro
LEFT JOIN sys.schemas s
    ON s.name = ro.SchemaName
LEFT JOIN sys.tables t
    ON t.schema_id = s.schema_id
   AND t.name = ro.ObjectName
LEFT JOIN sys.columns c
    ON c.object_id = t.object_id
LEFT JOIN sys.types ty
    ON ty.user_type_id = c.user_type_id
ORDER BY
    ro.IsLegacyRuntimeObject,
    ro.SchemaName,
    ro.ObjectName,
    c.column_id;

;WITH RuntimeProcedures AS
(
    SELECT
        v.SchemaName,
        v.ProcedureName,
        v.ObjectCategory,
        v.IsLegacyRuntimeObject
    FROM (VALUES
        (N'migration', N'usp_ClaimWorkItems', N'CurrentRuntimeQueueProcedure', CONVERT(bit, 0)),
        (N'migration', N'usp_CompleteWorkItem', N'CurrentRuntimeQueueProcedure', CONVERT(bit, 0)),
        (N'migration', N'usp_FailWorkItem', N'CurrentRuntimeQueueProcedure', CONVERT(bit, 0)),
        (N'migration', N'usp_EnqueueManifestWorkItems', N'CurrentRuntimeQueueProcedure', CONVERT(bit, 0)),
        (N'migration', N'usp_RecordWorkItemExecutionAttemptStarted', N'CurrentRuntimeExecutionHistoryProcedure', CONVERT(bit, 0)),
        (N'migration', N'usp_RecordWorkItemExecutionAttemptCompleted', N'CurrentRuntimeExecutionHistoryProcedure', CONVERT(bit, 0))
    ) AS v(SchemaName, ProcedureName, ObjectCategory, IsLegacyRuntimeObject)
)
SELECT
    rp.SchemaName,
    rp.ProcedureName,
    rp.ObjectCategory,
    rp.IsLegacyRuntimeObject,
    CASE WHEN p.object_id IS NULL THEN CONVERT(bit, 0) ELSE CONVERT(bit, 1) END AS ObjectExists,
    prm.parameter_id AS ParameterId,
    prm.name AS ParameterName,
    TYPE_NAME(prm.user_type_id) AS ParameterType,
    prm.max_length AS MaxLength,
    prm.is_output AS IsOutput
FROM RuntimeProcedures rp
LEFT JOIN sys.schemas s
    ON s.name = rp.SchemaName
LEFT JOIN sys.procedures p
    ON p.schema_id = s.schema_id
   AND p.name = rp.ProcedureName
LEFT JOIN sys.parameters prm
    ON prm.object_id = p.object_id
ORDER BY
    rp.SchemaName,
    rp.ProcedureName,
    prm.parameter_id;
