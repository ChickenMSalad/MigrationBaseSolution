/*
P7.9E runtime dev reset readiness diagnostics.
Read-only.
*/

SET NOCOUNT ON;

SELECT
    'RuntimeObjectInventory' AS SectionName,
    s.name AS SchemaName,
    t.name AS ObjectName,
    CASE
        WHEN t.name IN ('WorkItems', 'ManifestRows', 'Runs', 'WorkItemFailures', 'WorkItemExecutionAttempts') THEN 'CanonicalRuntime'
        WHEN t.name IN ('MigrationWorkItems', 'MigrationManifestRecords', 'MigrationManifestRows', 'MigrationRuns') THEN 'LegacyOrCompatibility'
        ELSE 'OtherMigrationObject'
    END AS ObjectCategory,
    SUM(p.rows) AS ApproxRows
FROM sys.tables t
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
LEFT JOIN sys.partitions p ON p.object_id = t.object_id AND p.index_id IN (0, 1)
WHERE s.name = N'migration'
GROUP BY s.name, t.name
ORDER BY ObjectCategory, ObjectName;

SELECT
    'RuntimeForeignKeys' AS SectionName,
    fk.name AS ForeignKeyName,
    OBJECT_SCHEMA_NAME(fk.parent_object_id) AS ChildSchema,
    OBJECT_NAME(fk.parent_object_id) AS ChildTable,
    OBJECT_SCHEMA_NAME(fk.referenced_object_id) AS ParentSchema,
    OBJECT_NAME(fk.referenced_object_id) AS ParentTable
FROM sys.foreign_keys fk
WHERE OBJECT_SCHEMA_NAME(fk.parent_object_id) = N'migration'
  AND OBJECT_NAME(fk.parent_object_id) IN (N'WorkItems', N'ManifestRows', N'WorkItemFailures', N'WorkItemExecutionAttempts')
ORDER BY ChildTable, ForeignKeyName;

SELECT
    'WorkItemStateSummary' AS SectionName,
    Status,
    COUNT_BIG(*) AS TotalRows,
    MIN(CreatedAtUtc) AS OldestCreatedAtUtc,
    MAX(UpdatedAtUtc) AS NewestUpdatedAtUtc
FROM migration.WorkItems
GROUP BY Status
ORDER BY Status;

SELECT
    'RecentWorkItems' AS SectionName,
    WorkItemId,
    RunId,
    WorkType,
    Status,
    AttemptCount,
    ClaimedBy,
    CreatedAtUtc,
    UpdatedAtUtc,
    CompletedAtUtc,
    LastErrorMessage
FROM migration.WorkItems
ORDER BY WorkItemId DESC
OFFSET 0 ROWS FETCH NEXT 25 ROWS ONLY;
