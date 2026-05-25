SET NOCOUNT ON;

SELECT
    s.name AS SchemaName,
    t.name AS TableName,
    SUM(p.rows) AS ApproximateRows
FROM sys.tables AS t
INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
LEFT JOIN sys.partitions AS p ON p.object_id = t.object_id AND p.index_id IN (0, 1)
WHERE s.name IN ('dbo', 'migration')
GROUP BY s.name, t.name
ORDER BY s.name, t.name;

SELECT
    s.name AS SchemaName,
    t.name AS TableName,
    c.name AS ColumnName,
    ty.name AS SqlType,
    c.max_length AS MaxLength,
    c.is_nullable AS IsNullable
FROM sys.columns AS c
INNER JOIN sys.tables AS t ON t.object_id = c.object_id
INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
INNER JOIN sys.types AS ty ON ty.user_type_id = c.user_type_id
WHERE c.name IN ('RunId', 'WorkItemId', 'ManifestRowId', 'Status', 'LeaseExpiresUtc', 'UpdatedUtc', 'CreatedUtc')
ORDER BY s.name, t.name, c.column_id;

SELECT
    fk.name AS ForeignKeyName,
    OBJECT_SCHEMA_NAME(fk.parent_object_id) AS ParentSchema,
    OBJECT_NAME(fk.parent_object_id) AS ParentTable,
    OBJECT_SCHEMA_NAME(fk.referenced_object_id) AS ReferencedSchema,
    OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable
FROM sys.foreign_keys AS fk
ORDER BY ParentSchema, ParentTable, ForeignKeyName;
