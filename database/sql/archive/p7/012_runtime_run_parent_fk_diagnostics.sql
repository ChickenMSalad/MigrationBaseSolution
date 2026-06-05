SET NOCOUNT ON;

PRINT 'P7.9A runtime run parent FK diagnostics';

SELECT
    s.name AS SchemaName,
    t.name AS TableName,
    c.name AS ColumnName,
    ty.name AS DataType,
    c.is_nullable AS IsNullable,
    c.is_identity AS IsIdentity
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
JOIN sys.columns c ON c.object_id = t.object_id
JOIN sys.types ty ON ty.user_type_id = c.user_type_id
WHERE s.name = N'migration'
  AND t.name IN (N'Runs', N'MigrationRuns', N'WorkItems')
  AND c.name IN (N'RunId', N'WorkItemId', N'Status', N'CreatedAtUtc', N'CreatedAt')
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

SELECT N'Runs' AS TableName, COUNT_BIG(*) AS TotalRows FROM migration.Runs
UNION ALL
SELECT N'MigrationRuns' AS TableName, COUNT_BIG(*) AS TotalRows FROM migration.MigrationRuns
UNION ALL
SELECT N'WorkItems' AS TableName, COUNT_BIG(*) AS TotalRows FROM migration.WorkItems;

SELECT
    N'WorkItemsWithoutCanonicalRun' AS Finding,
    COUNT_BIG(*) AS TotalRows
FROM migration.WorkItems wi
LEFT JOIN migration.Runs r ON r.RunId = wi.RunId
WHERE r.RunId IS NULL;

SELECT TOP (50)
    wi.WorkItemId,
    wi.RunId,
    wi.Status,
    wi.CreatedAtUtc
FROM migration.WorkItems wi
LEFT JOIN migration.Runs r ON r.RunId = wi.RunId
WHERE r.RunId IS NULL
ORDER BY wi.WorkItemId DESC;
