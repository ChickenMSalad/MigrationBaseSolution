SET NOCOUNT ON;

PRINT 'P7.9H runtime SQL baseline reconciliation diagnostics';

SELECT
    s.name AS SchemaName,
    t.name AS TableName,
    CASE
        WHEN t.name IN (N'Runs', N'WorkItems', N'ManifestRows', N'WorkItemFailures', N'WorkItemExecutionAttempts') THEN N'CanonicalRuntime'
        WHEN t.name IN (N'MigrationRuns', N'MigrationWorkItems', N'MigrationManifestRows', N'MigrationManifestRecords', N'MigrationFailures', N'MigrationIdentifierMaps') THEN N'LegacyRuntime'
        ELSE N'OtherMigration'
    END AS RuntimeCategory,
    SUM(CASE WHEN c.is_identity = 1 THEN 1 ELSE 0 END) AS IdentityColumnCount,
    COUNT(*) AS ColumnCount
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
JOIN sys.columns c ON c.object_id = t.object_id
WHERE s.name = N'migration'
GROUP BY s.name, t.name
ORDER BY RuntimeCategory, TableName;

SELECT
    s.name AS SchemaName,
    t.name AS TableName,
    c.column_id AS ColumnId,
    c.name AS ColumnName,
    ty.name AS DataType,
    c.max_length AS MaxLength,
    c.precision AS NumericPrecision,
    c.scale AS NumericScale,
    c.is_nullable AS IsNullable,
    c.is_identity AS IsIdentity
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
JOIN sys.columns c ON c.object_id = t.object_id
JOIN sys.types ty ON ty.user_type_id = c.user_type_id
WHERE s.name = N'migration'
  AND t.name IN (N'Runs', N'WorkItems', N'ManifestRows', N'MigrationRuns', N'MigrationWorkItems', N'MigrationManifestRows', N'MigrationManifestRecords')
ORDER BY t.name, c.column_id;

SELECT
    fk.name AS ForeignKeyName,
    OBJECT_SCHEMA_NAME(fk.parent_object_id) AS ChildSchema,
    OBJECT_NAME(fk.parent_object_id) AS ChildTable,
    pc.name AS ChildColumn,
    OBJECT_SCHEMA_NAME(fk.referenced_object_id) AS ParentSchema,
    OBJECT_NAME(fk.referenced_object_id) AS ParentTable,
    rc.name AS ParentColumn
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
JOIN sys.columns pc ON pc.object_id = fkc.parent_object_id AND pc.column_id = fkc.parent_column_id
JOIN sys.columns rc ON rc.object_id = fkc.referenced_object_id AND rc.column_id = fkc.referenced_column_id
WHERE OBJECT_SCHEMA_NAME(fk.parent_object_id) = N'migration'
  AND OBJECT_NAME(fk.parent_object_id) = N'WorkItems'
ORDER BY fk.name, fkc.constraint_column_id;

SELECT
    CASE
        WHEN EXISTS (
            SELECT 1
            FROM sys.foreign_keys fk
            WHERE fk.parent_object_id = OBJECT_ID(N'migration.WorkItems')
              AND fk.referenced_object_id = OBJECT_ID(N'migration.Runs')
        ) THEN CAST(1 AS bit)
        ELSE CAST(0 AS bit)
    END AS WorkItemsReferencesCanonicalRuns,
    CASE
        WHEN EXISTS (
            SELECT 1
            FROM sys.foreign_keys fk
            WHERE fk.parent_object_id = OBJECT_ID(N'migration.WorkItems')
              AND fk.referenced_object_id = OBJECT_ID(N'migration.MigrationRuns')
        ) THEN CAST(1 AS bit)
        ELSE CAST(0 AS bit)
    END AS WorkItemsReferencesLegacyMigrationRuns;
